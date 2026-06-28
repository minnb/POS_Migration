using POS.Common.Dtos.POS.Common;
using POS.Common.Dtos.Reward;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Features.Common;

/// <summary>
/// Migrated từ CommonBLO (VCM.POSBLUE.Business/Common/CommonBLO.cs) — thin wrapper qua repository.
/// Nhóm core (CentralSales routed + CentralMD) đã implement.
/// Nhóm deferred (KIOS — user yêu cầu bỏ qua, DCMStar, SendCodeReward, Staging, LogAPI)
/// vẫn NotImplementedException — controller catch và trả lỗi có message.
/// </summary>
public sealed class CommonService(
    ICentralSaleRepository centralSaleRepository,
    ICentralMDRepository centralMDRepository) : ICommonService
{
    // ── CentralSales (routed per-store) ───────────────────────────────────────

    public Task<TransCpnVchIssueModel?> TransactionQtyUseAsync(string articleNo, string siteCode)
        => centralSaleRepository.TransactionQtyUseAsync(articleNo, siteCode);

    public Task<BusinessDateResponse?> GetBusinessDateAsync(string siteCode)
        => centralSaleRepository.GetBusinessDateAsync(siteCode);

    public Task InsertBussinessDateOpenAsync(BussinessDateOpenModel model)
        => centralSaleRepository.InsertBussinessDateOpenAsync(model);

    public Task<ShiftHeaderModel?> GetShiftHeaderAsync(string siteCode, string posTerminal, DateTime businessDate)
        => centralSaleRepository.GetShiftHeaderAsync(siteCode, posTerminal, businessDate);

    public Task<bool> CheckSaleReturnAsync(string orderNo)
        => centralSaleRepository.CheckSaleReturnAsync(orderNo);

    public Task<List<SaleTableModel>> GetOrderInfoAsync(string orderNo)
        => centralSaleRepository.GetOrderInfoAsync(orderNo);

    public Task<List<POSDocumentNoModel>> ListPOSDocumentNoAsync(string storeNo, string posTerminal)
        => centralSaleRepository.ListPOSDocumentNoAsync(storeNo, posTerminal);

    public Task<List<TransHeaderOrderModel>> GetTopOrderNoAsync(string storeNo, string posNo)
        => centralSaleRepository.GetTopOrderNoAsync(storeNo, posNo);

    public Task<bool> UpdatePOSEODAsync(POSEOD_APIModel model)
        => centralSaleRepository.UpdatePOSEODAsync(model);

    // ── CentralMD ─────────────────────────────────────────────────────────────

    public Task<POSMonitorInsertResponse?> POSMonitorInsertAsync(POSMonitorInsertRequest model)
        => centralMDRepository.POSMonitorInsertAsync(model);

    public Task<PosTerminalModel?> CheckIPaddressPosAsync(string ipAddress)
        => centralMDRepository.CheckIPaddressPosAsync(ipAddress);

    public async Task<List<POSDataSetupModel>> GetDataSetupAsync()
        => await centralMDRepository.GetDataSetupListAsync() ?? [];

    public async Task<List<POSVersionModel>> GetPOSVersionAsync()
        => await centralMDRepository.GetPOSVersionAsync() ?? [];

    public Task<bool> CheckCouponLineAsync(string itemNo, string barCode)
        => centralMDRepository.CheckCouponLineAsync(itemNo, barCode);

    public Task<bool> InsertSignalStoreAsync(SignalStoreModel model)
        => centralMDRepository.InsertSignalStoreAsync(model);

    // ── Deferred — KIOS (user yêu cầu bỏ qua) ────────────────────────────────

    public Task<(bool Success, string Message)> KiosInsertSaleAsync(KiosInsertSaleRequest model)
        => throw new NotImplementedException("KiosInsertSale: deferred — KIOS bỏ qua theo yêu cầu");

    public Task LogSaleKiosAsync(LogSaleKiosModel model)
        => throw new NotImplementedException("LogSaleKios: deferred — KIOS bỏ qua theo yêu cầu");

    public Task<(bool Success, string Message, KiosCheckOrderResponse? Data)> KiosCheckOrderAsync(string storeNo, string posNo, string orderNo)
        => throw new NotImplementedException("KiosCheckOrder: deferred — KIOS bỏ qua theo yêu cầu");

    // ── Deferred — pass sau ──────────────────────────────────────────────────

    public Task<ResponseUpdateTransModel> InsertLineOrig_UpdateOrderInfoAsync(UpdateOrderInfoModel model)
        // TODO: cần CentralSaleStagingRepository (routed) — transaction nhiều bảng + SP InsertTransOrig
        => throw new NotImplementedException("InsertLineOrig_UpdateOrderInfo: needs CentralSaleStagingRepository");

    public Task<CheckTotalBillResponse?> CheckTotalBillAsync(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal)
        // TODO: cần CentralSaleStagingRepository (routed) — COUNT TransHeaderStaging
        => throw new NotImplementedException("CheckTotalBill: needs CentralSaleStagingRepository");

    public Task<InsuranceModel?> GetInsuranceAsync(string receiptNo, string posNo, string staffCode)
        // TODO: cần DCMStar connection — SP [dbo].[BAOHIEM5]
        => throw new NotImplementedException("GetInsurance: needs DCMStar repository");

    public Task<(bool Success, int Code, string Message, RewardCodeSendModel? Data)> SendCodeRewardAsync(RewardCodeRequest model)
        // TODO: CentralMD transaction — SP [dbo].[RewardCodeIssue] + INSERT RewardCodeSend (port PLGData.SendCodeReWardToPOS)
        => throw new NotImplementedException("SendCodeReward: needs RewardCodeIssue transaction in CentralMDRepository");

    public Task WriteLogApiAsync(LogAPIModel model)
        // TODO: cần ILoyaltyRepository.InsertLogAPIAsync — table Loyalty.dbo.LogAPI (port VinIDData.WriteLogAPI)
        => throw new NotImplementedException("WriteLogApi: needs LoyaltyRepository.InsertLogAPIAsync");
}
