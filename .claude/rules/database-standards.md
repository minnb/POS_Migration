# Rule: Database Standards — Stored Procedure / SQL conventions

## 🎯 Context (Khi nào áp dụng)
Khi viết/sửa/refactor bất kỳ stored procedure, script `docs/sql/*.sql`, hoặc Dapper inline query
đụng `RPOSMasterData` / `RPOSCentralSales` / `RPOSLoyalty`. Đây là **tiêu chuẩn bắt buộc**
(WHAT/WHY). Template SP, code mẫu Dapper/TVP và các "Pattern:" thực thi nằm ở
**`.claude/skills/database/SKILLS.md`**.

## ✅ DO (Bắt buộc làm)
- **Reserved keyword — bắt buộc bracket-quote `[ ]`**: mọi identifier (cột/bảng) trùng từ khoá
  reserved của SQL Server phải viết `[TênCột]`, trong SP mới, script `docs/sql/*.sql`, và Dapper
  inline. Áp dụng cho SELECT / INSERT column-list / ORDER BY / JOIN / GROUP BY. Cột hay sót:
  `[LineNo]`, `[Source]`, `[Status]`, `[Counter]`, `[No]`.
  > Dấu hiệu: lỗi **Msg 156** "Incorrect syntax near the keyword 'X'" mà cột `X` **có tồn tại
  > thật** → chắc chắn reserved keyword, bracket-quote ngay (khác lỗi **207** "Invalid column
  > name" = tên cột sai).
- **Đặt tên SP mới**: `dbo.usp_{Domain}_{Action}` — `usp_` prefix cố định, `{Domain}` +
  `{Action}` PascalCase (vd `usp_Product_Save`, `usp_SetupCoupon_CheckCodesExist`). TVP đi kèm:
  `dbo.{Name}TVP` (vd `dbo.ProductBarcodeTVP`).
  - SP đã tồn tại sẵn với tên cũ khác convention (`[SyncGetDataByTable]`, `GetProductList`...) →
    **giữ nguyên tên**, không đổi tên SP production chỉ để khớp convention.
- **Nguồn sự thật tên bảng/cột là file schema** — tra TRƯỚC khi viết SQL, KHÔNG suy đoán tên
  bảng/cột (kể cả số ít/số nhiều):

  | Database | File schema | Script gốc |
  |---|---|---|
  | `RPOSMasterData` (CentralMD) | `docs/architecture/centralMD-schema.md` | `docs/sql/database/CentralMD.sql` |
  | `RPOSCentralSales` | `docs/architecture/centralsale-schema.md` | `docs/sql/database/CentralSale.sql` (UTF-16) |
  | `RPOSLoyalty` | `docs/architecture/loyalty-schema.md` | `docs/sql/database/Loyalty.sql` (UTF-16) |

  Bảng chưa có trong doc → đọc script gốc (2 file UTF-16 đọc bằng PowerShell
  `Get-Content -Encoding Unicode -Raw`), bổ sung vào file schema đúng DB **cùng commit**.
- **Mọi SP ghi dữ liệu BẮT BUỘC**: `SET NOCOUNT ON` + `SET XACT_ABORT ON` + `BEGIN TRY/CATCH` +
  `IF XACT_STATE() <> 0 ROLLBACK TRANSACTION` khi lỗi + `THROW` (KHÔNG nuốt lỗi trong SP — để C#
  bắt qua `SqlException`).
- **Counter đồng bộ POS (bảng `Offer*` và bảng có cột `Counter bigint`)**: mọi lần ghi phải tăng
  `Counter`. Tính `MAX([Counter])+1` **trong 1 SP**, `SELECT ... WITH (UPDLOCK, HOLDLOCK)` cùng
  transaction với `UPDATE` — KHÔNG tính ở tầng C# rồi UPDATE riêng (race condition).
- **Single File Constraint** — mỗi SP chỉ có **đúng 1 file nguồn** trong `docs/sql/manifest.json`:
  - Track A (idempotent, `runOnce: false`): sửa trực tiếp file `.sql` hiện có (`DROP+CREATE`),
    giữ nguyên entry manifest.
  - Track B (one-shot rủi ro cao, `runOnce: true`): đổi tên file cũ thành `.sql.bak`, tạo file
    mới cùng tên gốc, entry manifest vẫn trỏ đúng 1 file (tên gốc), ghi note lý do rewrite.
- **Đăng ký `docs/sql/manifest.json`** cho mọi script CentralMD mới (`order`/`file`/`target`/
  `runOnce`) **cùng commit** — thiếu → `SqlManifestTests.cs` FAIL. SP idempotent →
  `runOnce: false` (Track A, `POS.DbMigrator` tự chạy). DDL một-lần rủi ro cao → `runOnce: true`
  (Track B, DBA chạy tay + ghi `docs/ROLLOUT.md`).
- **Connection factory**: chỉ dùng `StoreRoutedConnectionFactory` khi ghi vào bảng **sharded theo
  store** (TransHeader...); SP/bảng không phụ thuộc shard (audit log, master data) → ưu tiên
  `directConnectionFactory` (tránh thêm điểm lỗi mạng theo `StoreSetServer`).
- Giá trị `Status` con số cụ thể (0/1/2...) tra đúng trong doc nghiệp vụ liên quan — **không suy
  đoán theo tên biến** (`Status=0` có thể là Active, không phải "tắt").

## ❌ DON'T (Tuyệt đối cấm)
- Cấm để cột trùng reserved keyword không bracket-quote (gây Msg 156 lúc runtime).
- Cấm đặt tên SP mới theo dạng legacy (`sp_Article_Save`, `GetProductList`, `sp_ProductList_Get`).
- Cấm tạo file `.sql` thứ hai cho cùng 1 SP (`_v2`/`_new`/`_fix`) chạy song song; cấm để cả `.bak`
  lẫn file gốc cùng có mặt trong `manifest.json`.
- Cấm suy đoán tên bảng/cột theo convention DbSet (thêm/bớt "s") — luôn đối chiếu tên vật lý thật.
- Cấm nuốt lỗi trong SP ghi dữ liệu (thiếu `THROW`); cấm gọi SP legacy có `ROLLBACK` bằng
  `INSERT...EXEC` (dùng OUTPUT param).
- Cấm tính `MAX(Counter)+1` ở C# rồi UPDATE riêng (race condition đa request).

---

> Template SP ghi dữ liệu, pattern gọi Dapper/TVP, và các "Pattern:" (discriminator column,
> timeline merge, IssueMore, audit try/finally, optional filter UPDLOCK, OUTPUT param, code→name
> mapping): **`.claude/skills/database/SKILLS.md`** — đọc file đó để lấy code, KHÔNG lặp lại mandate
> ở đây.
