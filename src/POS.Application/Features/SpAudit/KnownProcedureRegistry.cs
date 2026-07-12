namespace POS.Application.Features.SpAudit;

/// <summary>
/// Danh sách THỦ CÔNG các stored procedure đang được gọi từ Repository layer (rút ra từ grep
/// "EXEC"/CommandType.StoredProcedure trong src/POS.Infrastructure/Repositories/).
///
/// RỦI RO BẢO TRÌ: nếu 1 Repository mới gọi thêm SP khác mà danh sách này không được cập nhật,
/// SP đó sẽ bị phân loại sai thành "không dùng từ code" (có thể đề xuất CleanupCandidate thay vì
/// MigrationCandidate/KeepAsIs đúng ra phải có). Cập nhật danh sách này mỗi khi thêm SP call mới.
/// </summary>
public static class KnownProcedureRegistry
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "RPT_GET_DETAIL_REVENUE_SALES_LIST",   // RptCentralSaleRepository
        "Rpt_ReportSaleByTime",                 // RptCentralSaleRepository
        "Rpt_ReportTopProduct",                 // RptCentralSaleRepository
        "Rpt_SalesByCategory",                  // RptCentralSaleRepository
        "Rpt_ReportSaleByPayment",               // RptCentralSaleRepository
        "Rpt_ReportSaleDetail_Insert",           // RptReportSaleDetailRepository
        "API_POS_CHECK_SHIFT_HEADER",           // CentralSaleRepository
        "Sale_InsertDataByOrder_KAFKA",          // CentralSaleRepository
        "SP_API_GET_OfferStaffRemn",             // OfferStaffRepository
        "SP_API_GetAndUseGiftCode",              // LoyaltyRepository
        "POSMonitorInsert"                       // CentralMDRepository
    };

    public static bool IsCalledFromCode(string procedureName) => Names.Contains(procedureName);
}
