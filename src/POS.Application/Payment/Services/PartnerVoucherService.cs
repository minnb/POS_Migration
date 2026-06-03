using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using POS.Application.Payment.DTOs;
using POS.Shared.Models;

namespace POS.Application.Payment.Services;

public class PartnerVoucherService : IPartnerVoucherService
{
    private readonly IUrboxService _urbox;
    private readonly IGotITService _gotIt;
    private readonly IOneUService _oneU;
    private readonly ILogger<PartnerVoucherService> _logger;

    public PartnerVoucherService(
        IUrboxService urbox,
        IGotITService gotIt,
        IOneUService oneU,
        ILogger<PartnerVoucherService> logger)
    {
        _urbox = urbox;
        _gotIt = gotIt;
        _oneU = oneU;
        _logger = logger;
    }

    public async Task<(int HttpStatus, ResultResponse Body)> CheckVoucherAsync(CheckVoucherPartnerRequest request)
    {
        _logger.LogInformation("CheckVoucher partner={Partner} store={StoreNo} pos={PosNo}", request.Partner, request.StoreNo, request.PosNo);

        switch (request.Partner.ToUpper())
        {
            case "URBOX":
                var urboxResult = await _urbox.CheckSerialAsync(request);
                _logger.LogInformation("CheckVoucher URBOX success={Success} message={Message}", urboxResult.Success, urboxResult.Message);
                return urboxResult.Success
                    ? (200, new ResultResponse { Status = 200, Message = urboxResult.Message, Data = urboxResult.Data, MessageTechnical = string.Empty })
                    : (400, new ResultResponse { Status = 400, Message = urboxResult.Message, Data = urboxResult.Data, MessageTechnical = string.Empty });

            case "GOTIT":
                var gotItResult = await _gotIt.CheckMultipleAsync(request);
                _logger.LogInformation("CheckVoucher GOTIT success={Success} message={Message}", gotItResult.Success, gotItResult.Message);
                return gotItResult.Success
                    ? (200, new ResultResponse { Status = 200, Message = gotItResult.Message, Data = gotItResult.Data, MessageTechnical = string.Empty })
                    : (400, new ResultResponse { Status = 400, Message = gotItResult.Message, Data = gotItResult.Data, MessageTechnical = string.Empty });

            case "ONEU":
                var oneUResult = await _oneU.EstimateAsync(request);
                _logger.LogInformation("CheckVoucher ONEU status={Status}", oneUResult.Status);
                return (oneUResult.Status, oneUResult);

            default:
                _logger.LogWarning("CheckVoucher unknown partner={Partner}", request.Partner);
                return (400, new ResultResponse
                {
                    Status = 400,
                    Message = $"Tham số PartnerCode truyền vào không đúng {request.Partner}",
                    Data = null,
                    MessageTechnical = string.Empty
                });
        }
    }
}
