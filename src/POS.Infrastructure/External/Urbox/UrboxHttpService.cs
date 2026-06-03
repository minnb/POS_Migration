using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using POS.Application.Payment.DTOs;
using POS.Application.Payment.Services;
using POS.Application.Shared.DTOs;
using POS.Application.Shared.Services;
using POS.Infrastructure.Helpers;

namespace POS.Infrastructure.External.Urbox;

public class UrboxHttpService : IUrboxService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISysWebApiConfigService _configService;
    private readonly ILogger<UrboxHttpService> _logger;

    public UrboxHttpService(IHttpClientFactory httpClientFactory, ISysWebApiConfigService configService, ILogger<UrboxHttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configService = configService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, List<DataVoucherPartnerResponse>? Data)> CheckSerialAsync(CheckVoucherPartnerRequest request)
    {
        var dto = await _configService.GetByAppCodeAsync("URBOX");
        if (dto == null)
            return (false, "Không tìm thấy cấu hình URBOX", null);

        var apiRoute = dto.GetRoute("CheckCodeUrbox");
        if (apiRoute == null)
            return (false, "Không tìm thấy route CheckCodeUrbox", null);

        // Description = semicolon-separated excluded stores
        if (!string.IsNullOrEmpty(dto.Description))
        {
            var excluded = dto.Description.Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (excluded.Contains(request.StoreNo))
                return (false, $"Cửa hàng {request.StoreNo} không sử dụng Urbox", null);
        }

        var version = (dto.Version ?? string.Empty).ToUpper();
        string codesJoined = string.Join(",", request.SerialNo);

        string bodyJson;
        if (version is "V2" or "V3")
        {
            int brandId = int.TryParse(dto.Authorization, out var bid) ? bid : 0;
            bodyJson = JsonConvert.SerializeObject(new
            {
                amount = 1,
                code = codesJoined,
                brand_id = brandId,
                store_id = request.StoreNo,
                terminal_id = request.PosNo
            });
        }
        else
        {
            bodyJson = JsonConvert.SerializeObject(new
            {
                amount = 1,
                code = codesJoined,
                store_id = dto.Authorization,
                terminal_id = request.PosNo
            });
        }

        try
        {
            var response = await CallUrboxApiAsync(dto, apiRoute.Route, bodyJson);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = await response.Content.ReadAsStringAsync();
                var resultList = TryDeserializeList<UrboxCheckResponse>(raw);

                if (resultList?.Count > 0)
                {
                    if ((version is "V2" or "V3") && request.Items?.Count > 0)
                    {
                        var skuApply = resultList.FirstOrDefault()?.data?.sku_apply;
                        if (skuApply?.Count > 0)
                        {
                            var skuResult = await CheckWithSkuAsync(dto, apiRoute.Route, request, skuApply, codesJoined);
                            if (skuResult.HasValue) return skuResult.Value;
                        }
                    }
                    return ValidateStatus(resultList, isApplySku: false);
                }

                var errObj = JsonConvert.DeserializeObject<UrboxErrorResponse>(raw);
                return (false, errObj?.Zero?.data?.msg ?? "Lỗi convert dữ liệu json", null);
            }

            var errContent = await response.Content.ReadAsStringAsync();
            return (false, response.StatusCode.ToString(), new List<DataVoucherPartnerResponse>
            {
                new() { Msg = errContent }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UrboxHttpService.CheckSerialAsync failed posNo={PosNo}", request.PosNo);
            return (false, ex.Message, null);
        }
    }

    private async Task<(bool, string, List<DataVoucherPartnerResponse>?)?> CheckWithSkuAsync(
        SysWebApiDto dto, string route,
        CheckVoucherPartnerRequest request, List<string> skuApply, string codesJoined)
    {
        var products = new List<object>();
        foreach (var sku in skuApply)
        {
            var matching = request.Items!.Where(x => x.Barcode.Trim() == sku).ToList();
            foreach (var item in matching)
                products.Add(new { product_code = sku, quantity = (int)item.Qty, total_price = (int)item.LineAmount });
        }

        if (products.Count == 0)
            return (false, "Voucher không áp dụng cho sản phẩm trong giỏ hàng", null);

        int brandId = int.TryParse(dto.Authorization, out var bid) ? bid : 0;
        var requestId = $"{request.PosNo}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var skuBodyJson = JsonConvert.SerializeObject(new
        {
            amount = 1,
            code = codesJoined,
            brand_id = brandId,
            store_id = dto.Authorization,
            terminal_id = request.PosNo,
            has_order_detail = 1,
            products,
            bill_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            bill_id = requestId
        });

        var skuResponse = await CallUrboxApiAsync(dto, route, skuBodyJson);
        if (skuResponse.StatusCode == HttpStatusCode.OK)
        {
            var raw2 = await skuResponse.Content.ReadAsStringAsync();
            var skuList = TryDeserializeList<UrboxCheckResponse>(raw2);
            if (skuList?.Count > 0)
                return ValidateStatus(skuList, isApplySku: true);

            var errObj = JsonConvert.DeserializeObject<UrboxErrorResponse>(raw2);
            return (false, errObj?.Zero?.data?.msg ?? "Lỗi convert dữ liệu json", null);
        }

        return null;
    }

    private static (bool, string, List<DataVoucherPartnerResponse>?) ValidateStatus(List<UrboxCheckResponse> responses, bool isApplySku)
    {
        var errors = new List<DataVoucherPartnerResponse>();
        var done = new List<DataVoucherPartnerResponse>();

        foreach (var item in responses)
        {
            var entry = new DataVoucherPartnerResponse
            {
                Code = item.data?.code ?? string.Empty,
                Amount = item.data?.amount ?? 0,
                Msg = item.data?.msg ?? string.Empty,
                Remark = string.Empty,
                Status = item.status.ToString(),
                IsApplySku = isApplySku
            };

            if (item.done == 0 || item.status != 101)
                errors.Add(entry);
            else
                done.Add(entry);
        }

        if (errors.Count > 0)
            return (false, errors[0].Msg, errors);
        if (done.Count == responses.Count)
            return (true, done[0].Msg, done);

        return (true, responses[0].data?.msg ?? string.Empty, new List<DataVoucherPartnerResponse>
        {
            new()
            {
                Code = responses[0].data?.code ?? string.Empty,
                Amount = responses[0].data?.amount ?? 0,
                Msg = responses[0].data?.msg ?? string.Empty,
                Status = responses[0].status.ToString()
            }
        });
    }

    public async Task<(bool Success, string Message, List<DataVoucherPartnerResponse>? Data)> PayCodeAsync(UpdateStatusVoucherPartnerRequest request)
    {
        var dto = await _configService.GetByAppCodeAsync("URBOX");
        if (dto == null)
            return (false, "Không tìm thấy cấu hình URBOX", null);

        if (!string.IsNullOrEmpty(dto.Description))
        {
            var excluded = dto.Description.Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (excluded.Contains(request.StoreNo))
                return (false, $"Cửa hàng {request.StoreNo} không sử dụng Urbox", null);
        }

        var apiRoute = dto.GetRoute("PayCodeUrbox");
        if (apiRoute == null)
            return (false, "Không tìm thấy route PayCodeUrbox", null);

        var version = (dto.Version ?? string.Empty).ToUpper();
        int brandId = int.TryParse(dto.Authorization, out var bid) ? bid : 0;
        var codesJoined = string.Join(",", request.SerialNo.Select(x => x.Code));
        int totalAmount = (int)request.SerialNo.Sum(x => x.Amount);
        string bodyJson;

        if (version == "V3")
        {
            if (request.Items == null || request.Items.Count == 0)
                return (false, "POS không gửi danh sách sản phẩm", null);

            var products = request.Items
                .GroupBy(x => x.Barcode.Trim())
                .Select(g => new { product_code = g.Key, quantity = (int)g.Sum(x => x.Qty), total_price = (int)g.Sum(x => x.LineAmount) })
                .ToList<object>();

            bodyJson = JsonConvert.SerializeObject(new
            {
                amount = totalAmount,
                code = codesJoined,
                brand_id = brandId,
                store_id = request.StoreNo,
                terminal_id = request.PosNo,
                bill_id = request.OrderNo,
                has_order_detail = 1,
                products,
                bill_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
            });
        }
        else if (version == "V0" && request.Items?.Count > 0)
        {
            // V0: check first to get approved SKU list, then pay with matched products
            var checkReq = new CheckVoucherPartnerRequest
            {
                Partner = request.Partner,
                PosNo = request.PosNo,
                StoreNo = request.StoreNo,
                SerialNo = request.SerialNo.Select(x => x.Code).ToList(),
                Items = request.Items
            };
            var checkResult = await CheckSerialAsync(checkReq);
            List<object> products;
            if (checkResult.Success && checkResult.Data?.Any(x => x.IsApplySku) == true)
            {
                // Build products from approved SKUs matched against items
                var approvedCodes = checkResult.Data.Where(x => x.IsApplySku).Select(x => x.Code).ToHashSet();
                products = request.Items
                    .Where(i => approvedCodes.Contains(i.Barcode.Trim()))
                    .GroupBy(x => x.Barcode.Trim())
                    .Select(g => new { product_code = g.Key, quantity = (int)g.Sum(x => x.Qty), total_price = (int)g.Sum(x => x.LineAmount) })
                    .ToList<object>();
            }
            else
            {
                products = request.Items
                    .GroupBy(x => x.Barcode.Trim())
                    .Select(g => new { product_code = g.Key, quantity = (int)g.Sum(x => x.Qty), total_price = (int)g.Sum(x => x.LineAmount) })
                    .ToList<object>();
            }

            bodyJson = JsonConvert.SerializeObject(new
            {
                amount = totalAmount,
                code = codesJoined,
                brand_id = brandId,
                store_id = request.StoreNo,
                terminal_id = request.PosNo,
                bill_id = request.OrderNo,
                has_order_detail = 1,
                products,
                bill_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
            });
        }
        else
        {
            // V2 or default: no products
            bodyJson = JsonConvert.SerializeObject(new
            {
                amount = totalAmount,
                code = codesJoined,
                brand_id = brandId,
                store_id = request.StoreNo,
                terminal_id = request.PosNo,
                bill_id = request.OrderNo
            });
        }

        try
        {
            var response = await CallUrboxApiAsync(dto, apiRoute.Route, bodyJson);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = await response.Content.ReadAsStringAsync();
                var resultList = TryDeserializeList<UrboxCheckResponse>(raw);
                if (resultList != null)
                    return ValidateStatus(resultList, isApplySku: false);

                return (false, "Lỗi convert dữ liệu json", null);
            }

            var errContent = await response.Content.ReadAsStringAsync();
            // Try error envelope (PayErrorUrboxResponse has { Original: List<CheckUrboxResponse> })
            try
            {
                var payErr = JsonConvert.DeserializeObject<PayErrorUrboxResponse>(errContent);
                if (payErr?.Original?.Count > 0)
                    return ValidateStatus(payErr.Original, isApplySku: false);
            }
            catch { /* fall through */ }

            return (false, response.StatusCode.ToString(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UrboxHttpService.PayCodeAsync failed posNo={PosNo} orderNo={OrderNo}", request.PosNo, request.OrderNo);
            return (false, ex.Message, null);
        }
    }

    private async Task<HttpResponseMessage> CallUrboxApiAsync(SysWebApiDto dto, string route, string bodyJson)
    {
        var signature = RsaSignatureHelper.Sign(bodyJson, dto.PrivateKey, useAscii: false);
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(dto.Host);
        client.Timeout = TimeSpan.FromSeconds(45);
        client.DefaultRequestHeaders.Add("App-Id", dto.UserName.Trim());
        client.DefaultRequestHeaders.Add("App-Secret", dto.Password.Trim());
        client.DefaultRequestHeaders.Add("Signature", signature);

        return await client.PostAsync(route, new StringContent(bodyJson, Encoding.UTF8, "application/json"));
    }

    private static List<T>? TryDeserializeList<T>(string json)
    {
        try { return JsonConvert.DeserializeObject<List<T>>(json); }
        catch { return null; }
    }

    private sealed class UrboxCheckResponse
    {
        public int done { get; set; }
        public int status { get; set; }
        public UrboxData? data { get; set; }
    }
    private sealed class UrboxData
    {
        public string? msg { get; set; }
        public string? code { get; set; }
        public int amount { get; set; }
        public List<string>? sku_apply { get; set; }
    }
    private sealed class UrboxErrorResponse
    {
        [JsonProperty("0")]
        public UrboxCheckResponse? Zero { get; set; }
    }
    private sealed class PayErrorUrboxResponse
    {
        public List<UrboxCheckResponse>? Original { get; set; }
    }
}
