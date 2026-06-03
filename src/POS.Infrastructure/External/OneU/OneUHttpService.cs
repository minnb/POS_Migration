using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using POS.Application.Payment.DTOs;
using POS.Application.Payment.Services;
using POS.Application.Shared.DTOs;
using POS.Application.Shared.Services;
using POS.Infrastructure.Helpers;
using POS.Shared.Models;

namespace POS.Infrastructure.External.OneU;

public class OneUHttpService : IOneUService
{
    private const string TokenCacheKey = "BLUEPOS:OneUToken";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISysWebApiConfigService _configService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OneUHttpService> _logger;

    public OneUHttpService(
        IHttpClientFactory httpClientFactory,
        ISysWebApiConfigService configService,
        IMemoryCache cache,
        ILogger<OneUHttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configService = configService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ResultResponse> EstimateAsync(CheckVoucherPartnerRequest request)
    {
        try
        {
            var dto = await _configService.GetByAppCodeAsync("ONEU");
            if (dto == null)
                return new ResultResponse { Status = 400, Message = "Không tìm thấy cấu hình ONEU", Data = null };

            // Description = merchant_code
            var merchantCode = dto.Description.Trim();

            var tokenResult = await GetTokenAsync(dto);
            if (!tokenResult.Success)
                return new ResultResponse { Status = 400, Message = "Lỗi không lấy được token OneU", Data = null, MessageTechnical = tokenResult.Message };

            decimal totalAmount = request.Items?.Sum(x => x.LineAmount) ?? 0;
            var vouchers = request.SerialNo.Select(s => new
            {
                type = "VOUCHER",
                serial = s,
                merchant_code = merchantCode
            }).ToList<object>();

            var bodyJson = JsonConvert.SerializeObject(new
            {
                usecase = "WCM",
                apply_items = vouchers,
                checkout_data = new
                {
                    total_amount = totalAmount,
                    id = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % int.MaxValue),
                    merchant = new { code = merchantCode, name = "Wincommerce" },
                    store = new { code = request.StoreNo, name = "" },
                    money_sources = new[] { new { payment_method_code = "ZVID" } },
                    order_details = new { }
                }
            });

            var estimateRoute = dto.GetRoute("Estimate");
            if (estimateRoute == null)
                return new ResultResponse { Status = 400, Message = "Không tìm thấy route Estimate của ONEU", Data = null };

            var (rawResponse, httpStatus) = await CallApiAsync(dto, tokenResult.Message, "Estimate", request.PosNo, estimateRoute.Route, bodyJson);

            if (string.IsNullOrEmpty(rawResponse))
                return new ResultResponse { Status = 400, Message = $"{httpStatus}_{rawResponse}", Data = null };

            var estimateResp = JsonConvert.DeserializeObject<EstimateResponseOneU>(rawResponse);
            if (estimateResp?.Meta == null)
                return new ResultResponse { Status = 400, Message = $"{httpStatus}_{rawResponse}", Data = null };

            if (estimateResp.Meta.Code == 200 && httpStatus == 200)
            {
                var itemDetail = estimateResp.Data?.Item_details?.FirstOrDefault();
                var voucherAmount = (int)(estimateResp.Data?.Total_discount ?? 0);
                if (itemDetail?.Item_type == "FIXED")
                    voucherAmount = (int)itemDetail.Item_value;

                if (estimateResp.Data?.Total_discount > 0)
                {
                    var dataVouchers = new List<DataVoucherPartnerResponse>
                    {
                        new()
                        {
                            Status = "200",
                            Code = request.SerialNo[0],
                            Amount = voucherAmount,
                            Msg = estimateResp.Meta.Message ?? string.Empty
                        }
                    };
                    return new ResultResponse { Status = 200, Message = rawResponse, Data = dataVouchers, MessageTechnical = null };
                }
            }

            var errorMsg = $"Code {estimateResp.Meta.Code} - {estimateResp.Meta.Message}";
            return new ResultResponse { Status = 400, Message = errorMsg, Data = null, MessageTechnical = rawResponse };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OneUHttpService.EstimateAsync failed posNo={PosNo}", request.PosNo);
            return new ResultResponse { Status = 503, Message = ex.Message, Data = null, MessageTechnical = JsonConvert.SerializeObject(ex) };
        }
    }

    private async Task<(bool Success, string Message)> GetTokenAsync(SysWebApiDto dto)
    {
        if (_cache.TryGetValue<string>(TokenCacheKey, out var cached) && !string.IsNullOrEmpty(cached))
            return (true, cached!);

        var tokenRoute = dto.GetRoute("Token");
        if (tokenRoute == null)
            return (false, "Không tìm thấy route Token của ONEU");

        // Notes = audience
        var bodyJson = JsonConvert.SerializeObject(new
        {
            client_id = dto.UserName.Trim(),
            client_secret = dto.Password.Trim(),
            audience = tokenRoute.Notes?.Trim() ?? string.Empty
        });

        var (raw, httpStatus) = await CallApiAsync(dto, null, "Token", "SYSTEM", tokenRoute.Route, bodyJson);

        if (httpStatus == 200 && !string.IsNullOrEmpty(raw))
        {
            var tokenResp = JsonConvert.DeserializeObject<TokenResponseOneU>(raw);
            var accessToken = tokenResp?.Data?.Access_token;
            if (!string.IsNullOrEmpty(accessToken))
            {
                var ttl = (tokenResp!.Data!.Expires_in - 60);
                _cache.Set(TokenCacheKey, accessToken, TimeSpan.FromSeconds(ttl > 0 ? ttl : 60));
                return (true, accessToken!);
            }
        }

        return (false, $"HttpStatus:{httpStatus} response:{raw}");
    }

    private async Task<(string Raw, int HttpStatus)> CallApiAsync(SysWebApiDto dto, string? bearerToken, string function, string posNo, string route, string bodyJson)
    {
        var merchantCode = dto.Description.Trim();
        var publicKey = dto.PublicKey.Trim();
        var privateKeyXml = dto.PrivateKey.Trim();
        int timeout = int.TryParse(dto.Version, out var t) && t > 0 ? t : 30;

        var timestamp = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds().ToString();
        var rawData = $"{merchantCode}:{publicKey}:{timestamp}:{bodyJson}";
        var signature = string.IsNullOrEmpty(privateKeyXml) ? string.Empty : RsaSignatureHelper.Sign(rawData, privateKeyXml, useAscii: true);

        var requestId = Guid.NewGuid().ToString();
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(dto.Host);
        client.Timeout = TimeSpan.FromSeconds(timeout);

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("X-Merchant-ID", merchantCode);
        client.DefaultRequestHeaders.Add("X-Key-ID", publicKey);
        client.DefaultRequestHeaders.Add("X-Timestamp", timestamp);
        client.DefaultRequestHeaders.Add("X-Signature", signature);
        client.DefaultRequestHeaders.Add("X-Request-ID", requestId);

        if (!string.IsNullOrEmpty(bearerToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        _logger.LogInformation("OneU {Function} posNo={PosNo} route={Route}", function, posNo, route);

        var response = await client.PostAsync(route, new StringContent(bodyJson, Encoding.UTF8, "application/json"));
        var raw = await response.Content.ReadAsStringAsync();
        return (raw, (int)response.StatusCode);
    }

    private sealed class TokenResponseOneU
    {
        public MetaOneU? Meta { get; set; }
        public TokenDataOneU? Data { get; set; }
    }
    private sealed class TokenDataOneU
    {
        public string? Access_token { get; set; }
        public int Expires_in { get; set; }
    }
    private sealed class MetaOneU
    {
        public int Code { get; set; }
        public string? Message { get; set; }
    }
    private sealed class EstimateResponseOneU
    {
        public MetaOneU? Meta { get; set; }
        public EstimateDataOneU? Data { get; set; }
    }
    private sealed class EstimateDataOneU
    {
        public decimal Total_discount { get; set; }
        public List<EstimateItemDetail>? Item_details { get; set; }
    }
    private sealed class EstimateItemDetail
    {
        public string? Item_type { get; set; }
        public long Item_value { get; set; }
    }
}
