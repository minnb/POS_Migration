# Skill: Stored Procedure — RPOSMasterData (CentralMD)

> **Áp dụng khi:** tạo mới bất kỳ stored procedure nào cho `RPOSMasterData` (CentralMD),
> hoặc chuyển 1 đoạn Dapper INSERT/UPDATE inline sang SP để dễ kiểm soát lỗi. Đọc file này
> TRƯỚC khi viết SP mới.

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
