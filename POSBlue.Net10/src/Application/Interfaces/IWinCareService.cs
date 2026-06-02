namespace VCM.POSBLUE.Application.Interfaces
{
    using System.Threading.Tasks;
    using VCM.POSBLUE.Shared.DTOs;
    using VCM.POSBLUE.Shared.DTOs.WinCare;

    public interface IWinCareService
    {
        Task<ResultResponse> WinPayAccumulationAsync(WinPayAccumulationDto request);
        Task<ResultResponse> WinPayAccumulationV3Async(WinPayAccumulationDto request);
        Task<string> RequestSentNotifyUserAsync(SentNotifyPOSRequest request);
        ResultResponse GetInfoBarcode(SaleOrderWinPlusBarcode request);
        ResultResponse ConfirmCollectedCash(ConfirmCollectedCashRequest request);
    }
}
