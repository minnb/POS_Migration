using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Api.Models.Winpay;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers
{
    [Route("api/v2/winpay")]
    [ApiController]
    public class WinpayController : BaseController
    {
        private readonly IWinpayService _winpayService;

        public WinpayController(IWinpayService winpayService)
        {
            _winpayService = winpayService;
        }

        // POST api/v2/winpay/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterWinpayPOS request)
        {
            if (!ModelState.IsValid) return NewExceptionModels();
            var result = await _winpayService.RegisterAsync(request);
            return ConvertResult(result);
        }

        // GET api/v2/winpay/get-register-info?phone=...
        [HttpGet("get-register-info")]
        public async Task<IActionResult> GetInfo([FromQuery] string phone)
        {
            if (!ModelState.IsValid) return NewExceptionModels();
            var formattedPhone = FormatHelper.PhoneNumberVietNam(phone);
            var result = await _winpayService.GetRegisterInfoAsync(formattedPhone);
            return ConvertResult(result);
        }

        // POST api/v2/winpay/unregister
        [HttpPost("unregister")]
        public async Task<IActionResult> UnRegister([FromBody] UnregisterWinpayPOS request)
        {
            if (!ModelState.IsValid) return NewExceptionModels();
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            var result = await _winpayService.UnRegisterAsync(request);
            return ConvertResult(result);
        }

        // POST api/v2/winpay/payment
        [HttpPost("payment")]
        public async Task<IActionResult> Payment([FromBody] PaymentWinpayPOS request)
        {
            if (!ModelState.IsValid) return NewExceptionModels();
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            var result = await _winpayService.PaymentAsync(request, ActionWinpayEnum.payment.ToString());
            return ConvertResult(result);
        }

        // POST api/v2/winpay/refund
        [HttpPost("refund")]
        public async Task<IActionResult> Refund([FromBody] RefundWinpayPOS request)
        {
            if (!ModelState.IsValid) return NewExceptionModels();
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            var result = await _winpayService.RefundAsync(request);
            return ConvertResult(result);
        }

        // POST api/v2/winpay/deposit
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] PaymentWinpayPOS request)
        {
            if (!ModelState.IsValid) return NewExceptionModels();
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            var result = await _winpayService.DepositAsync(request);
            return ConvertResult(result);
        }

        // POST api/v2/winpay/withdraw
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawWinpayPOS request)
        {
            if (!ModelState.IsValid) return NewExceptionModels();
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            var result = await _winpayService.WithdrawAsync(request);
            return ConvertResult(result);
        }

        // POST api/v2/winpay/fp-update
        [HttpPost("fp-update")]
        public async Task<IActionResult> UpdateFinger([FromBody] UpdateFingerWinpayPOS request)
        {
            if (!ModelState.IsValid) return NewExceptionModels();
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            var result = await _winpayService.UpdateFingerAsync(request);
            return ConvertResult(result);
        }

        // POST api/v2/winpay/fp-verify
        [HttpPost("fp-verify")]
        public async Task<IActionResult> VerifyFinger([FromBody] VerifyFingerWinpayPOS request)
        {
            if (!ModelState.IsValid) return NewExceptionModels();
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            var result = await _winpayService.VerifyFingerAsync(request);
            return ConvertResult(result);
        }

        // POST api/v2/winpay/cashback
        [HttpPost("cashback")]
        public async Task<IActionResult> Cashback([FromBody] CashbackWinpayPOS request)
        {
            if (!ModelState.IsValid) return NewExceptionModels();
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            var result = await _winpayService.CashbackAsync(request);
            return ConvertResult(result);
        }

        private IActionResult ConvertResult(ResultResponse response)
        {
            // ResultResponse already contains Status (HttpStatusCode) and data.
            return StatusCode((int)response.Status, response);
        }
    }
}
