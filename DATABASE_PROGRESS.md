# DATABASE MIGRATION: MS SQL Server → PostgreSQL

## 1. LỜI GỌI HỆ THỐNG DÀNH CHO AI AGENT (BẮT BUỘC ĐỌC)
- Khi thực hiện bất kỳ thay đổi liên quan đến database layer, AI Agent **BẮT BUỘC** đọc file này trước.
- Sau khi sửa xong một file repository/caching, **BẮT BUỘC** cập nhật trạng thái ở Section 5.

---

## 2. TRẠNG THÁI TỔNG QUAN

- **Tiến độ:** **0 %** — Toàn bộ code hiện tại vẫn dùng MS SQL syntax
- **Cập nhật lần cuối:** 2026-06-03 (+07:00)

---

## 3. NHỮNG GÌ CẦN THAY ĐỔI — PHÂN TÍCH ĐẦY ĐỦ

### 3.1 Driver / NuGet Package

| Thay đổi | File |
|---|---|
| **Xóa** `Microsoft.Data.SqlClient` | `Infrastructure.csproj`, `LoyaltyRepository.cs` |
| **Xóa** `System.Data.SqlClient` | `OfferRepository.cs` |
| **Thêm** `Npgsql` (v8+) hoặc `Npgsql.DependencyInjection` | `Infrastructure.csproj` |
| Dapper: **không cần đổi** — works natively với Npgsql | — |

### 3.2 SqlConnectionFactory — đổi driver

```csharp
// TRƯỚC (MS SQL)
using Microsoft.Data.SqlClient;
return new SqlConnection(connectionString);

// SAU (PostgreSQL)
using Npgsql;
return new NpgsqlConnection(connectionString);
```

**File:** `src/Infrastructure/Persistence/SqlConnectionFactory.cs`

### 3.3 Exception type

```csharp
// TRƯỚC
catch (SqlException e) { ... }

// SAU
catch (NpgsqlException e) { ... }
// Hoặc dùng DbException (generic, không cần import Npgsql trực tiếp)
catch (System.Data.Common.DbException e) { ... }
```

**File ảnh hưởng:** `LoyaltyRepository.cs` (line 136)

### 3.4 Connection string format

```json
// TRƯỚC (appsettings.json — MS SQL)
"DAPPER_CENTRAL_MD": "Server=HOST;Database=DBNAME;User Id=USER;Password=PASS;"

// SAU (PostgreSQL)
"DAPPER_CENTRAL_MD": "Host=HOST;Port=5432;Database=DBNAME;Username=USER;Password=PASS;"
```

---

## 4. RULES BẮT BUỘC — SQL SYNTAX CONVERSION

### Rule 1: `(NOLOCK)` / `WITH (NOLOCK)` → XÓA HOÀN TOÀN

PostgreSQL không có khái niệm NOLOCK hint. Cần xóa toàn bộ.

```sql
-- TRƯỚC (MS SQL)
SELECT * FROM StoreMapping (NOLOCK)
SELECT * FROM POSTerminals WITH (NOLOCK)

-- SAU (PostgreSQL)
SELECT * FROM StoreMapping
SELECT * FROM POSTerminals
```

**Số lần xuất hiện:** ~20 queries trên toàn bộ Persistence + Caching layer.

---

### Rule 2: `ISNULL(expr, default)` → `COALESCE(expr, default)`

```sql
-- TRƯỚC
ISNULL(StatementMethod, 0)
ISNULL(@Transaction, '')
ISNULL(StoreNo, '')

-- SAU
COALESCE(StatementMethod, 0)
COALESCE(@Transaction, '')
COALESCE(StoreNo, '')
```

**File ảnh hưởng:** `LoyaltyRepository.cs`, `ValidateRepository.cs`, `PosTerminalRepository.cs`, `MemoryCacheService.cs`

---

### Rule 3: `IIF(cond, trueVal, falseVal)` → `CASE WHEN ... THEN ... ELSE ... END`

```sql
-- TRƯỚC
IIF(ISNULL(B.RemnValue, 0) > 0, (A.MaxValue - ISNULL(B.RemnValue, 0)), 0) UsedValue

-- SAU
CASE WHEN COALESCE(B.RemnValue, 0) > 0
     THEN (A.MaxValue - COALESCE(B.RemnValue, 0))
     ELSE 0 END AS UsedValue
```

**File ảnh hưởng:** `ValidateRepository.cs` (line 91-92), `MemoryCacheService.cs` (line 80)

---

### Rule 4: `TOP N` → `LIMIT N`

```sql
-- TRƯỚC
SELECT TOP 1 * FROM LoggingLoyalty ORDER BY CrtDate DESC
SELECT TOP 1 * FROM OfferStaffSetup ...

-- SAU
SELECT * FROM LoggingLoyalty ORDER BY CrtDate DESC LIMIT 1
SELECT * FROM OfferStaffSetup ... LIMIT 1
```

**File ảnh hưởng:** `LoyaltyRepository.cs`, `OfferRepository.cs`, `PosTerminalRepository.cs`

---

### Rule 5: `GETDATE()` / `getdate()` → `NOW()`

```sql
-- TRƯỚC
getdate()
CAST(getdate() AS DATE)

-- SAU
NOW()
CURRENT_DATE   -- hoặc NOW()::date
```

**File ảnh hưởng:** `ValidateRepository.cs`, `WinCodeRepository.cs`, `MemoryCacheService.cs`, `OfferRepository.cs`

---

### Rule 6: `[dbo].[TableName]` và `[ColumnName]` — bỏ dấu ngoặc vuông

```sql
-- TRƯỚC
FROM [dbo].[LoggingLoyalty]
SELECT [ActionType], [Status]
INSERT INTO [dbo].[WinCodeCustomer] ([ID], [WinCode], ...)

-- SAU
FROM loggingLoyalty                 -- hoặc public.loggingLoyalty
SELECT action_type, status          -- tuỳ convention đặt tên cột
INSERT INTO wincustomer (id, wincode, ...)
```

> **Lưu ý:** PostgreSQL phân biệt chữ hoa/thường khi dùng `"double quotes"`. Nên chuyển toàn bộ tên bảng/cột sang **lowercase_snake_case** và bỏ quotes.

---

### Rule 7: Từ khóa dành riêng (Reserved Words)

Các cột hiện tại trùng với reserved word của PostgreSQL — cần đổi tên hoặc luôn dùng `"double quotes"`:

| Tên cột hiện tại | Vấn đề | Giải pháp |
|---|---|---|
| `[Month]` | `month` là từ khóa PostgreSQL | Đổi tên → `period_month` hoặc dùng `"month"` |
| `[Value]` | `value` là từ khóa trong một số context | Đổi tên → `config_value` hoặc dùng `"value"` |
| `[Status]` | Không phải reserved nhưng ambiguous | Dùng tên rõ ràng hơn |
| `[Year]` | Nếu tồn tại trong schema | Đổi tên → `period_year` |
| `[Transaction]` | Là SQL keyword | Đổi tên → `transaction_data` hoặc dùng `"transaction"` |

---

### Rule 8: Stored Procedures → PostgreSQL Functions

PostgreSQL dùng **Functions** (không phải Procedures cho việc SELECT) và cú pháp gọi khác:

```sql
-- TRƯỚC (MS SQL - EXEC)
EXEC SP_API_TransactionQtyUse @ArticleNo, @SiteCode
EXEC SP_API_GetBusinessDate @SiteCode
EXEC SP_API_InsertBussinessDateOpen @Code, @StoreNo, @BussinessDate, @CreatedUser, @CreatedDate
[dbo].[SP_CpnVchCodeSend] @ItemNo,@StoreNo,@PosID,@OrderNo,@PhoneNumber,@QtyOfDay,@LimitQty
EXEC SP_API_GetAndUseGiftCode @OrderNo, @MemberCard, @Amount, @SaleType

-- SAU (PostgreSQL - CALL hoặc SELECT)
-- Nếu function trả kết quả (RETURNS TABLE hoặc RETURNS SETOF):
SELECT * FROM sp_api_transaction_qty_use(@article_no, @site_code)
SELECT * FROM sp_api_get_business_date(@site_code)
SELECT * FROM sp_api_insert_bussiness_date_open(@code, @store_no, @bussiness_date, @created_user, @created_date)

-- Nếu là void procedure:
CALL sp_api_insert_bussiness_date_open($1, $2, $3, $4, $5)
```

> **QUAN TRỌNG:** Tất cả `SP_API_*` stored procedures trong MS SQL phải được **tạo lại** trên PostgreSQL dưới dạng **functions** hoặc **procedures**. Đây là công việc lớn nhất.

**Danh sách đầy đủ SP cần tạo lại trên PostgreSQL:**

| SP Name (MS SQL) | File gọi | Ghi chú |
|---|---|---|
| `SP_API_TransactionQtyUse` | `CommonRepository.cs` | |
| `SP_API_GetBusinessDate` | `CommonRepository.cs` | |
| `SP_API_InsertBussinessDateOpen` | `CommonRepository.cs` | |
| `SP_API_InsertSignalStore` | `CommonRepository.cs` | |
| `SP_API_GET_SHIFT_HEADER` | `CommonRepository.cs` | |
| `SP_API_POSMonitorInsert` | `CommonRepository.cs` | |
| `SP_API_GetDataSetup` | `CommonRepository.cs` | |
| `SP_API_GetPOSVersion` | `CommonRepository.cs` | |
| `SP_API_CheckSaleReturn` | `CommonRepository.cs` | |
| `SP_API_GetOrderInfo` | `CommonRepository.cs` | |
| `SP_API_ListPOSDocumentNo` | `CommonRepository.cs` | |
| `SP_API_CheckCouponLine` | `CommonRepository.cs` | |
| `SP_API_InsertLineOrig_UpdateOrderInfo` | `CommonRepository.cs` | |
| `SP_API_GetInsurance` | `CommonRepository.cs` | |
| `SP_API_UpdatePOSEOD` | `CommonRepository.cs` | |
| `SP_API_GetAndUseGiftCode` | `LoyaltyRepository.cs` | |
| `SP_CpnVchCodeSend` | `SAPService.cs` | |

---

### Rule 9: Cross-Database Queries → KHÔNG HỖ TRỢ TRỰC TIẾP

PostgreSQL không hỗ trợ query trực tiếp sang database khác (khác với SQL Server dùng linked server). Các query này cần được xử lý khác:

| Query hiện tại | File | Giải pháp |
|---|---|---|
| `EInvoice.[dbo].[InvoiceCreated_Temp]` | `ValidateRepository.cs` | Move sang cùng DB, hoặc dùng `postgres_fdw` extension |
| `EInvoice.[dbo].Cash_OrderNoSentToEInvoice` | `ValidateRepository.cs` | Như trên |
| `CentralGeneral.dbo.StoreSetServer` | `MemoryCacheService.cs` | Merge schema hoặc dùng `postgres_fdw` |

> **Khuyến nghị:** Trong PostgreSQL, gộp các bảng vào cùng 1 database với nhiều **schema** (thay vì nhiều database). Ví dụ: `einvoice.invoicecreated_temp` thay vì `EInvoice.dbo.InvoiceCreated_Temp`.

---

### Rule 10: Data Types

| MS SQL Type | PostgreSQL Type |
|---|---|
| `UNIQUEIDENTIFIER` | `UUID` |
| `NVARCHAR(n)` | `VARCHAR(n)` hoặc `TEXT` |
| `DATETIME` | `TIMESTAMP` |
| `BIT` | `BOOLEAN` |
| `INT IDENTITY(1,1)` | `SERIAL` hoặc `GENERATED ALWAYS AS IDENTITY` |
| `MONEY` | `NUMERIC(19,4)` |
| `IMAGE` / `VARBINARY` | `BYTEA` |
| `TINYINT` | `SMALLINT` |

---

### Rule 11: CAST / Date Functions

```sql
-- TRƯỚC
CAST(getdate() AS DATE)
CAST(ApplyFrom AS DATE)
DATEADD(day, 7, getdate())
DATEDIFF(minute, StartDate, getdate())

-- SAU
CURRENT_DATE
ApplyFrom::date
NOW() + INTERVAL '7 days'
EXTRACT(EPOCH FROM (NOW() - StartDate)) / 60
```

---

### Rule 12: Parameter syntax trong Dapper

```csharp
// Dapper với Npgsql: vẫn dùng @param (Dapper tự convert sang $1, $2)
// KHÔNG cần thay đổi C# code, Dapper xử lý tự động
db.Query<T>("SELECT * FROM table WHERE id = @id", new { id = 1 });
// ✅ Works với Npgsql
```

> Dapper 2.x tự động map `@param` → `$1` khi dùng với Npgsql. Không cần sửa C# code phần này.

---

## 5. TIẾN ĐỘ SỬA TỪNG FILE

### 5.1 Infrastructure/Persistence

| File | MSSQL Issues | Trạng thái |
|---|---|---|
| `SqlConnectionFactory.cs` | `SqlConnection` → `NpgsqlConnection`; `Microsoft.Data.SqlClient` | ⏳ Chưa |
| `LoyaltyRepository.cs` | `(NOLOCK)` ×6, `[dbo].` ×5, `TOP 1`, `ISNULL` ×1, `SqlException`, `Microsoft.Data.SqlClient` | ⏳ Chưa |
| `CommonRepository.cs` | `EXEC SP_*` ×17 | ⏳ Chưa |
| `ValidateRepository.cs` | `(NOLOCK)` ×6, `[dbo].` ×3, `ISNULL` ×2, `IIF` ×2, `getdate()` ×1, cross-DB ×3, `[Month]` ×3 | ⏳ Chưa |
| `WinCodeRepository.cs` | `(NOLOCK)` ×2, `[dbo].` ×2, `getdate()` ×2 | ⏳ Chưa |
| `PosTerminalRepository.cs` | `TOP 1`, `WITH (NOLOCK)`, `ISNULL` ×4 | ⏳ Chưa |
| `OfferRepository.cs` | `(NOLOCK)` ×2, `[dbo].`, `TOP 1`, `CAST(getdate() AS DATE)` ×2, `System.Data.SqlClient` | ⏳ Chưa |

### 5.2 Infrastructure/Caching

| File | MSSQL Issues | Trạng thái |
|---|---|---|
| `MemoryCacheService.cs` | `(NOLOCK)` ×12, `ISNULL` ×5, `IIF` ×1, `NOLOCK` ×5, `WITH (NOLOCK)`, `getdate()` ×3, cross-DB ×2, `[...]` brackets | ⏳ Chưa |

### 5.3 Application/Services

| File | MSSQL Issues | Trạng thái |
|---|---|---|
| `SAPService.cs` | `SP_CpnVchCodeSend` (stored procedure call) | ⏳ Chưa |
| `OfferService.cs` | `SqlException` string check | ⏳ Chưa |

### 5.4 Stored Procedures (cần tạo mới trên PostgreSQL)

| SP Name | Trạng thái |
|---|---|
| `SP_API_TransactionQtyUse` | ⏳ Chưa tạo |
| `SP_API_GetBusinessDate` | ⏳ Chưa tạo |
| `SP_API_InsertBussinessDateOpen` | ⏳ Chưa tạo |
| `SP_API_InsertSignalStore` | ⏳ Chưa tạo |
| `SP_API_GET_SHIFT_HEADER` | ⏳ Chưa tạo |
| `SP_API_POSMonitorInsert` | ⏳ Chưa tạo |
| `SP_API_GetDataSetup` | ⏳ Chưa tạo |
| `SP_API_GetPOSVersion` | ⏳ Chưa tạo |
| `SP_API_CheckSaleReturn` | ⏳ Chưa tạo |
| `SP_API_GetOrderInfo` | ⏳ Chưa tạo |
| `SP_API_ListPOSDocumentNo` | ⏳ Chưa tạo |
| `SP_API_CheckCouponLine` | ⏳ Chưa tạo |
| `SP_API_InsertLineOrig_UpdateOrderInfo` | ⏳ Chưa tạo |
| `SP_API_GetInsurance` | ⏳ Chưa tạo |
| `SP_API_UpdatePOSEOD` | ⏳ Chưa tạo |
| `SP_API_GetAndUseGiftCode` | ⏳ Chưa tạo |
| `SP_CpnVchCodeSend` | ⏳ Chưa tạo |

---

## 6. THỨ TỰ ƯU TIÊN THỰC HIỆN

```
Phase 1 — Driver & Factory (làm trước, không đổi SQL):
  ├── SqlConnectionFactory.cs     → thay SqlConnection → NpgsqlConnection
  ├── LoyaltyRepository.cs        → thay SqlException import
  ├── OfferRepository.cs          → thay System.Data.SqlClient import
  └── appsettings.json            → đổi connection string format

Phase 2 — SQL Syntax (theo Rule 1-7, Rule 10-11):
  ├── MemoryCacheService.cs       → nhiều nhất: NOLOCK ×17, ISNULL ×5, IIF ×1, getdate() ×3
  ├── ValidateRepository.cs       → IIF ×2, cross-DB cần giải quyết kiến trúc
  ├── LoyaltyRepository.cs        → TOP 1, ISNULL, NOLOCK
  ├── WinCodeRepository.cs        → NOLOCK, getdate()
  ├── PosTerminalRepository.cs    → TOP 1, ISNULL, NOLOCK
  └── OfferRepository.cs          → TOP 1, getdate(), NOLOCK

Phase 3 — Cross-Database (cần quyết định kiến trúc):
  ├── ValidateRepository.cs       → EInvoice.dbo.* → schema einvoice.*
  └── MemoryCacheService.cs       → CentralGeneral.dbo.* → schema centralgeneral.*

Phase 4 — Stored Procedures (công việc lớn nhất):
  ├── Tạo lại 17 SP trên PostgreSQL dưới dạng Functions
  └── Cập nhật call syntax trong CommonRepository.cs, LoyaltyRepository.cs, SAPService.cs
```

---

## 7. QUICK REFERENCE CHEAT SHEET

```sql
-- MS SQL                          → PostgreSQL
(NOLOCK)                           → [XÓA]
WITH (NOLOCK)                      → [XÓA]
ISNULL(x, y)                       → COALESCE(x, y)
IIF(cond, a, b)                    → CASE WHEN cond THEN a ELSE b END
TOP N                              → LIMIT N (đặt cuối query)
GETDATE()                          → NOW()
CAST(x AS DATE)                    → x::DATE  hoặc  CAST(x AS DATE)
NEWID()                            → gen_random_uuid()
[dbo].[TableName]                  → public.tablename (hoặc tablename)
[ColumnName]                       → column_name (lowercase, không quotes)
NVARCHAR(n)                        → VARCHAR(n) hoặc TEXT
UNIQUEIDENTIFIER                   → UUID
INT IDENTITY(1,1)                  → SERIAL hoặc GENERATED ALWAYS AS IDENTITY
EXEC SP_Name @p1, @p2              → SELECT * FROM sp_name($1, $2)
DATEADD(day, n, d)                 → d + INTERVAL 'n days'
DATEDIFF(minute, d1, d2)           → EXTRACT(EPOCH FROM d2-d1)/60
FOR XML PATH                       → STRING_AGG() hoặc json_agg()
EInvoice.dbo.TableName             → einvoice.tablename (sau khi merge schema)
```

---

> **NOTE:** File này là "bộ nhớ kỹ thuật" cho database migration. Mọi thay đổi SQL liên quan đến PostgreSQL **phải** tham chiếu các rules ở đây. Cập nhật Section 5 sau mỗi file hoàn thành.
