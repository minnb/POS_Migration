using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using POS.Application.Payment.DTOs;
using POS.Application.Payment.Services;
using POS.Application.Shared.Services;

namespace POS.Infrastructure.External.GotIT;

public class GotITHttpService : IGotITService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISysWebApiConfigService _configService;
    private readonly IConfiguration _appConfig;
    private readonly ILogger<GotITHttpService> _logger;

    public GotITHttpService(
        IHttpClientFactory httpClientFactory,
        ISysWebApiConfigService configService,
        IConfiguration appConfig,
        ILogger<GotITHttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configService = configService;
        _appConfig = appConfig;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, List<DataVoucherPartnerResponse>? Data)> CheckMultipleAsync(CheckVoucherPartnerRequest request)
    {
        var dto = await _configService.GetByAppCodeAsync("GOTIT");
        if (dto == null)
            return (false, "Không tìm thấy cấu hình GOTIT", null);

        var version = (dto.Version ?? string.Empty).ToUpper();
        var routeName = version == "V6" ? "CheckMultipleV6" : "CheckMultiple";
        var apiRoute = dto.GetRoute(routeName);
        if (apiRoute == null)
            return (false, $"Không tìm thấy route GotIT {routeName}", null);

        // Authorization = prefix for PIN; PIN = Authorization + StoreNo
        var pin = dto.Authorization + request.StoreNo;

        // Environment from appsettings (deployment concern, not partner config)
        var environment = (_appConfig["GotIT:Environment"] ?? "PRD").ToUpper();

        var skusInfo = new List<(string Sku, decimal Price, decimal Qty)>();
        if (request.Items?.Count > 0)
        {
            foreach (var item in request.Items)
                skusInfo.Add((GetSku(item.Barcode, environment), item.UnitPrice, item.Qty));
        }

        var grouped = skusInfo
            .GroupBy(x => x.Sku)
            .Select(g => new { Sku = g.Key, Price = (int)g.First().Price, Quantity = g.Sum(x => x.Qty) })
            .ToList<object>();

        var totalBill = grouped.Cast<dynamic>().Sum(x => (decimal)x.Quantity * (int)x.Price);

        var bodyJson = JsonConvert.SerializeObject(new
        {
            pin,
            codes = request.SerialNo,
            bill_number = $"{request.PosNo}_{request.SerialNo[0]}",
            skip_reserved_when_mark_used = true,
            total_bill = totalBill,
            skus_info = grouped
        });

        try
        {
            var response = await CallGotITApiAsync(dto.Host, apiRoute.Route, bodyJson);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = await response.Content.ReadAsStringAsync();
                var gotItResp = JsonConvert.DeserializeObject<GotITResponse>(raw);

                if (gotItResp == null || gotItResp.Data == null)
                    return (false, "Mã code không hợp lệ hoặc không đúng.", null);

                if (!gotItResp.Success)
                    return (false, gotItResp.Message_vi ?? "Voucher không hợp lệ", null);

                return ValidateStatus(gotItResp, version);
            }

            if (response.StatusCode == HttpStatusCode.InternalServerError)
                return (false, $"InternalServerError: {dto.Host}", null);

            var errContent = await response.Content.ReadAsStringAsync();
            return (false, response.StatusCode.ToString(), new List<DataVoucherPartnerResponse>
            {
                new() { Code = string.Empty, Msg = errContent }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GotITHttpService.CheckMultipleAsync failed posNo={PosNo}", request.PosNo);
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Success, string Message, List<DataVoucherPartnerResponse>? Data)> MarkUseMultipleAsync(UpdateStatusVoucherPartnerRequest request)
    {
        var dto = await _configService.GetByAppCodeAsync("GOTIT");
        if (dto == null)
            return (false, "Không tìm thấy cấu hình GOTIT", null);

        var version = (dto.Version ?? string.Empty).ToUpper();
        var routeName = version == "V6" ? "MarkUseMultipleV6" : "MarkUseMultiple";
        var apiRoute = dto.GetRoute(routeName);
        if (apiRoute == null)
            return (false, $"Không tìm thấy route GotIT {routeName}", null);

        var environment = (_appConfig["GotIT:Environment"] ?? "PRD").ToUpper();
        var pin = dto.Authorization + request.StoreNo;

        decimal totalAmount = request.Items?.Sum(x => x.LineAmount) ?? 1100m;
        var skusInfo = new List<(string Sku, decimal Price, decimal Qty)>();
        if (request.Items?.Count > 0)
        {
            foreach (var item in request.Items)
                skusInfo.Add((GetSku(item.Barcode, environment), item.UnitPrice, item.Qty));
        }

        var grouped = skusInfo
            .GroupBy(x => x.Sku)
            .Select(g => new { Sku = g.Key, Price = (int)g.First().Price, Quantity = g.Sum(x => x.Qty) })
            .ToList<object>();

        var bodyJson = JsonConvert.SerializeObject(new
        {
            pin,
            codes = request.SerialNo.Select(x => x.Code).ToList(),
            total_bill = totalAmount,
            bill_number = request.OrderNo,
            skip_reserved_when_mark_used = true,
            skus_info = grouped
        });

        try
        {
            var response = await CallGotITApiAsync(dto.Host, apiRoute.Route, bodyJson);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = await response.Content.ReadAsStringAsync();
                var gotItResp = JsonConvert.DeserializeObject<GotITResponse>(raw);

                if (gotItResp == null || gotItResp.Data == null)
                    return (false, "Mã code không hợp lệ hoặc không đúng.", null);

                if (!gotItResp.Success)
                    return (false, gotItResp.Message_vi ?? "Voucher không hợp lệ", null);

                return ValidateStatus(gotItResp, version);
            }

            var errContent = await response.Content.ReadAsStringAsync();
            return (false, response.StatusCode.ToString(), new List<DataVoucherPartnerResponse>
            {
                new() { Code = string.Empty, Msg = errContent }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GotITHttpService.MarkUseMultipleAsync failed posNo={PosNo} orderNo={OrderNo}", request.PosNo, request.OrderNo);
            return (false, ex.Message, null);
        }
    }

    private static string GetSku(string barcode, string environment)
        => environment == "PRD" ? barcode : "SKU" + barcode;

    private static (bool, string, List<DataVoucherPartnerResponse>?) ValidateStatus(GotITResponse responses, string version)
    {
        if (!responses.Success)
            return (false, responses.Message_vi ?? "Voucher không hợp lệ", null);

        var done = new List<DataVoucherPartnerResponse>();

        if (version == "V6")
        {
            var items = JsonConvert.DeserializeObject<List<VoucherCodeDataV6>>(JsonConvert.SerializeObject(responses.Data));
            if (items == null)
                return (false, responses.Message_vi ?? "Mã code không hợp lệ hoặc không đúng.", null);

            foreach (var item in items)
            {
                var amount = item.Value;
                if (item.Redemptions?.Redemption_value > 0)
                    amount = item.Redemptions.Redemption_value;

                var entry = new DataVoucherPartnerResponse
                {
                    Code = item.Code ?? string.Empty,
                    Amount = amount,
                    Msg = responses.Message_vi ?? string.Empty,
                    Status = item.State?.ToString() ?? "00"
                };

                if (item.Voucher_type?.ToUpper() == "CONDITIONAL")
                {
                    entry.Remark = JsonConvert.SerializeObject(item.Conditions?.Redeemable_skus);
                    entry.IsApplySku = true;
                }
                done.Add(entry);
            }
        }
        else
        {
            var items = JsonConvert.DeserializeObject<List<VoucherCodeDataV4>>(JsonConvert.SerializeObject(responses.Data));
            if (items == null)
                return (false, responses.Message_vi ?? "Mã code không hợp lệ hoặc không đúng.", null);

            foreach (var item in items)
            {
                done.Add(new DataVoucherPartnerResponse
                {
                    Code = item.Code ?? string.Empty,
                    Amount = (int)(item.Product?.Value ?? 0),
                    Msg = item.Product?.Product_name_vi ?? string.Empty,
                    Status = item.State?.ToString() ?? "00"
                });
            }
        }

        return (true, responses.Message_vi ?? "OK", done);
    }

    private async Task<HttpResponseMessage> CallGotITApiAsync(string host, string route, string bodyJson)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(host);
        client.Timeout = TimeSpan.FromSeconds(35);
        return await client.PostAsync(route, new StringContent(bodyJson, Encoding.UTF8, "application/json"));
    }

    private sealed class GotITResponse
    {
        public bool Success { get; set; }
        public string? Return_code { get; set; }
        public string? Message_vi { get; set; }
        public List<object>? Data { get; set; }
    }
    private sealed class VoucherCodeDataV4
    {
        public string? Code { get; set; }
        public ProductGotItV4? Product { get; set; }
        public int? State { get; set; }
    }
    private sealed class ProductGotItV4
    {
        public string? Product_name_vi { get; set; }
        public decimal? Value { get; set; }
    }
    private sealed class VoucherCodeDataV6
    {
        public string? Code { get; set; }
        public int? State { get; set; }
        public int Value { get; set; }
        public string? Voucher_type { get; set; }
        public RedemptionsV6? Redemptions { get; set; }
        public ConditionsV6? Conditions { get; set; }
    }
    private sealed class RedemptionsV6 { public int Redemption_value { get; set; } }
    private sealed class ConditionsV6 { public List<string>? Redeemable_skus { get; set; } }
}
