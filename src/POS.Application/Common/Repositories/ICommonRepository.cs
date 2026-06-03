using POS.Application.Common.DTOs;

namespace POS.Application.Common.Repositories;

public interface ICommonRepository
{
    Task<TransCpnVchIssueResponse?> TransactionQtyUseAsync(string articleNo, string siteCode);
    Task<BusinessDateResponse?> GetBusinessDateAsync(string siteCode);
    Task InsertBussinessDateOpenAsync(string siteCode, DateTime bussinessDate);
    Task InsertSignalStoreAsync(string storeNo, string posTerminalId, DateTime? bussinessDate);
    Task<bool> CheckEndShiftAsync(string siteCode, string posTerminal, DateTime businessDate);
    Task<POSMonitorInsertResponse> POSMonitorInsertAsync(POSMonitorInsertRequest model);
    Task<PosTerminalResponse?> CheckIPaddressPosAsync(string ipAddress);
    Task<List<POSDataSetupResponse>> GetDataSetupAsync();
    Task<List<POSVersionResponse>> GetPOSVersionAsync();
    Task<List<SaleTableResponse>> GetOrderInfoAsync(string orderNo);
    Task<bool> CheckSaleReturnAsync(string orderNo);
    Task<List<POSDocumentNoResponse>> ListPOSDocumentNoAsync(string storeNo, string posTerminal);
    Task<bool> CheckCouponLineAsync(string itemNo, string barCode);
    Task<ResponseUpdateTransModel> InsertLineOrig_UpdateOrderInfoAsync(UpdateOrderInfoRequest model);
    Task<InsuranceResponse?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode);
    Task<bool> UpdatePOSEODAsync(POSEODRequest model);
    Task<CheckTotalBillResponse> CheckTotalBillAsync(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal);
    Task<(bool Success, string Message)> KiosInsertSaleAsync(string storeNo, string type, string json);
    Task<(bool Success, string Message, KiosCheckOrderResponse? Data)> KiosCheckOrderAsync(string storeNo, string posNo, string orderNo);
}
