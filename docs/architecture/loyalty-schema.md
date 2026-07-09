# Database Schema — RPOSLoyalty

> **Nguồn**: `docs/sql/database/Loyalty.sql` (script tạo DB `RPOSLoyalty`, generated 7/8/2026).
> **Mục đích tài liệu này**: bản đồ tra cứu **tên bảng / tên cột / kiểu dữ liệu / PK** để viết
> query/SP/Repository (`ILoyaltyRepository`/`LoyaltyRepository`) mapping chính xác tuyệt đối —
> **KHÔNG suy đoán tên cột**.
>
> ## ⚠️ QUY TẮC BẮT BUỘC
> Trước khi viết bất kỳ SQL query, stored procedure, hoặc Repository method nào đụng tới các
> bảng trong `RPOSLoyalty`, **BẮT BUỘC đối chiếu file này trước** để lấy đúng tên bảng, tên cột,
> kiểu dữ liệu, độ dài. Bảng cần dùng chưa có trong tài liệu này → đọc lại
> `docs/sql/database/Loyalty.sql` (hoặc script cập nhật mới nhất, lưu ý file gốc là UTF-16 — dùng
> `Get-Content -Encoding Unicode` khi đọc bằng PowerShell), sau đó bổ sung vào file này trong cùng
> commit.
>
> Xem thêm: [`centralMD-schema.md`](centralMD-schema.md) (RPOSMasterData/CentralMD),
> [`centralsale-schema.md`](centralsale-schema.md) (RPOSCentralSales).

## Mục lục

| Domain | Bảng |
|---|---|
| [Logging](#logging) | LoggingLoyalty |

---

## Logging

### LoggingLoyalty
PK: `(AppCode, OrderNo, TransactionType, ActionType)` (composite, clustered)
```
AppCode             varchar(10)     NOT NULL
OrderNo             varchar(30)     NOT NULL
TransactionType     int             NOT NULL
OrigOrderNo         varchar(30)     NOT NULL
MemberCardNo        varchar(50)     NOT NULL
CustName            nvarchar(500)   NULL
ActionType          varchar(50)     NOT NULL
LoyaltyPoints       bigint          NOT NULL
Transaction         nvarchar(50)    NOT NULL   -- tên cột trùng reserved keyword "TRANSACTION" → BẮT BUỘC bracket-quote [Transaction] trong SQL
Status              varchar(10)     NOT NULL
Request             nvarchar(max)   NOT NULL
Response            nvarchar(max)   NOT NULL
Items               nvarchar(max)   NULL
CrtDate             datetime        NOT NULL
StoreNo             varchar(10)     NULL   -- PENDING: chưa có trong docs/sql/database/Loyalty.sql (script gốc 2026-07-08).
                                            -- Sẽ được DBA bổ sung thủ công (xác nhận 2026-07-09) để phục vụ row-level
                                            -- filter theo cửa hàng cho StoreOperator. IRptLoyaltyRepository.GetInvoiceLoyaltyListAsync
                                            -- (RptLoyaltyRepository.cs) đã viết WHERE StoreNo=@StoreNo giả định cột này tồn tại —
                                            -- SẼ LỖI "Invalid column name 'StoreNo'" cho tới khi DBA apply ALTER TABLE thêm cột.
```
> Bảng log giao dịch loyalty gửi đi/nhận về (Request/Response raw JSON hoặc XML) theo từng
> `OrderNo` + `TransactionType` + `ActionType`. Không có cột `Counter`/`Pkey` — bảng này không
> đồng bộ xuống POS (chỉ log nội bộ).

---

## Stored Procedures

Chưa có SP nào trong script `Loyalty.sql` — DB `RPOSLoyalty` hiện chỉ có 1 bảng thuần (không
kèm SP). Khi tạo SP mới cho DB này, áp dụng cùng convention `dbo.usp_{Domain}_{Action}` mô tả
trong `.claude/skills/database/SKILLS.md`.
