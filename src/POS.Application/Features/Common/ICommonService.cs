using POS.Common.Dtos.POS.Common;
using POS.Common.Dtos.Reward;

namespace POS.Application.Features.Common;

public interface ICommonService
{
    Task<TransCpnVchIssueModel?> TransactionQtyUseAsync(string articleNo, string siteCode);

    Task<BusinessDateResponse?> GetBusinessDateAsync(string siteCode);
    Task InsertBussinessDateOpenAsync(BussinessDateOpenModel model);
    Task<bool> InsertSignalStoreAsync(SignalStoreModel model);

    Task<ShiftHeaderModel?> GetShiftHeaderAsync(string siteCode, string posTerminal, DateTime businessDate);

    Task<POSMonitorInsertResponse?> POSMonitorInsertAsync(POSMonitorInsertRequest model);

    Task<PosTerminalModel?> CheckIPaddressPosAsync(string ipAddress);
    Task<List<POSDataSetupModel>> GetDataSetupAsync();
    Task<List<POSVersionModel>> GetPOSVersionAsync();

    Task<bool> CheckSaleReturnAsync(string orderNo);
    Task<List<SaleTableModel>> GetOrderInfoAsync(string orderNo);

    Task<List<POSDocumentNoModel>> ListPOSDocumentNoAsync(string storeNo, string posTerminal);
    Task<List<TransHeaderOrderModel>> GetTopOrderNoAsync(string storeNo, string posNo);
    Task<bool> CheckCouponLineAsync(string itemNo, string barCode);

    Task<ResponseUpdateTransModel> InsertLineOrig_UpdateOrderInfoAsync(UpdateOrderInfoModel model);

    Task<InsuranceModel?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode);

    Task<bool> UpdatePOSEODAsync(POSEOD_APIModel model);

    Task<CheckTotalBillResponse?> CheckTotalBillAsync(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal);

    Task<(bool Success, string Message)> KiosInsertSaleAsync(KiosInsertSaleRequest model);
    Task LogSaleKiosAsync(LogSaleKiosModel model);
    Task<(bool Success, string Message, KiosCheckOrderResponse? Data)> KiosCheckOrderAsync(string storeNo, string posNo, string orderNo);

    /// <summary>
    /// Tương đương IpLGBLO.SendCodeReWardToPOS — gọi PLH external API.
    /// Returns: (Success, StatusCode, Message, RewardData)
    /// </summary>
    Task<(bool Success, int Code, string Message, RewardCodeSendModel? Data)> SendCodeRewardAsync(RewardCodeRequest model);

    Task WriteLogApiAsync(LogAPIModel model);
}
