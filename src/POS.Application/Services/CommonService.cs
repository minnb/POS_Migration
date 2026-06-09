using POS.Application.Interfaces;
using POS.Common.Dtos.POS.Common;
using POS.Common.Dtos.Reward;

namespace POS.Application.Services;

/// <summary>
/// Stub implementation — các method ném NotImplementedException cho đến khi
/// infrastructure tương ứng (CentralSaleRepository, StagingRepository, PLHService)
/// được migrate. Xem TODO comment trong từng method.
/// </summary>
public sealed class CommonService : ICommonService
{
    // TODO: inject ICentralMDRepository, ICentralSaleRepository, IStagingDbRepository
    //       sau khi các repository đó được tạo và bổ sung đủ method.

    public Task<TransCpnVchIssueModel?> TransactionQtyUseAsync(string articleNo, string siteCode)
        // TODO: implement via CentralSaleRepository (store-specific DB routing)
        => throw new NotImplementedException("TransactionQtyUse: needs CentralSaleRepository");

    public Task<BusinessDateResponse?> GetBusinessDateAsync(string siteCode)
        // TODO: implement via CentralSaleRepository
        => throw new NotImplementedException("GetBusinessDate: needs CentralSaleRepository");

    public Task InsertBussinessDateOpenAsync(BussinessDateOpenModel model)
        // TODO: implement via CentralSaleRepository
        => throw new NotImplementedException("InsertBussinessDateOpen: needs CentralSaleRepository");

    public Task<bool> InsertSignalStoreAsync(SignalStoreModel model)
        // TODO: implement via CentralSaleRepository
        => throw new NotImplementedException("InsertSignalStore: needs CentralSaleRepository");

    public Task<ShiftHeaderModel?> GetShiftHeaderAsync(string siteCode, string posTerminal, DateTime businessDate)
        // TODO: implement via CentralSaleRepository — SP: [API_POS_CHECK_SHIFT_HEADER]
        => throw new NotImplementedException("GetShiftHeader: needs CentralSaleRepository");

    public Task<POSMonitorInsertResponse?> POSMonitorInsertAsync(POSMonitorInsertRequest model)
        // TODO: implement via CentralMDRepository — SP: [dbo].[POSMonitorInsert]
        => throw new NotImplementedException("POSMonitorInsert: needs CentralMDRepository.POSMonitorInsertAsync");

    public Task<PosTerminalModel?> CheckIPaddressPosAsync(string ipAddress)
        // TODO: implement via CentralMDRepository — SELECT from POSTerminals
        => throw new NotImplementedException("CheckIPaddressPos: needs CentralMDRepository.CheckIPaddressPosAsync");

    public Task<List<POSDataSetupModel>> GetDataSetupAsync()
        // TODO: implement via CentralMDRepository — SELECT from POSDataSetup
        => throw new NotImplementedException("GetDataSetup: needs CentralMDRepository.GetDataSetupAsync");

    public Task<List<POSVersionModel>> GetPOSVersionAsync()
        // TODO: implement via CentralMDRepository — SELECT from POSVersion
        => throw new NotImplementedException("GetPOSVersion: needs CentralMDRepository.GetPOSVersionAsync");

    public Task<bool> CheckSaleReturnAsync(string orderNo)
        // TODO: implement via CentralSaleRepository or CentralMDRepository
        => throw new NotImplementedException("CheckSaleReturn: needs repository");

    public Task<List<SaleTableModel>> GetOrderInfoAsync(string orderNo)
        // TODO: implement via CentralMDRepository or CentralSaleRepository
        => throw new NotImplementedException("GetOrderInfo: needs repository");

    public Task<List<POSDocumentNoModel>> ListPOSDocumentNoAsync(string storeNo, string posTerminal)
        // TODO: implement via CentralSaleRepository
        => throw new NotImplementedException("ListPOSDocumentNo: needs CentralSaleRepository");

    public Task<bool> CheckCouponLineAsync(string itemNo, string barCode)
        // TODO: implement via CentralMDRepository
        => throw new NotImplementedException("CheckCouponLine: needs CentralMDRepository");

    public Task<ResponseUpdateTransModel> InsertLineOrig_UpdateOrderInfoAsync(UpdateOrderInfoModel model)
        // TODO: implement via CentralSaleRepository — complex multi-step operation
        => throw new NotImplementedException("InsertLineOrig_UpdateOrderInfo: needs CentralSaleRepository");

    public Task<InsuranceModel?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode)
        // TODO: implement via CentralMDRepository or CentralSaleRepository
        => throw new NotImplementedException("GetInsurance: needs repository");

    public Task<bool> UpdatePOSEODAsync(POSEOD_APIModel model)
        // TODO: implement via CentralSaleRepository — table: POSEOD_API
        => throw new NotImplementedException("UpdatePOSEOD: needs CentralSaleRepository");

    public Task<CheckTotalBillResponse?> CheckTotalBillAsync(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal)
        // TODO: implement via CentralSaleRepository
        => throw new NotImplementedException("CheckTotalBill: needs CentralSaleRepository");

    public Task<(bool Success, string Message)> KiosInsertSaleAsync(KiosInsertSaleRequest model)
        // TODO: implement via StagingDbRepository — KIOS tables
        => throw new NotImplementedException("KiosInsertSale: needs StagingDbRepository");

    public Task LogSaleKiosAsync(LogSaleKiosModel model)
        // TODO: implement via StagingDbRepository — LogSaleKio table
        => throw new NotImplementedException("LogSaleKios: needs StagingDbRepository");

    public Task<(bool Success, string Message, KiosCheckOrderResponse? Data)> KiosCheckOrderAsync(string storeNo, string posNo, string orderNo)
        // TODO: implement via StagingDbRepository — KIOS tables
        => throw new NotImplementedException("KiosCheckOrder: needs StagingDbRepository");

    public Task<(bool Success, int Code, string Message, RewardCodeSendModel? Data)> SendCodeRewardAsync(RewardCodeRequest model)
        // TODO: implement via PLHService (external API) — replaces IpLGBLO.SendCodeReWardToPOS
        => throw new NotImplementedException("SendCodeReward: needs PLHService (external API integration)");

    public Task WriteLogApiAsync(LogAPIModel model)
        // TODO: implement via LoyaltyRepository or LogRepository — replaces VinIDBLO.WriteLogAPI
        => throw new NotImplementedException("WriteLogApi: needs LogRepository");
}
