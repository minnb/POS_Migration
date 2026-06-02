using System.Threading.Tasks;
using TCX.API.Common.Dtos.Winpay;
using TCX.API.Common.Dtos;
using VCM.POSBLUE.Application.Interfaces;

namespace VCM.POSBLUE.Application.Services
{
    public class WinpayService : IWinpayService
    {
        private readonly TCX.WebApiCore.WinpayService _legacy;

        public WinpayService()
        {
            _legacy = new TCX.WebApiCore.WinpayService(); // legacy implementation from WebApi
        }

        public Task<ResultResponse> RegisterAsync(RegisterWinpayPOS request) => Task.FromResult(_legacy.RegisterWinpay(request));
        public Task<ResultResponse> GetRegisterInfoAsync(string phone) => Task.FromResult(_legacy.GetRegisterInfo(string.Empty, phone));
        public Task<ResultResponse> UnRegisterAsync(UnregisterWinpayPOS request) => Task.FromResult(_legacy.UnRegisterWinpay(request));
        public Task<ResultResponse> PaymentAsync(PaymentWinpayPOS request, string action) => Task.FromResult(_legacy.PaymentWinpay(request, action));
        public Task<ResultResponse> RefundAsync(RefundWinpayPOS request) => Task.FromResult(_legacy.RefundWinpay(request));
        public Task<ResultResponse> DepositAsync(PaymentWinpayPOS request) => Task.FromResult(_legacy.PaymentWinpay(request, ActionWinpayEnum.cashin.ToString()));
        public Task<ResultResponse> WithdrawAsync(WithdrawWinpayPOS request) => Task.FromResult(_legacy.PaymentWinpay(request, ActionWinpayEnum.cashout.ToString()));
        public Task<ResultResponse> UpdateFingerAsync(UpdateFingerWinpayPOS request) => Task.FromResult(_legacy.FingerUpdate(request));
        public Task<ResultResponse> VerifyFingerAsync(VerifyFingerWinpayPOS request) => Task.FromResult(_legacy.VerifyFingerWinpay(request));
        public Task<ResultResponse> CashbackAsync(CashbackWinpayPOS request) => Task.FromResult(_legacy.CashbackWinpay(request));

        public Task<(bool Success, string Message)> SendOtpWinpayAsync(string posNo, string phoneNumber, string action)
        {
            var result = _legacy.SendOtpWinpay(posNo, phoneNumber, action);
            return Task.FromResult((result.Item1, result.Item2 ?? ""));
        }
    }
}
