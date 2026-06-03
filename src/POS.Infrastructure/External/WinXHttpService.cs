using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using POS.Application.Gift.DTOs;
using POS.Application.Gift.Services;
using POS.Application.Shared.Services;

namespace POS.Infrastructure.External;

public class WinXHttpService : IWinXService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISysWebApiConfigService _configService;
    private readonly ILogger<WinXHttpService> _logger;

    private static readonly JsonSerializerSettings CamelCaseSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public WinXHttpService(IHttpClientFactory httpClientFactory, ISysWebApiConfigService configService, ILogger<WinXHttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configService = configService;
        _logger = logger;
    }

    public async Task<(WinXQrCodeResult? Result, string Message)> PosPostTransactionsAsync(MMLSchemeRequest request)
    {
        var dto = await _configService.GetByAppCodeAsync("WINX");
        if (dto == null)
        {
            _logger.LogWarning("WinX config not found in SysWebApi table");
            return (null, "Không tìm thấy cấu hình WinX");
        }

        var apiRoute = dto.GetRoute("PosPostTransactions");
        if (apiRoute == null)
        {
            _logger.LogWarning("WinX route PosPostTransactions not found");
            return (null, "Không tìm thấy route WinX PosPostTransactions");
        }

        // Version field = timeout seconds for WINX
        int timeout = int.TryParse(dto.Version, out int t) && t > 0 ? t : 10;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(dto.Host);
            client.Timeout = TimeSpan.FromSeconds(timeout);

            if (!string.IsNullOrEmpty(dto.UserName) && !string.IsNullOrEmpty(dto.Password))
                client.DefaultRequestHeaders.TryAddWithoutValidation(dto.UserName.Trim(), dto.Password.Trim());

            var body = JsonConvert.SerializeObject(request, CamelCaseSettings);
            _logger.LogInformation("WinX PosPostTransactions posNo={PosNo} orderNo={OrderNo}", request.PosNo, request.OrderNo);

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(apiRoute.Route, content);
            var responseStr = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("WinX response status={Status}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
                return (null, responseStr);

            var winxResp = JsonConvert.DeserializeObject<WinxApiResponse>(responseStr);
            if (winxResp?.Status == 200 && winxResp.Data != null)
            {
                var result = JsonConvert.DeserializeObject<WinXQrCodeResult>(
                    JsonConvert.SerializeObject(winxResp.Data));
                return (result, "OK");
            }

            return (null, responseStr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WinX PosPostTransactions failed orderNo={OrderNo}", request.OrderNo);
            return (null, JsonConvert.SerializeObject(ex));
        }
    }

    public async Task<(string? ResolvedCode, string Message)> ResolveDynamicVouchersAsync(string posNo, string voucher)
    {
        var dto = await _configService.GetByAppCodeAsync("WINX");
        if (dto == null) return (null, "Không tìm thấy cấu hình WinX");

        var apiRoute = dto.GetRoute("GetVoucherCapillary");
        if (apiRoute == null) return (null, "Không tìm thấy route GetVoucherCapillary của WinX");

        int timeout = int.TryParse(dto.Version, out int t) && t > 0 ? t : 10;

        try
        {
            var body = JsonConvert.SerializeObject(new { dynamic_codes = new[] { voucher } });
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(dto.Host);
            client.Timeout = TimeSpan.FromSeconds(timeout);

            if (!string.IsNullOrEmpty(dto.UserName) && !string.IsNullOrEmpty(dto.Password))
                client.DefaultRequestHeaders.TryAddWithoutValidation(dto.UserName.Trim(), dto.Password.Trim());

            _logger.LogInformation("WinX ResolveDynamicVouchers posNo={PosNo} voucher={Voucher}", posNo, voucher);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(apiRoute.Route, content);
            var responseStr = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (null, $"Lỗi kết nối WinX HTTP {(int)response.StatusCode}");

            var resp = JsonConvert.DeserializeObject<WinXDynamicVoucherResponse>(responseStr);
            if (resp?.Status == 200 && resp.Data?.Data?.Count > 0)
                return (resp.Data.Data[0].Capillary_voucher_code, voucher);

            if (resp?.Data?.Errors?.Count > 0)
                return (null, resp.Data.Errors[0].Error ?? "Không tìm thấy voucher");

            return (null, $"Không tìm thấy voucher {voucher}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WinX ResolveDynamicVouchers failed voucher={Voucher}", voucher);
            return (null, ex.Message);
        }
    }

    private sealed class WinxApiResponse
    {
        public int Status { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }

    private sealed class WinXDynamicVoucherResponse
    {
        public int Status { get; set; }
        public WinXDynamicVoucherData? Data { get; set; }
    }
    private sealed class WinXDynamicVoucherData
    {
        public List<WinXDynamicVoucherItem>? Data { get; set; }
        public List<WinXDynamicVoucherError>? Errors { get; set; }
    }
    private sealed class WinXDynamicVoucherItem
    {
        public string? Capillary_voucher_code { get; set; }
    }
    private sealed class WinXDynamicVoucherError
    {
        public string? Error { get; set; }
    }
}
