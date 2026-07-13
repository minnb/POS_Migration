---
name: database-stored-procedure
description: HOW viết SP cho RPOSMasterData/RPOSCentralSales/RPOSLoyalty — template SP ghi dữ liệu, gọi Dapper/TVP, và các Pattern Repository/SP (audit try/finally, UPDLOCK Counter, OUTPUT param, timeline merge). Rules (naming, reserved keyword, Single File, XACT_ABORT, manifest) ở .claude/rules/database-standards.md.
---

# Skill: Stored Procedure — RPOSMasterData / RPOSCentralSales / RPOSLoyalty

> **Áp dụng khi:** tạo mới bất kỳ stored procedure nào cho `RPOSMasterData` (CentralMD),
> `RPOSCentralSales`, hoặc `RPOSLoyalty`, hoặc chuyển 1 đoạn Dapper INSERT/UPDATE inline sang SP
> để dễ kiểm soát lỗi. Đọc file này TRƯỚC khi viết SP mới. Convention đặt tên/template bên dưới
> áp dụng chung cho cả 3 database — chỉ khác file schema tra cứu tên bảng/cột (xem mục "Nơi đặt
> script SQL").
>
> **Lưu ý riêng `RPOSCentralSales`**: DB này JOIN chéo sang `RPOSMasterData` trong nhiều SP hiện
> có (vd `Store`, `Item`, `Staff`, `MCH`) — SP mới nếu cần dữ liệu master, dùng
> `RPOSMasterData..{Table}` (three-part name), không tạo bản sao dữ liệu.
>
> **Rules (tiêu chuẩn bắt buộc — đọc TRƯỚC):** reserved keyword bracket-quote, naming
> `dbo.usp_{Domain}_{Action}` + TVP, Single File Constraint, `SET XACT_ABORT`+TRY/CATCH+THROW,
> Counter+UPDLOCK, manifest.json, schema-doc là nguồn sự thật tên bảng/cột, direct-vs-StoreRouted
> factory — xem **`.claude/rules/database-standards.md`**. File này chỉ giữ template + Pattern (HOW).

---

## Nơi đặt script SQL (HOW)

Mỗi SP mới (+ TVP đi kèm nếu có) viết thành 1 file `docs/sql/{Domain}_{Action}.sql`, có
`USE [TênDB];` đầu file. Ví dụ đã có: `docs/sql/Product_Save.sql`, `docs/sql/SetupCoupon_Save.sql`,
`docs/sql/SpecialCombo_Save.sql`, `docs/sql/SetupVoucher_Save.sql`.

> Bảng schema-file để tra tên bảng/cột + quy tắc manifest.json/Track A-B là **Rules** — xem
> `.claude/rules/database-standards.md`. 2 file `CentralSale.sql`/`Loyalty.sql` là UTF-16, đọc bằng
> PowerShell `Get-Content -Encoding Unicode -Raw`.

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

> `SET XACT_ABORT ON` + `BEGIN TRY/CATCH` + `ROLLBACK` + `THROW` là **Rule bắt buộc** cho mọi SP
> ghi dữ liệu — xem `.claude/rules/database-standards.md`.

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

## Pattern: SP xử lý chồng lấn khoảng ngày hiệu lực (timeline merge, set-based)

> Áp dụng khi: bulk save nhiều dòng "giá trị + khoảng ngày hiệu lực" (giá bán, khuyến mãi, tỷ giá...)
> mà khoảng mới có thể chồng lấn/cắt/nối các khoảng cũ đang active — cần tách lại thành các đoạn
> nguyên tử không chồng lấn, soft-delete đoạn cũ không còn hiệu lực, KHÔNG được lặp vòng lặp
> ROW-BY-ROW (chậm + dễ lệch transaction khi gọi SP legacy lồng nhau).

Kỹ thuật: 1 hàm `dbo.tvf_{Domain}_Timeline(@Json)` dựng lại toàn bộ timeline bằng CTE set-based:
1. Parse `@Json` (OPENJSON) → khoảng mới, `RowId` tăng dần làm độ ưu tiên (mới thắng cũ khi chồng lấn).
2. Gộp segment cũ (từ bảng thật, prio=0) + segment mới (prio=RowId) → tính **mọi điểm biên**
   (Start và End+1 của mọi segment) → cắt thành các **khoảng nguyên tử** không chồng lấn.
3. Mỗi khoảng nguyên tử lấy giá trị của segment có prio CAO NHẤT phủ nó (`CROSS APPLY TOP 1 ... ORDER BY prio DESC`).
4. Gộp lại các khoảng nguyên tử liền kề CÙNG giá trị (gap/island bằng `LAG()` + `SUM() OVER`) để tránh
   sinh thừa nhiều dòng nhỏ cho cùng 1 giá trị.
5. SP gọi hàm này rồi `MERGE` 1 lần vào bảng thật: `WHEN MATCHED` (đổi giá/ngày hoặc revive dòng đã
   soft-delete) → UPDATE tại chỗ; `WHEN NOT MATCHED BY TARGET` → INSERT; `WHEN NOT MATCHED BY SOURCE`
   (dòng active cũ không còn trong timeline mới) → soft-delete (`IsActive=0`), KHÔNG xóa cứng (giữ
   lịch sử + đồng bộ Counter cho client polling).

**KHÔNG làm:** gọi SP legacy trả result-set bên trong 1 vòng lặp T-SQL cursor (chậm, dễ lỗi lồng
transaction "mismatching BEGIN and COMMIT"); tự so sánh khoảng ngày bằng nhiều câu `IF EXISTS`
riêng lẻ cho từng trường hợp chồng lấn (dễ sót case, khó test).

> Ví dụ thực tế: `dbo.tvf_SetupSalePrice_Timeline` + `dbo.Setup_SalePrice_Get_ALL`
> (`docs/sql/SetupSalePrice_Save.sql`) — xử lý chồng lấn khoảng `StartingDate/EndingDate` khi bulk
> import bảng giá bán (9.3 Setup Giá).

---

## Pattern: SP đổi Status trên bảng có cột `Counter` đồng bộ POS

> Áp dụng khi: SP update 1 bảng thuộc nhóm `Offer*` (hoặc bảng tương tự có cột `Counter bigint`
> dùng cho cơ chế delta-sync xuống ~5.000 máy POS — xem `docs/architecture/centralMD-schema.md`
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

## Pattern: Audit log table với try/finally trong Repository

> Áp dụng khi: cần ghi audit log sau mỗi lần insert/process data, bất kể thành công hay thất bại, kể cả khi có nhiều return path.

```csharp
// Khai báo tracking variables TRƯỚC try
bool   _flag     = false;
string _errorMsg = "";
string _dataType = "";
try
{
    // ... logic chính, cập nhật _flag/_errorMsg/_dataType ở mỗi nhánh ...
    _flag = true;
    return (true, "OK");
}
catch (Exception ex) { _errorMsg = ex.Message; return (false, _errorMsg); }
finally
{
    // finally LUÔN chạy — đảm bảo log dù return ở nhánh nào
    await InsertDataRawJsonAsync(transactionId, _dataType, message, _flag,
        _flag ? null : _errorMsg);
}

private async Task InsertDataRawJsonAsync(string transactionId, string dataType,
    string message, bool flag, string? errorMessage)
{
    try
    {
        using var conn = await directConnectionFactory.CreateOpenConnectionAsync(
            CancellationToken.None);  // CancellationToken.None — log phải chạy kể cả request bị cancel
        await conn.ExecuteAsync(new CommandDefinition(sql, new { ... }, commandTimeout: Timeout));
    }
    catch { /* Swallow — nếu log fail, main processing đã fail → RabbitMQ retry tự động */ }
}
```

> Ví dụ thực tế: `src/POS.Infrastructure/Repositories/CentralSaleRepository.cs` — `InInsertToTableByJson` + `InsertDataRawJsonAsync`

**Anti-pattern:** Gọi log function ở từng return path riêng lẻ → dễ bỏ sót khi thêm nhánh mới.

**Gotcha (2026-07-08):** `InInsertToTableByJson` từng mở connection chính (không phải audit log)
qua `StoreRoutedConnectionFactory` (route theo `StoreSetServer`) — khi `ServerIP` của 1 store
không còn kết nối được trên UAT/Prod, method throw "network-related... SQL Server" dù các hàm đọc
cùng bảng vẫn chạy bình thường (chúng dùng `directConnectionFactory` cố định). Đã đổi sang luôn
dùng `directConnectionFactory`. Bài học: chỉ dùng `StoreRoutedConnectionFactory` khi thật sự cần
ghi vào bảng **sharded theo store** (TransHeader...); nếu SP/bảng đích không phụ thuộc shard, ưu
tiên `directConnectionFactory` để tránh thêm 1 điểm lỗi mạng không cần thiết.

---

## Pattern: Optional filter param trong UPDLOCK transaction

> Áp dụng khi: cần enforce thêm điều kiện (VoucherType, loại hàng, v.v.) bên trong transaction
> có UPDLOCK — check PHẢI nằm trong transaction, không thể làm ngoài (TOCTOU risk).

```csharp
// Interface — thêm optional param, caller cũ không bị break
Task<...> RedeemVouchersAsync(
    List<(string VoucherNumber, double AmountRedeem)> serials,
    string orderNo,
    string? requiredVoucherType = null,   // ← optional, default null = bỏ qua check
    CancellationToken ct = default);

// Repository — check ngay sau SELECT UPDLOCK, trước UPDATE
if (requiredVoucherType != null)
{
    var wrongType = vouchers.FirstOrDefault(v => v.VoucherType != requiredVoucherType);
    if (wrongType != null) { tx.Rollback(); return (false, $"Voucher ... không phải loại {requiredVoucherType}", []); }
}
```

> Anti-pattern: check VoucherType trước rồi mới gọi transaction → race condition giữa check và update.
> Ví dụ thực tế: `src/POS.Infrastructure/Repositories/SAPVoucherRepository.cs`

### Named CancellationToken khi thêm optional param vào giữa signature

> Áp dụng khi: thêm optional param mới vào giữa signature của method đang có caller dùng positional args.

```csharp
// Lỗi compile — CancellationToken truyền nhầm vào optional string? mới thêm:
repo.RedeemVouchersAsync(serials, orderNo, ct);     // ct → slot của string? ❌

// Đúng — dùng named param để CancellationToken vào đúng slot:
repo.RedeemVouchersAsync(serials, orderNo, ct: ct); // ✅
```

> Quy tắc: Khi thêm optional param vào giữa signature, scan toàn bộ callers và thêm `ct: ct` nếu cần.
> Ví dụ thực tế: `src/POS.Application/Services/SAPService.cs` — `RedeemCpnVchAsync`

---

## Pattern: Xác minh tên bảng vật lý trước khi viết raw SQL

> Áp dụng khi: viết raw SQL/SP call mới nhắm vào bảng đã tồn tại trong `RPOSMasterData`.
> Rút ra từ sự cố thực tế: `CentralMDRepository` từng dùng `dbo.POSTerminalBanks` và `dbo.Banks`
> (số nhiều — suy đoán theo convention EF DbSet cũ), trong khi tên bảng vật lý thật là
> `dbo.POSTerminalBank`, `dbo.Bank` (số ít). Query chạy thẳng vào production DB thật sẽ throw
> `Invalid object name` — chỉ phát hiện lúc runtime, không phải lúc build.

**Cách xác minh đúng — tra `docs/architecture/centralMD-schema.md` (nguồn sự thật schema DB
theo quy tắc ở `CLAUDE.md`), KHÔNG suy đoán tên bảng theo convention số ít/số nhiều:**
1. Mở `docs/architecture/centralMD-schema.md`, tìm đúng tên bảng + cột + kiểu dữ liệu + PK.
2. Bảng cần dùng chưa có trong doc → đọc `docs/sql/database/CentralMD.sql` (nguồn gốc sinh ra
   `centralMD-schema.md`) để lấy tên chính xác, rồi bổ sung vào `centralMD-schema.md` cùng commit.
3. **KHÔNG** tự thêm/bớt "s" theo thói quen đặt tên DbSet — luôn đối chiếu tên bảng vật lý thật.

> Ví dụ thực tế: `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs`
> (`GetBankPOSListAsync`/`SaveBankPOSAsync`/`DeleteBankPOSAsync` → `dbo.POSTerminalBank`;
> `GetBankListForDropdownAsync` → `dbo.Bank`)

---

## Pattern: Map SP trả cột đã format/localize sẵn (khác kiểu bảng vật lý)

> Áp dụng khi: gọi 1 SP có sẵn (không tự viết) mà SELECT convert cột sang dạng hiển thị
> (vd `IIF(Status=1, N'Đang dùng', N'Không dùng')`, `Format(Date,'dd/MM/yyyy')`,
> `Convert(varchar,Counter)`) — kiểu cột trả về KHÁC kiểu cột vật lý trong bảng, map thẳng
> vào DTO dùng kiểu vật lý (bool/int/DateTime) sẽ làm Dapper throw lỗi cast ngay dòng đầu tiên.

```csharp
// Repository — KHÔNG map thẳng vào DTO public (BankPOSListDto), dùng row riêng khớp đúng
// cột SP trả (text/string), rồi convert sang kiểu UI cần trong bước project.
var rows = await QueryAsync<BankPOSListRow>(sql, param, ct: ct);
return rows.Select(r => new BankPOSListDto
{
    IsOnline = r.IsOnline == "Có",                              // text tiếng Việt → bool
    Status   = r.Status == "Đang được sử dụng" ? 1 : 0,          // text tiếng Việt → int (round-trip Save)
    StatusText = r.Status,                                      // giữ nguyên text để hiển thị/export
    Counter  = r.Counter,                                       // varchar sẵn — giữ string, không ép int
}).ToList();

private sealed class BankPOSListRow { public string IsOnline {get;set;} = ""; /* ... khớp đúng tên+kiểu cột SP trả */ }
```

**Quan trọng:**
- Giữ nguyên field kiểu "gốc" (vd `Status` int) trên DTO nếu còn nơi khác (form Edit/Save) cần round-trip đúng kiểu đó — chỉ thêm field mới (`StatusText`) cho phần hiển thị, KHÔNG đổi kiểu field đang được dùng để ghi ngược lại DB.
- Dapper `QueryAsync<T>` không throw khi property DTO không có cột khớp (giữ default) — an toàn khi SP sau này thêm cột mới (vd thêm `PartnerId` vào SELECT) mà không cần sửa code map nếu đã khai báo sẵn field tương ứng.

> Ví dụ thực tế: `src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs` (`GetBankPOSListAsync` + `BankPOSListRow`)

---

## Pattern: SP trả kết quả qua OUTPUT param khi ủy quyền SP-legacy có result set

> Áp dụng khi: SP mới `EXEC` một SP legacy tự `SELECT` (vd Interface_Errors) và/hoặc có `ROLLBACK` bên trong.

Không thể hứng result set legacy bằng `INSERT...EXEC` nếu SP legacy có `ROLLBACK` ("Cannot use the
ROLLBACK statement within an INSERT-EXEC statement"). Nếu để result set legacy lọt ra, Dapper
`QueryFirstOrDefault<T>` đọc NHẦM set đầu → `null` → báo lỗi giả. Giải pháp: trả `@Ok bit/@Message`
qua **OUTPUT param**; repository dùng `ExecuteAsync` (ExecuteNonQuery nuốt hết result set rồi mới gán output).

```csharp
p.Add("@Ok", dbType: DbType.Boolean, direction: ParameterDirection.Output);
p.Add("@Message", dbType: DbType.String, size: 4000, direction: ParameterDirection.Output);
await conn.ExecuteAsync(new CommandDefinition("dbo.usp_X", p, commandType: CommandType.StoredProcedure));
var ok = p.Get<bool?>("@Ok") ?? false;
```
> Ví dụ thực tế: `docs/sql/SetupSalePrice_Save.sql`, `src/POS.Infrastructure/Repositories/Price/PriceRepository.cs` (`SaveAsync`).

---

## Pattern: SP đổi 1 cột từ mã (code) sang tên hiển thị (name) — luôn thêm cột mã gốc riêng cho composite key

> Áp dụng khi: sửa/mở rộng 1 SP list có sẵn để JOIN thêm bảng lookup và **thế** cột mã bằng cột tên hiển thị
> (vd `SalesCode` từ trả `PriceGroupCode` đổi sang trả `PriceGroupName` cho đẹp UI). Rút ra từ sự cố thực tế:
> `GetSalesPriceList` đổi `SalesCode` sang trả tên nhóm giá, nhưng code Sửa/Xóa (`PriceRowKey`) vẫn dùng
> đúng field đó làm khoá gửi tới `usp_SalesPrice_UpdatePrice`/`_SoftDelete` (đang lọc theo **mã**, không phải
> tên) → mọi thao tác Sửa/Xóa sẽ báo "Không tìm thấy dữ liệu" ngay khi Code ≠ Name.

**Quy tắc**: mỗi khi 1 cột SP đang được dùng làm khoá composite (Update/Delete/lookup ngược) bị đổi ý nghĩa
sang giá trị hiển thị, **PHẢI** thêm 1 cột mới song song mang mã gốc (lấy thẳng từ bảng vật lý, không qua
JOIN lookup có thể `LEFT JOIN` miss), map vào 1 field riêng trên DTO (đặt tên rõ ràng kiểu `XxxCode`, có
comment "KHÔNG hiển thị — dùng làm khoá"), rồi sửa nơi build khoá composite dùng field mã mới thay vì field
hiển thị cũ.

```sql
-- SP list — thêm cột mã gốc song song với cột tên hiển thị đã đổi
ISNULL(G.PriceGroupName,'') AS SalesCode,       -- tên hiển thị (đã đổi ý nghĩa)
ISNULL(S.[SalesCode],'')    AS SalesGroupCode,  -- mã gốc — LẤY THẲNG từ bảng vật lý, dùng cho Sửa/Xóa
```
```csharp
// DTO — field mã gốc tách riêng, comment rõ mục đích
public string? SalesCode { get; set; }       // tên hiển thị — cột lưới
public string? SalesGroupCode { get; set; }  // mã gốc — KHÔNG hiển thị, dùng build PriceRowKey
```

> Anti-pattern: tiếp tục dùng field cũ (`row.SalesCode`) để build khoá sau khi ý nghĩa cột đã đổi — lỗi
> không xuất hiện lúc build/test (kiểu vẫn là `string`), chỉ lộ ra khi chạy thật với dữ liệu có Code≠Name.
> Ví dụ thực tế: `docs/sql/GetSalesPriceList_AddSaleType.sql` (`SalesGroupCode`),
> `docs/sql/GetSalesPriceList_AddSalesTypeCode.sql` (`SalesTypeCode`),
> `src/POS.Web/Components/Pages/Catalog/Price/PricesPage.razor` (`TryBuildKey`).

---

## Checklist tạo SP mới

1. Tên SP: `dbo.usp_{Domain}_{Action}` — tên TVP (nếu có): `dbo.{Name}TVP` (Rule:
   `.claude/rules/database-standards.md`).
2. Tra đúng tên bảng/cột trong file schema đúng DB đích (bảng schema-file ở
   `.claude/rules/database-standards.md`) trước khi viết SQL.
3. Viết script trong `docs/sql/{Domain}_{Action}.sql`, có `USE [TênDB];` đầu file — có
   `TRY/CATCH` + `XACT_ABORT` + rollback cho SP ghi dữ liệu.
4. Sửa Repository gọi qua `DynamicParameters` + `CommandType.StoredProcedure` (không còn
   Dapper inline INSERT/UPDATE nhiều câu rời rạc cho cùng 1 nghiệp vụ).
5. Build + `dotnet test tests/POS.ContractTests` phải xanh.
6. **Đăng ký vào `docs/sql/manifest.json`** (thêm 1 entry: `order`, `file`, `target`, `runOnce`)
   **cùng commit** — thiếu bước này thì `tests/POS.ContractTests/SqlManifestTests.cs` sẽ FAIL
   ngay lúc `dotnet test` (đã verify: tạo file `.sql` không đăng ký → test đỏ tức thì). SP mới thường
   là idempotent (`DROP+CREATE`/`CREATE OR ALTER`) → `runOnce: false` (Track A, `POS.DbMigrator`
   tự chạy lại mỗi lần deploy — xem `docs/ROLLOUT.md` §D0). Chỉ đặt `runOnce: true` (Track B) cho
   DDL một-lần rủi ro cao (rebuild bảng, đổi dữ liệu, `sp_rename`) — loại này migrator KHÔNG BAO GIỜ
   tự chạy, vẫn cần báo DBA chạy tay + ghi vào `docs/ROLLOUT.md`.
