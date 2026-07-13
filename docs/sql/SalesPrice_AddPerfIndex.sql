/* ============================================================================
   9.1 Danh mục Bảng giá — index bổ sung cho GetSalesPriceList / GetSalesPriceList_Export
   RPOSMasterData (CentralMD). CHẠY THỦ CÔNG / QUA POS.DbMigrator (Track A, idempotent).

   Lý do: [dbo].[SalesPrice] chỉ có PK composite (ItemNo, SalesCode, StartingDate,
   UnitOfMeasureCode), KHÔNG có index phụ nào. Cả GetSalesPriceList và GetSalesPriceList_Export
   luôn bắt buộc WHERE S.IsActive = 1 (không phụ thuộc filter nào) và ORDER BY StartingDate DESC
   — không có index nào hỗ trợ 2 điều kiện này trước đây. Khi filter mặc định trống (mở trang
   lần đầu, SalesGroup="ALL" → NormalizeSalesGroup map về ''), SQL Server buộc phải quét toàn bộ
   clustered index của SalesPrice (mọi dòng lịch sử giá) rồi mới OFFSET/FETCH — nguyên nhân
   thực sự gây chậm/giật trang /catalog/prices (không phải do client-side pagination — đã xác
   nhận PricesPage.razor dùng MudTable ServerData đúng chuẩn).

   Index này CHỈ bổ sung, KHÔNG đổi thân 2 SP (GetSalesPriceList_AddSalesTypeCode.sql giữ
   nguyên). Không tối ưu nhánh CHARINDEX theo ItemNo/ItemName (giữ nguyên hành vi tìm "chứa
   chuỗi con" theo quyết định đã chốt) — chỉ tối ưu base-scan + sort cho nhánh phổ biến nhất:
   mở trang không filter / lọc theo SaleType, SalesGroup, isCheck.
   ============================================================================ */
USE [RPOSMasterData]
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_SalesPrice_IsActive_StartingDate'
      AND object_id = OBJECT_ID('dbo.SalesPrice')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SalesPrice_IsActive_StartingDate
    ON dbo.SalesPrice (IsActive, StartingDate DESC)
    INCLUDE (EndingDate, SalesType, UnitPrice, Counter, Pkey);
END
GO
