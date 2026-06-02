using TCX.API.Common.Dtos.Winpay;
using TCX.API.Common.Dtos;
using TCX.API.Common.Enums;
using TCX.WebApiCore;
using System.Web.Http;
using TCX.API.Common.Shared;
using System.Net.Http;
using System.Net;
using TCX.API.Common.Models;
using TCX.API.Common.Helpers;
using System;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api/v2/winpay")]
    public class WinpayController:BaseController
    {
        public readonly WinpayService _winpayService;
        public WinpayController()
        {
            _winpayService = new WinpayService();
        }

        //winpay_1
        [HttpPost]
        [Route("register")]
        public HttpResponseMessage WinpayRegister(RegisterWinpayPOS request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            return ConvertHttpResponseMessage(_winpayService.RegisterWinpay(request));
        }

        //winpay_2
        [HttpGet]
        [Route("get-register-info")]
        public HttpResponseMessage WinpayGetInfo(string phone)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            phone = FormatHelper.PhoneNumberVietNam(phone);
            return ConvertHttpResponseMessage(_winpayService.GetRegisterInfo(StringHelper.Right(phone, 4), phone));
        }

        //winpay_9
        [HttpPost]
        [Route("unregister")]
        public HttpResponseMessage WinpayUnRegister(UnregisterWinpayPOS request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            return ConvertHttpResponseMessage(_winpayService.UnRegisterWinpay(request));
        }

        //winpay_3
        [HttpPost]
        [Route("payment")]
        public HttpResponseMessage WinpayPayment(PaymentWinpayPOS request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            return ConvertHttpResponseMessage(_winpayService.PaymentWinpay(request, ActionWinpayEnum.payment.ToString()));
        }

        //winpay_7
        [HttpPost]
        [Route("refund")]
        public HttpResponseMessage WinpayRefund(RefundWinpayPOS request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            return ConvertHttpResponseMessage(_winpayService.RefundWinpay(request));
        }

        //winpay_4
        [HttpPost]
        [Route("deposit")]
        public HttpResponseMessage WinpayDeposit(PaymentWinpayPOS request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            return ConvertHttpResponseMessage(_winpayService.PaymentWinpay(request, ActionWinpayEnum.cashin.ToString()));
        }

        //winpay_5
        [HttpPost]
        [Route("withdraw")]
        public HttpResponseMessage WinpayWithdraw(WithdrawWinpayPOS request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            return ConvertHttpResponseMessage(_winpayService.PaymentWinpay(request, ActionWinpayEnum.cashout.ToString()));
        }

        //winpay_6
        [HttpPost]
        [Route("fp-update")]
        public HttpResponseMessage WinpayUpdateFinger(UpdateFingerWinpayPOS request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            return ConvertHttpResponseMessage(_winpayService.FingerUpdate(request));
        }

        //winpay_8
        [HttpPost]
        [Route("fp-verify")]
        public HttpResponseMessage WinpayVerifyFinger(VerifyFingerWinpayPOS request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            return ConvertHttpResponseMessage(_winpayService.VerifyFingerWinpay(request));
        }

        [HttpPost]
        [Route("cashback")]
        public HttpResponseMessage WinpayCashback(CashbackWinpayPOS request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
            
            return ConvertHttpResponseMessage(_winpayService.CashbackWinpay(request));
        }
        private HttpResponseMessage ConvertHttpResponseMessage(NewHttpResponseDto newHttpResponseDto)
        {
            if(newHttpResponseDto.StatusCode == 200)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = newHttpResponseDto.Data,
                    Message = newHttpResponseDto.StatusName,
                    Status = HttpStatusCode.OK,
                    MessageTechnical = newHttpResponseDto.MessageTechnical,
                });
            }
            else if(newHttpResponseDto.StatusCode == 409)
            {
                return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                {
                    Data = newHttpResponseDto.Data,
                    Message = newHttpResponseDto.StatusName,
                    Status = HttpStatusCode.Conflict,
                    MessageTechnical = newHttpResponseDto.MessageTechnical,
                });
            }
            else if (newHttpResponseDto.StatusCode == 404)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, new ResultResponse
                {
                    Data = newHttpResponseDto.Data,
                    Message = newHttpResponseDto.StatusName,
                    Status = HttpStatusCode.NotFound,
                    MessageTechnical = newHttpResponseDto.MessageTechnical,
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = newHttpResponseDto.Data,
                    Message = newHttpResponseDto.StatusName,
                    Status = HttpStatusCode.BadRequest,
                    MessageTechnical = newHttpResponseDto.MessageTechnical,
                });
            }
        }
    }
}
