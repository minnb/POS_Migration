/* =====================================================================================
   Tối ưu sp_ReportSaleByTime / ReportSaleDetail cho quy mô ~10 triệu dòng
   Liên quan: src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor
             src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs (đã thêm Redis cache)
   DB:  CentralSales
   -------------------------------------------------------------------------------------
   File này là phần DB của remediation. Phần code (cache Redis + giảm timeout xuống 45s +
   tham số includeKpi) đã được triển khai trong repository. Hai mục dưới đây cần DBA áp dụng
   trực tiếp trên DB (cần xem body SP + schema bảng thật để điền đúng tên cột).
   ===================================================================================== */


/* -------------------------------------------------------------------------------------
   MỤC 3 (ƯU TIÊN CAO NHẤT) — Index trên ReportSaleDetail
   Đòn bẩy lớn nhất ở 10M dòng. Không có index này thì mỗi exec sp_ReportSaleByTime là
   một clustered-scan toàn bảng.

   BƯỚC 1 — Kiểm tra index hiện có và tên cột thật của bảng:
--------------------------------------------------------------------------------------- */
-- EXEC sp_helpindex 'dbo.ReportSaleDetail';
-- SELECT c.name, t.name AS data_type
-- FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id
-- WHERE c.object_id = OBJECT_ID('dbo.ReportSaleDetail') ORDER BY c.column_id;

/* BƯỚC 2 — Tạo index phủ (THAY tên cột cho khớp schema thật):
     - Cột lọc của SP:  @StoreNo, khoảng ngày (@FromDate/@ToDate)
     - INCLUDE các cột đo mà SP SUM/aggregate (gross, net, discount, vat, orders, qty...)
   Bỏ comment và chỉnh tên cột trước khi chạy: */

-- IF NOT EXISTS (SELECT 1 FROM sys.indexes
--                WHERE object_id = OBJECT_ID('dbo.ReportSaleDetail')
--                  AND name = 'IX_ReportSaleDetail_Store_Date')
-- CREATE NONCLUSTERED INDEX IX_ReportSaleDetail_Store_Date
--     ON dbo.ReportSaleDetail ( StoreNo, SaleDate )       -- <== sửa tên cột ngày/store thật
--     INCLUDE ( GrossRevenue, NetRevenue, TotalDiscount,  -- <== sửa danh sách cột đo thật
--               TotalVAT, TotalOrders, TotalReturnOrders, TotalQty )
--     WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);

/* BƯỚC 3 — Xác minh execution plan dùng index (Seek, không Scan):
     SET STATISTICS IO ON;
     EXEC dbo.sp_ReportSaleByTime @FromDate='2026-01-01', @ToDate='2026-12-31',
                                  @StoreNo=NULL, @GroupBy='DAY';
   Kỳ vọng: Index Seek trên IX_ReportSaleDetail_Store_Date, logical reads giảm mạnh. */


/* -------------------------------------------------------------------------------------
   MỤC 1 — Thêm @IncludeKpi để bỏ tính RS1 (KPI) dư khi không cần.

   Trang gọi SP cho DAY (cần KPI) + HOUR + WEEKDAY (chỉ cần series) + tùy chọn compare.
   RS1 (KPI tổng) giống hệt nhau với mọi @GroupBy nên hiện đang bị tính lặp 3–4 lần.
   Repository ĐÃ truyền includeKpi=false cho HOUR/WEEKDAY/compare; SP cần hỗ trợ @IncludeKpi
   để THỰC SỰ bỏ khối tính RS1 ở phía DB.

   Sửa định nghĩa SP (cần body thật):
     1) Thêm tham số:           @IncludeKpi BIT = 1
     2) Bọc khối SELECT RS1:    IF @IncludeKpi = 1 BEGIN  <SELECT KPI ...>  END
        (giữ nguyên khối tính RS2 / series như cũ)

   Tương thích ngược: mặc định @IncludeKpi = 1 nên các caller cũ không truyền vẫn chạy đúng.
   Repository truyền @IncludeKpi qua Dapper sau khi SP đã có tham số này — hiện code CHƯA
   truyền @IncludeKpi xuống SP (chỉ dùng để gate giá trị trả về) nhằm tránh vỡ SP đang chạy.
   Sau khi áp dụng ALTER, mở thêm dòng truyền @IncludeKpi trong GetSaleByTimeAsync để cắt
   hẳn chi phí tính KPI dư.
--------------------------------------------------------------------------------------- */
