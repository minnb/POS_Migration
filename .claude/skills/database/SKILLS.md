# Skill: Stored Procedure — RPOSMasterData (CentralMD)

> **Áp dụng khi:** tạo mới bất kỳ stored procedure nào cho `RPOSMasterData` (CentralMD),
> hoặc chuyển 1 đoạn Dapper INSERT/UPDATE inline sang SP để dễ kiểm soát lỗi. Đọc file này
> TRƯỚC khi viết SP mới.

---

## Reserved keyword — BẮT BUỘC bracket-quote `[ ]`

> Áp dụng cho **mọi** SQL trong dự án — SP mới, script `docs/sql/*.sql`, và Dapper inline query
> trong Repository — không giới hạn riêng SP.

Trước khi dùng bất kỳ tên cột/bảng nào làm identifier trong SQL Server, nếu trùng với 1 từ khoá
reserved → viết `[TênCột]`. Case cụ thể đã gặp: cột `LineNo` trong `TransVoidLine`
(`CentralSaleRepository.cs` → `GetVoidReportAsync`) gây lỗi `Msg 156 "Incorrect syntax near the
keyword 'LineNo'"` khi không bracket-quote — sửa thành `vl.[LineNo]`. Dự án đã có tiền lệ tương tự
với `[Source]` trong `CentralSaleRepository.cs` (`GetDataRawJsonListAsync`).

**Dấu hiệu nhận biết**: SQL Server báo lỗi **156** "Incorrect syntax near the keyword 'X'" mà cột
`X` **có tồn tại thật** trong bảng (khác lỗi **207** "Invalid column name 'X'", là lỗi tên cột sai/
không tồn tại) → chắc chắn là reserved keyword, bracket-quote ngay `[X]`, không cần đoán thêm.

---

## Quy tắc đặt tên — BẮT BUỘC

**Mọi stored procedure mới PHẢI đặt tên theo:**

```
dbo.usp_{Domain}_{Action}
```

| Phần | Ý nghĩa | Ví dụ |
|---|---|---|
| `usp_` | Prefix cố định cho mọi SP mới của dự án | — |
| `{Domain}` | Tên nghiệp vụ/entity, PascalCase | `Product`, `SetupCoupon`, `ProductLock` |
| `{Action}` | Hành động, PascalCase | `Save`, `Get`, `Delete`, `CheckCodesExist` |

**Ví dụ đã có trong dự án** (tham chiếu khi đặt tên SP mới):

| SP | Domain | Action |
|---|---|---|
| `dbo.usp_Product_Save` | Product | Save |
| `dbo.usp_SetupCoupon_SaveIssue` | SetupCoupon | SaveIssue |
| `dbo.usp_SetupCoupon_SaveAdvanced` | SetupCoupon | SaveAdvanced |
| `dbo.usp_SetupCoupon_CheckCodesExist` | SetupCoupon | CheckCodesExist |
| `dbo.usp_SetupCoupon_Delete` | SetupCoupon | Delete |

> **KHÔNG** dùng các dạng tên khác đã thấy trong legacy/DB cũ (`sp_Article_Save`,
> `GetProductList`, `sp_ProductList_Get`, `[SyncGetDataByTable]`...) cho SP **mới tạo**.
> Các SP đã tồn tại sẵn trong DB với tên khác (đã dùng trước khi có convention này) thì
> **giữ nguyên tên**, không đổi tên SP đang chạy production chỉ để khớp convention.

**Table-Valued Parameter (TVP)** đi kèm SP (khi cần truyền list/child rows) đặt tên:

```
dbo.{Name}TVP
```
Ví dụ: `dbo.ProductBarcodeTVP`, `dbo.CouponCodeTVP`, `dbo.CouponLineTVP`.

---

## Nơi đặt script SQL

Mỗi SP mới (+ TVP đi kèm nếu có) viết thành 1 file trong `docs/sql/{Domain}_{Action}.sql`,
áp dụng **thủ công 1 lần** trên `RPOSMasterData` (không có migration tool tự động).

Ví dụ đã có: `docs/sql/Product_Save.sql`, `docs/sql/SetupCoupon_Save.sql`,
`docs/sql/SpecialCombo_Save.sql`, `docs/sql/SetupVoucher_Save.sql`.

Nếu SP đụng bảng nào — **tra tên bảng/cột đúng trong `docs/architecture/database-schema.md`
TRƯỚC khi viết**, không suy đoán tên bảng/cột (xem mục "Cổng chặn trùng lặp" trong
`CLAUDE.md`). Bảng chưa có trong doc → đọc `docs/sql/database/CentralMD.sql`, bổ sung vào
`database-schema.md` cùng commit.

---

## Template SP ghi dữ liệu (Save/Insert/Update)

```sql
/* ============================================================================
   {Mô tả ngắn nghiệp vụ} — RPOSMasterData
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── TVP (nếu cần truyền list) ────────────────────────────────────────────── */
IF TYPE_ID(N'dbo.{Name}TVP') IS NULL
CREATE TYPE dbo.{Name}TVP AS TABLE
(
    Col1  nvarchar(50)  NULL,
    Col2  nvarchar(10)  NULL
);
GO

/* ── SP ───────────────────────────────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_{Domain}_{Action}', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_{Domain}_{Action};
GO

CREATE PROCEDURE dbo.usp_{Domain}_{Action}
(
    @Param1      nvarchar(50),
    @Lines       dbo.{Name}TVP READONLY,
    @OutItemNo   nvarchar(20) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- logic nghiệp vụ: sinh mã, INSERT/UPDATE, insert từ TVP...

        SET @OutItemNo = @ItemNo;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
```

**Bắt buộc** trong mọi SP ghi dữ liệu: `SET XACT_ABORT ON` + `BEGIN TRY/CATCH` +
`ROLLBACK TRANSACTION` khi lỗi + `THROW` (không nuốt lỗi trong SP — để C# bắt qua
`SqlException` như bình thường).

---

## Gọi SP từ Repository (C#/Dapper) — pattern chuẩn

```csharp
var p = new DynamicParameters();
p.Add("@Param1", value1);
p.Add("@Lines", BuildLineTable(rows).AsTableValuedParameter("dbo.{Name}TVP"));
p.Add("@OutItemNo", dbType: DbType.String, direction: ParameterDirection.Output, size: 20);

using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
await conn.ExecuteAsync(new CommandDefinition("dbo.usp_{Domain}_{Action}", p,
    commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct));

var itemNo = p.Get<string>("@OutItemNo") ?? string.Empty;
```

`BuildLineTable`/`Build{X}Table` helper (private static, `DataTable`) để convert
`List<TDto>` → TVP — xem ví dụ `BuildProductBarcodeTable` trong
`src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs` (`CreateProductAsync`)
và `BuildCodeTable`/`BuildLineTable` trong
`src/POS.Infrastructure/Repositories/CouponVoucher/CouponRepository.cs`.

Khi SP chỉ trả 1 dòng kết quả nghiệp vụ (vd `Success`/`Message`) thay vì output param —
dùng `QueryFirstOrDefaultAsync<TRow>` với `commandType: CommandType.StoredProcedure`
(xem `CouponRepository.DeleteAsync` → `usp_SetupCoupon_Delete`).

---

## Pattern: Bảng dùng chung nhiều nguồn dữ liệu (discriminator column)

> Áp dụng khi: 2 tính năng khác domain nhưng cùng bản chất "mã định danh + trạng thái" cần soi
> chung dữ liệu (vd 1 endpoint check/redeem phải nhận diện được cả 2 nguồn). Thay vì ép chung 1
> Repository/Service (vi phạm SRP) hoặc giữ 2 bảng trùng lặp mãi mãi — dùng 1 bảng vật lý + cột
> `Source varchar(10)` phân biệt, mỗi domain vẫn có Repository/SP riêng, chỉ chung storage.

```sql
-- SP ghi của domain A: insert kèm Source='A' NGAY LÚC TẠO, các field domain B sở hữu để NULL
-- SP ghi của domain B: insert kèm Source='B', ngược lại

-- SP đọc dùng chung (vd check/redeem) — KHÔNG lọc Source nếu khóa nghiệp vụ (Code) đã UNIQUE
-- toàn bảng; ngược lại các SP CRUD nội bộ của từng domain BẮT BUỘC filter đúng Source của mình
-- (tránh 1 row domain khác "lừa" IF EXISTS hoặc bị UPDATE đè dữ liệu domain kia).
```

Khi 2 SP lưu tách rời theo 2 bước (vd "Issue" rồi "Advanced" trong cùng 1 lần Lưu UI, field B chỉ
biết ở bước 2) — thêm 1 UPDATE đồng bộ **không điều kiện** ở SP bước sau, filter đúng
`Source`, để dữ liệu luôn khớp Header mới nhất kể cả khi sửa lại nhiều lần sau này. Tuyệt đối
không đụng cột trạng thái vòng đời (`Status`/`Enabled`) trong UPDATE đồng bộ này — dễ vô tình
reset trạng thái đã redeem/dùng.

> Ví dụ thực tế: `CpnVchBOMCodeIssue` (`Source='COUPON'|'SAP'`) — `docs/sql/SetupCoupon_Save.sql`
> (Section 3/3b/3c) + `docs/sql/Voucher_Save.sql`/`Voucher_Read.sql` (`usp_Voucher_*` không lọc
> Source vì `Code` đã unique toàn bảng).

---

## Pattern: SP đổi Status trên bảng có cột `Counter` đồng bộ POS

> Áp dụng khi: SP update 1 bảng thuộc nhóm `Offer*` (hoặc bảng tương tự có cột `Counter bigint`
> dùng cho cơ chế delta-sync xuống ~5.000 máy POS — xem `docs/architecture/database-schema.md`
> mục `OfferHeader`). Mọi lần ghi lên các bảng này **bắt buộc** tăng `Counter` để POS nhận biết
> thay đổi (client so `Counter` mới với `LastCounter` đã lưu theo từng store để quyết định có
> cần tải lại hay không) — quên tăng `Counter` = thay đổi "vô hình" với POS dù DB đã đổi đúng.

```sql
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @NewCounter bigint;
    SELECT  @NewCounter = ISNULL(MAX([Counter]), 0) + 1
    FROM    dbo.{Table} WITH (UPDLOCK, HOLDLOCK);   -- khóa đọc-rồi-ghi, tránh 2 request giành cùng 1 Counter

    UPDATE dbo.{Table}
    SET    [Status] = @NewStatus, [Counter] = @NewCounter
    WHERE  [No] = @Key;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
```

- **KHÔNG** tính `MAX(Counter)+1` ở tầng C# rồi UPDATE riêng — race-condition khi nhiều request
  ghi đồng thời (2 request đọc cùng MAX trước khi request đầu commit). Luôn làm trong 1 SP,
  `SELECT MAX ... WITH (UPDLOCK, HOLDLOCK)` cùng transaction với `UPDATE`.
- Giá trị `Status` con số cụ thể (0/1/2...) tra đúng trong doc nghiệp vụ liên quan (vd
  `docs/web/logic/LOGIC_APPROVE_CTKM.md`) — **không suy đoán theo tên biến** (`Status=0` từng bị
  hiểu nhầm là "tắt" trong khi thực ra là Active, xem case `usp_OfferHeader_Deactivate`).

> Ví dụ thực tế: `docs/sql/OfferHeader_Deactivate.sql` (`usp_OfferHeader_Deactivate`).

---

## Pattern: SP "tạo lần đầu" (guard tồn tại) vs SP "phát hành thêm" (append, không guard)

> Áp dụng khi: 1 nghiệp vụ có 2 giai đoạn — (1) tạo header + phát sinh dữ liệu con **lần đầu**
> (SP đã có sẵn 1 guard `IF NOT EXISTS` để tránh phát sinh trùng khi lỡ gọi lại), và sau đó (2)
> nghiệp vụ mới yêu cầu cho phép **thêm dữ liệu con nhiều lần nữa** vào cùng header đó. Guard của
> SP giai đoạn (1) sẽ **âm thầm bỏ qua** insert nếu gọi lại với header đã có dữ liệu con — KHÔNG
> tái dùng được cho giai đoạn (2). Giải pháp: viết **SP mới riêng** cho hành động "thêm", không có
> guard tồn tại, luôn insert dữ liệu con mới; SP giai đoạn (1) giữ nguyên không đổi.

```sql
-- SP giai đoạn (1) — đã có, giữ nguyên, vẫn còn guard (chỉ chạy 1 lần khi header mới):
-- IF NOT EXISTS (SELECT 1 FROM dbo.{ChildTable} WHERE {Key}=@Key AND Source='X')
-- BEGIN INSERT ... END

-- SP mới cho giai đoạn (2) — usp_{Domain}_{Action}More — KHÔNG có guard, luôn insert:
CREATE PROCEDURE dbo.usp_{Domain}_IssueMore
(
    @{Key}            nvarchar(20),      -- BẮT BUỘC đã tồn tại — không tạo header mới
    @NewRows          dbo.{Name}TVP READONLY,
    @OutQuantityAdded int OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.{HeaderTable} WHERE {Key}=@{Key})
        ;THROW 50002, N'Không tìm thấy header (Key không tồn tại)', 1;

    BEGIN TRY
        BEGIN TRANSACTION;
        INSERT INTO dbo.{ChildTable} (...)
        SELECT ... FROM @NewRows;              -- luôn insert, không kiểm tra đã có dữ liệu chưa
        SET @OutQuantityAdded = @@ROWCOUNT;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
```

- `Counter` (nếu bảng con có cột đồng bộ POS) dùng **chung 1 giá trị `MAX(Counter)+1` cho cả lô**
  mới thêm — khớp quy ước "1 Counter/lô" của SP giai đoạn (1), KHÔNG tăng theo từng dòng.
- `@{Key}` không tồn tại → `THROW` lỗi nghiệp vụ rõ ràng — không được tự tạo header mới trong SP
  "thêm" (tách biệt rõ trách nhiệm 2 SP).
- Nếu header có field kiểu "giới hạn tổng số dòng con" (vd `LimitQty`) — SP "thêm" nên cộng dồn
  (dòng con hiện có + dòng mới) so với giới hạn đó trước khi insert, tránh vượt trần không kiểm
  soát (dễ bị bỏ sót vì SP giai đoạn (1) không cần logic này — chỉ gọi 1 lần).

> Ví dụ thực tế: `docs/sql/SetupVoucher_SaveIssue.sql` (`usp_SetupVoucher_SaveIssue`, có guard,
> giữ nguyên) vs `docs/sql/SetupVoucher_IssueMore.sql` (`usp_SetupVoucher_IssueMore`, SP mới,
> không guard) — nghiệp vụ "phát hành nhiều lần từ 1 mã phát hành Voucher".

---

## Checklist tạo SP mới

1. Tên SP: `dbo.usp_{Domain}_{Action}` — tên TVP (nếu có): `dbo.{Name}TVP`.
2. Tra đúng tên bảng/cột trong `docs/architecture/database-schema.md` trước khi viết SQL.
3. Viết script trong `docs/sql/{Domain}_{Action}.sql` — có `TRY/CATCH` + `XACT_ABORT` +
   rollback cho SP ghi dữ liệu.
4. Sửa Repository gọi qua `DynamicParameters` + `CommandType.StoredProcedure` (không còn
   Dapper inline INSERT/UPDATE nhiều câu rời rạc cho cùng 1 nghiệp vụ).
5. Build + `dotnet test tests/POS.ContractTests` phải xanh.
6. Báo cho user (hoặc ghi vào `docs/ROLLOUT.md`) **chạy script 1 lần** trên
   `RPOSMasterData` trước khi tính năng hoạt động — app KHÔNG tự tạo SP.
