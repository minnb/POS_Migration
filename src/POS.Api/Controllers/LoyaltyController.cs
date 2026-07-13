using Elastic.Transport;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using POS.Application.Features.Partner;
using POS.Common;
using POS.Common.Dtos.Capillary.Customer;
using POS.Common.Dtos.Loyalty;
using POS.Common.Dtos.Loyalty.CX;
using POS.Infrastructure.Logging;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace POS.Api.Controllers;

[Route("api")]
public sealed class LoyaltyController(
    IKibanaService kibanaService,
    IFileLogHelper fileLogHelper,
    IAkaChainLoyaltyService akaChainLoyaltyService
) : BaseController
{
    private readonly IKibanaService _kibanaService = kibanaService;
    private readonly IFileLogHelper _fileLogHelper = fileLogHelper;
    private readonly IAkaChainLoyaltyService _akaChainLoyaltyService = akaChainLoyaltyService;

    [HttpPost]
    [Route("v2/loyalty/customer")]
    public async Task<IActionResult> CustomerRegistration(RegisterMemberDto request)
    {
        if (!ModelState.IsValid) return ExceptionModels();
        var sw = Stopwatch.StartNew();
        var endpoint = "api/v2/loyalty/customer";

        try
        {
            var res = await _akaChainLoyaltyService.RegisterMemberAsync(request);
            sw.Stop();

            ResultResponse result;
            if (res.Status == HttpStatusCode.OK)
            {
                var data = new CustomerRegistrationResponse
                {
                    CreatedId = 123456789,
                    Warnings = [],
                    SideEffects = []
                };
            
                result = new ResultResponse
                    {
                        Data = data,
                        Message = "OK",
                        Status = HttpStatusCode.OK,
                        MessageTechnical = request.PosCode
                    };
            }
            else
            {
                result = new ResultResponse
                {
                    Data = res.Data,
                    Message = res.Message,
                    Status = res.Status,
                    MessageTechnical = res.MessageTechnical
                };
            }

            _kibanaService.LogResponse(endpoint, request.PosCode, sw.ElapsedMilliseconds, "", JsonConvert.SerializeObject(result));
            return StatusCode((int)result.Status, result);
        }
        catch (Exception ex)
        {
            _fileLogHelper.WriteExpLogs("LoyaltyController.GetCustomerDetail", ex);
            _kibanaService.LogException(endpoint, request.PosCode, 0, "", ex.Message);
            return BadRequestResult(ex);
        }
    }

    [HttpGet]
    [Route("vinid/GetInfoMember")]//Thông tin khách hàng cũ VINID
    public async Task<IActionResult> GetInfoMember_Old(string numberCard, string posID, string storeNo, string clubCode = "", bool isLog = true)
    {
        var sw = Stopwatch.StartNew();
        var endpoint = "api/vinid/GetInfoMember";
        if (string.IsNullOrEmpty(numberCard) || numberCard.Length < 9)
        {
            return BadRequestResult("Số điện thoại/số thẻ đang bị trống hoặc không đủ chiều dài ký tự");
        }

        try
        {
            var res = await _akaChainLoyaltyService.GetMemberProfileAsync("Phone", numberCard);
            sw.Stop();
            ResultResponse result;
            if (res.Status == HttpStatusCode.OK)
            {
                result = new ResultResponse
                {
                    Data = res.Data,
                    Message = "OK",
                    Status = HttpStatusCode.OK,
                    MessageTechnical = clubCode
                };
            }
            else
            {
                result = new ResultResponse
                {
                    Data = res.Data,
                    Message = res.Message,
                    Status = res.Status,
                    MessageTechnical = res.MessageTechnical
                };
            }

            _kibanaService.LogResponse(endpoint, posID, sw.ElapsedMilliseconds, "", JsonConvert.SerializeObject(result));
            return StatusCode((int)result.Status, result);
        }
        catch (Exception ex)
        {
            _fileLogHelper.WriteExpLogs("LoyaltyController.GetCustomerDetail", ex);
            _kibanaService.LogException(endpoint, posID, 0, "", ex.Message);
            return BadRequestResult(ex);
        }
    }

    [HttpGet]
    [Route("v2/loyalty/customer/get")]
    public async Task<IActionResult> GetCustomerDetail(
        string numberCard, string posID, string storeNo,
        string clubCode = "", bool isMobile = false, bool isLog = true)
    {
        var sw = Stopwatch.StartNew();
        var endpoint = "api/v2/loyalty/customer/get";

        if (string.IsNullOrEmpty(numberCard) || numberCard.Length < 9)
        {
            return BadRequestResult("Số điện thoại/số thẻ đang bị trống hoặc không đủ chiều dài ký tự");
        }

        try
        {
            var res = await _akaChainLoyaltyService.GetMemberProfileAsync("Phone", numberCard);
            sw.Stop();

            ResultResponse result;
            if (res.Status == HttpStatusCode.OK)
            {
                result = new ResultResponse
                {
                    Data = res.Data,
                    Message = "OK",
                    Status = HttpStatusCode.OK,
                    MessageTechnical = clubCode
                };
            }
            else
            {
                result = new ResultResponse
                {
                    Data = res.Data,
                    Message = res.Message,
                    Status = res.Status,
                    MessageTechnical = res.MessageTechnical
                };
            }

            _kibanaService.LogResponse(endpoint, posID, sw.ElapsedMilliseconds, "", JsonConvert.SerializeObject(result));
            return StatusCode((int)result.Status, result);
        }
        catch (Exception ex)
        {
            _fileLogHelper.WriteExpLogs("LoyaltyController.GetCustomerDetail", ex);
            _kibanaService.LogException(endpoint, posID, 0, "", ex.Message);
            return BadRequestResult(ex);
        }
    }

    [HttpPost]
    [Route("v2/loyalty/transaction/add")]
    public async Task<IActionResult> AddTransaction([FromBody] VinIDSalesRequest model)
    {
        if (!ModelState.IsValid) return ExceptionModels();

        var sw = Stopwatch.StartNew();
        var endpoint = "api/v2/loyalty/transaction/add";

        // TODO: extract phone validation to a helper
        if (!(model.CardNumber.Length >= 9 && model.CardNumber.Length <= 11 && model.CardNumber.All(char.IsDigit)))
        {
            return BadRequestResult($"Số thẻ {model.CardNumber} không hợp lệ");
        }

        try
        {
            var result = await _akaChainLoyaltyService.AddTransactionAsync(model);
            sw.Stop();
            _kibanaService.LogResponse(endpoint, model.PosID ?? "", sw.ElapsedMilliseconds, "", JsonConvert.SerializeObject(result));
            return StatusCode((int)result.Status, new ResultResponse
            {
                Data = result.Data,
                Message = result.Message,
                Status = result.Status
            });
        }
        catch (Exception ex)
        {
            _fileLogHelper.WriteExpLogs("LoyaltyController.AddTransaction", ex);
            _kibanaService.LogException(endpoint, model.PosID ?? "", 0, "", ex.Message);
            return BadRequestResult(ex);
        }
    }

    [HttpPost]
    [Route("v2/loyalty/transaction/refund")]
    public async Task<IActionResult> RefundTransactionAsync([FromBody] VinIDRefundRequest model)
    {
        if (!ModelState.IsValid) return ExceptionModels();

        var sw = Stopwatch.StartNew();
        var endpoint = "api/v2/loyalty/transaction/refund";

        // TODO: extract phone validation to a helper
        if (!(model.CardNumber.Length >= 9 && model.CardNumber.Length <= 11 && model.CardNumber.All(char.IsDigit)))
        {
            return BadRequestResult($"Số thẻ {model.CardNumber} không hợp lệ");
        }

        try
        {
            var result = await _akaChainLoyaltyService.ReturnTransactionAsync(model);
            sw.Stop();
            _kibanaService.LogResponse(endpoint, model.TerminalId ?? "", sw.ElapsedMilliseconds, "", JsonConvert.SerializeObject(result));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _fileLogHelper.WriteExpLogs("LoyaltyController.RefundTransactionAsync", ex);
            _kibanaService.LogException(endpoint, model.TerminalId ?? "", 0, "", ex.Message);
            return BadRequestResult(ex);
        }
    }

    [HttpGet]
    [Route("v2/otp/generate")]
    public IActionResult GenerateOTPV2([Required] string posNo = "201801", [Required] string phoneNumber = "0948886666", string action = "WIN_MEMBER_REGISTER")
    {
        var data = new CXResponse()
        {
            Message = "",
            DeveloperMessage = "",
            Data = new
            {
                MessageId = Guid.NewGuid(),
                Status = "PROCESSING",
                Reason = ""
            }
        };

        var result = new ResultResponse
        {
            Data = data,
            Message = "Success",
            Status = HttpStatusCode.OK,
            MessageTechnical = ""
        };

        return StatusCode((int)result.Status, result);
    }

    [HttpPost]
    [Route("v2/otp/verify")]
    public IActionResult VerifyOTPV2(POSVerifyOTPRequest request)
    {
        var result = new ResultResponse
        {
            Data = new VerifyOTPData()
            {
                PhoneNumber = request.PhoneNumber,
                Otp = request.Otp,
                IsValid = true
            },
            Message = "Success",
            Status = HttpStatusCode.OK,
            MessageTechnical = ""
        };

        return StatusCode((int)result.Status, result);
    }
}
