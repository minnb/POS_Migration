using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using POS.Common.Dtos;
using POS.Common.Dtos.GotIT;
using POS.Common.Dtos.PartnerApi;
using POS.Common.Enums;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.AppServices.Partner;

public sealed class GotITService(
    ICentralMDRepository centralMDRepository,
    IKibanaService kibanaService,
    IFileLogHelper fileLogHelper,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration
) : IGotITAppService
{
    private const string NotFoundConfig = "Không tìm thấy thông tin cấu hình";

    // GotIT API expects lowercase keys (pin, codes, bill_number, ...)
    private static readonly JsonSerializerSettings _camelCaseSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public async Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> CheckMultiple(
        CheckVoucherPartnerPOSRequest request, CancellationToken ct = default)
    {
        try
        {
            string function = "CheckMultiple";
            var sysWebApiDto = await centralMDRepository.GetSysWebApiAsync(PartnerEnum.GOTIT.ToString(), ct);
            if (sysWebApiDto == null) return Fail(NotFoundConfig);

            if (sysWebApiDto.Version == "V6") function = "CheckMultipleV6";

            var route = sysWebApiDto.SysWebApiRoute?.FirstOrDefault(x =>
                string.Equals(x.Name, function, StringComparison.OrdinalIgnoreCase));
            if (route == null) return Fail(NotFoundConfig);

            var skusGrouped = BuildSkusInfo(request.Items, sysWebApiDto);
            var payload = new CheckMultipleDto
            {
                Pin = GetPin(sysWebApiDto, request.StoreNo),
                Codes = request.SerialNo,
                Bill_number = request.PosNo + "_" + request.SerialNo[0],
                Skip_reserved_when_mark_used = true,
                Total_bill = skusGrouped.Sum(x => x.Quantity * x.Price),
                Skus_info = skusGrouped
            };

            var response = await CallApiGotIT(sysWebApiDto, request.PosNo, route.Route ?? "",
                JsonConvert.SerializeObject(payload, _camelCaseSettings), ct);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var readData = await response.Content.ReadAsStringAsync(ct);
                var result = JsonConvert.DeserializeObject<GotITResponse>(readData);
                if (result == null || result.Data == null)
                    return Fail("Mã code không hợp lệ hoặc không đúng.");
                if (!result.Success)
                    return Fail(result.Message_vi ?? "Voucher không hợp lệ");
                return ValidateStatusGotIt(result, sysWebApiDto.Version ?? "V4");
            }
            else if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                return Fail($"InternalServerError: {sysWebApiDto.Host}");
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                return new(false, response.StatusCode.ToString(),
                    [new DataVoucherPartnerResponse { Code = "", Msg = content }]);
            }
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    public async Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> MarkUseMultiple(
        UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default)
    {
        try
        {
            string function = "MarkUseMultiple";
            var sysWebApiDto = await centralMDRepository.GetSysWebApiAsync(PartnerEnum.GOTIT.ToString(), ct);
            if (sysWebApiDto == null) return Fail(NotFoundConfig);

            if (sysWebApiDto.Version == "V6") function = "MarkUseMultipleV6";

            var route = sysWebApiDto.SysWebApiRoute?.FirstOrDefault(x =>
                string.Equals(x.Name, function, StringComparison.OrdinalIgnoreCase));
            if (route == null) return Fail(NotFoundConfig);

            decimal totalAmount = 1100;
            List<Skus_info_gotit> skusInfo = [];
            if (request.Items != null && request.Items.Count > 0)
            {
                totalAmount = request.Items.Sum(x => x.LineAmount);
                foreach (var item in request.Items)
                {
                    skusInfo.Add(new Skus_info_gotit
                    {
                        Sku = GetSKU(item.Barcode ?? ""),
                        Price = item.UnitPrice,
                        Quantity = item.Qty
                    });
                }
            }

            var skusGrouped = skusInfo
                .GroupBy(p => p.Sku!.Trim())
                .Select(g => new Skus_info_gotit
                {
                    Sku = g.Key,
                    Price = g.First().Price,
                    Quantity = g.Sum(p => p.Quantity)
                }).ToList();

            var payload = new MarkUseMultipleDto
            {
                Pin = GetPin(sysWebApiDto, request.StoreNo),
                Codes = request.SerialNo?.Select(x => x.Code).ToList() ?? [],
                Total_bill = totalAmount,
                Bill_number = request.OrderNo,
                Skip_reserved_when_mark_used = true,
                Skus_info = skusGrouped
            };

            var response = await CallApiGotIT(sysWebApiDto, request.PosNo, route.Route ?? "",
                JsonConvert.SerializeObject(payload, _camelCaseSettings), ct);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var readData = await response.Content.ReadAsStringAsync(ct);
                var result = JsonConvert.DeserializeObject<GotITResponse>(readData);
                if (result == null || result.Data == null)
                    return Fail("Mã code không hợp lệ hoặc không đúng.");
                if (!result.Success)
                    return Fail(result.Message_vi ?? "Voucher không hợp lệ");
                return ValidateStatusGotIt(result, sysWebApiDto.Version ?? "V4");
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                kibanaService.LogInfo(
                    $"[{AppCodeMessageEnum.BLUEPOS_GOTIT}] Lỗi GotIt trả về khi thanh toán",
                    request.PosNo,
                    $"{response.StatusCode}_{response.ReasonPhrase}_{content} | order={request.OrderNo}");
                return new(false, response.StatusCode.ToString(),
                    [new DataVoucherPartnerResponse { Code = "", Msg = content }]);
            }
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("MarkUseMultiple Exception", ex);
            return Fail(ex.Message);
        }
    }

    private async Task<HttpResponseMessage> CallApiGotIT(
        SysWebApiDto sysWebApiDto, string posNo, string endPoint, string bodyJson, CancellationToken ct)
    {
        try
        {
            HttpClient client;
            if (!string.IsNullOrEmpty(sysWebApiDto.HttpProxy))
            {
                // TODO: refactor — per-request HttpClient with proxy risks socket exhaustion
                var handler = new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = new WebProxy(sysWebApiDto.HttpProxy)
                };
                client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(sysWebApiDto.Host!),
                    Timeout = TimeSpan.FromSeconds(35)
                };
            }
            else
            {
                client = httpClientFactory.CreateClient("GotIT");
                client.BaseAddress = new Uri(sysWebApiDto.Host!);
                client.Timeout = TimeSpan.FromSeconds(35);
            }

            var st = Stopwatch.StartNew();
            kibanaService.LogRequest(sysWebApiDto.Host + endPoint, posNo, bodyJson);

            var response = await client.PostAsync(endPoint,
                new StringContent(bodyJson, Encoding.UTF8, "application/json"), ct);
            st.Stop();

            // Buffer content so callers can read it without exhausting the stream
            await response.Content.LoadIntoBufferAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                kibanaService.LogInfo(
                    $"[{AppCodeMessageEnum.BLUEPOS_GOTIT}] WebApi {sysWebApiDto.Host} lỗi {response.StatusCode}_{response.ReasonPhrase}",
                    posNo,
                    $"Request: {bodyJson}_:::_Response: {response.StatusCode}_{response.ReasonPhrase}_{content}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            kibanaService.LogResponse(sysWebApiDto.Host + endPoint, posNo, st.ElapsedMilliseconds, "",
                $"{bodyJson} response: httpsStatus: {response.StatusCode} {responseBody}");

            return response;
        }
        catch (AggregateException ex)
        {
            kibanaService.LogException(sysWebApiDto.Host ?? "", posNo, 0, endPoint, JsonConvert.SerializeObject(ex));
            if (ex.InnerException != null) fileLogHelper.WriteExpLogs("CallApiGotIT InnerException:", ex);
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }

    private Tuple<bool, string, List<DataVoucherPartnerResponse>> ValidateStatusGotIt(
        GotITResponse responses, string version)
    {
        if (responses.Data == null)
            return Fail($"{responses.Return_code}: {responses.Message_vi}" ?? "Mã code không hợp lệ hoặc không đúng.");

        if (!responses.Success)
            return new(false, responses.Message_vi ?? "Voucher không hợp lệ", []);

        List<DataVoucherPartnerResponse> codeDone = [];

        if (version == "V6")
        {
            var voucherCodeData = JsonConvert.DeserializeObject<List<VoucherCodeDataV2>>(
                JsonConvert.SerializeObject(responses.Data));
            if (voucherCodeData == null)
                return Fail($"{responses.Return_code}: {responses.Message_vi}" ?? "Mã code không hợp lệ hoặc không đúng.");

            foreach (var item in voucherCodeData)
            {
                var voucherAmount = item.Value;
                if (item.Redemptions?.Redemption_value > 0)
                    voucherAmount = item.Redemptions.Redemption_value;

                var dataVoucher = new DataVoucherPartnerResponse
                {
                    Code = item.Code ?? "",
                    Amount = voucherAmount,
                    Msg = responses.Message_vi ?? "",
                    Status = item.State?.ToString() ?? "00"
                };
                if (string.Equals(item.Voucher_type, VoucherTypeGotIt.conditional.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    dataVoucher.Remark = JsonConvert.SerializeObject(item.Conditions?.Redeemable_skus);
                    dataVoucher.IsApplySku = true;
                }
                codeDone.Add(dataVoucher);
            }
        }
        else
        {
            var voucherCodeData = JsonConvert.DeserializeObject<List<VoucherCodeData>>(
                JsonConvert.SerializeObject(responses.Data));
            if (voucherCodeData == null)
                return Fail($"{responses.Return_code}: {responses.Message_vi}" ?? "Mã code không hợp lệ hoặc không đúng.");

            foreach (var item in voucherCodeData)
            {
                codeDone.Add(new DataVoucherPartnerResponse
                {
                    Code = item.Code ?? "",
                    Amount = item.Product?.Value != null ? (int)item.Product.Value : 0,
                    Msg = item.Product?.Product_name_vi ?? "",
                    Remark = "",
                    Status = item.State?.ToString() ?? "00"
                });
            }
        }

        return new(true, responses.Message_vi ?? "OK", codeDone);
    }

    private List<Skus_info_gotit> BuildSkusInfo(
        List<SkuApplyVoucherPartner>? items, SysWebApiDto sysWebApiDto)
    {
        if (items == null || items.Count == 0) return [];
        return items
            .Select(item => new Skus_info_gotit
            {
                Sku = GetSKU(item.Barcode ?? ""),
                Price = item.UnitPrice,
                Quantity = item.Qty
            })
            .GroupBy(p => p.Sku!.Trim())
            .Select(g => new Skus_info_gotit
            {
                Sku = g.Key,
                Price = g.First().Price,
                Quantity = g.Sum(p => p.Quantity)
            }).ToList();
    }

    private string GetPin(SysWebApiDto sysWebApiDto, string storeNo)
        => sysWebApiDto.Authorization + storeNo;

    private string GetSKU(string barcode)
    {
        if (configuration["AppSettings:Environment"] == "PRD")
            return barcode;
        return "SKU" + barcode;
    }

    private static Tuple<bool, string, List<DataVoucherPartnerResponse>> Fail(string message)
        => new(false, message, []);
}
