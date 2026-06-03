using POS.Application.Common.DTOs;

namespace POS.Application.Common.Services;

public interface ICommonService
{
    Task<TransCpnVchIssueResponse?> TransactionQtyUseAsync(string articleNo, string siteCode);
    Task<BusinessDateResponse?> GetBusinessDateAsync(string siteCode, string? posTerminal);
    Task<bool> CheckEndShiftAsync(string siteCode, string posTerminal, DateTime businessDate);
    Task<POSMonitorInsertResponse> POSMonitorInsertAsync(POSMonitorInsertRequest model);
    Task<PosTerminalResponse?> CheckIPaddressPosAsync(string ipAddress);
    Task<List<POSDataSetupResponse>> GetDataSetupAsync();
    Task<List<POSVersionResponse>> GetPOSVersionAsync();
    Task<List<SaleTableResponse>> GetOrderInfoAsync(string orderNo, string storeNo);
    Task<string> WriteFileByManualAsync(string storeNo, string posTerminal);
    Task<List<POSDocumentNoResponse>> GetListPOSDocumentNoAsync(string siteCode, string posTerminal);
    Task<bool> CheckCouponLineAsync(string itemNo, string barCode);
    Task<ResponseUpdateTransModel> UpdateOrderTransAsync(UpdateOrderInfoRequest model);
    Task<InsuranceResponse?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode);
    Task<bool> UpdateEODAsync(POSEODRequest model);
    Task<CheckTotalBillResponse> CheckTotalBillAsync(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal);
    Task<(bool Success, string Message)> KiosInsertSaleAsync(string storeNo, string posId, string saleJson);
    Task<(bool Success, string Message, KiosCheckOrderResponse? Data)> KiosCheckOrderAsync(string storeNo, string posNo, string orderNo);
    Task LoggingAsync(LoggingElasticRequest model);
}
