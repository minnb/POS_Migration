using System.Threading.Tasks;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Api.Models.Winpay;

namespace VCM.POSBLUE.Application.Interfaces
{
    public interface IWinpayService
    {
        Task<ResultResponse> RegisterAsync(RegisterWinpayPOS request);
        Task<ResultResponse> GetRegisterInfoAsync(string phone);
        Task<ResultResponse> UnRegisterAsync(UnregisterWinpayPOS request);
        Task<ResultResponse> PaymentAsync(PaymentWinpayPOS request, string action);
        Task<ResultResponse> RefundAsync(RefundWinpayPOS request);
        Task<ResultResponse> DepositAsync(PaymentWinpayPOS request);
        Task<ResultResponse> WithdrawAsync(WithdrawWinpayPOS request);
        Task<ResultResponse> UpdateFingerAsync(UpdateFingerWinpayPOS request);
        Task<ResultResponse> VerifyFingerAsync(VerifyFingerWinpayPOS request);
        Task<ResultResponse> CashbackAsync(CashbackWinpayPOS request);

        /// <summary>Gửi OTP qua Winpay — port từ WinpayService.SendOtpWinpay cũ.</summary>
        Task<(bool Success, string Message)> SendOtpWinpayAsync(string posNo, string phoneNumber, string action);
    }
}
