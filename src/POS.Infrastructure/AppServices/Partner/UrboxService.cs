using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using POS.Common.Dtos;
using POS.Common.Dtos.PartnerApi;
using POS.Common.Enums;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.AppServices.Partner;

public sealed class UrboxService(
    ICentralMDRepository centralMDRepository,
    IKibanaService kibanaService,
    IFileLogHelper fileLogHelper
) : IUrboxAppService
{
    private const string NotFoundConfig = "Không tìm thấy thông tin cấu hình";

    public async Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> CheckSerialUrbox(
        CheckVoucherPartnerPOSRequest request, CancellationToken ct = default)
    {
        long responseTime = 0;
        var sysWebApiDto = await centralMDRepository.GetSysWebApiAsync(PartnerEnum.URBOX.ToString(), ct);
        if (sysWebApiDto == null) return Fail4(NotFoundConfig);
        if (!CheckStoreUsedUrbox(sysWebApiDto, request.StoreNo))
            return Fail4($"Cửa hàng {request.StoreNo} không sử dụng Urbox");

        var route = sysWebApiDto.SysWebApiRoute?.FirstOrDefault(x =>
            string.Equals(x.Name, "CheckCodeUrbox", StringComparison.OrdinalIgnoreCase));
        if (route == null) return Fail4(NotFoundConfig);

        try
        {
            // inline: StringHelper.RandomRequestID(posNo, true)
            string requestID = $"{request.PosNo}_{Guid.NewGuid():N}";
            string dataBody;

            if (string.Equals(sysWebApiDto.Version, "V2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(sysWebApiDto.Version, "V3", StringComparison.OrdinalIgnoreCase))
            {
                var payload = new CheckUrboxRequestV2
                {
                    amount = 1,
                    // inline: StringHelper.ConvertArrayToString(arr, ',')
                    code = string.Join(",", request.SerialNo),
                    brand_id = int.TryParse(sysWebApiDto.Authorization, out var bid) ? bid : 0,
                    store_id = request.StoreNo,
                    terminal_id = request.PosNo
                };
                dataBody = JsonConvert.SerializeObject(payload);
            }
            else
            {
                var payload = new CheckUrboxRequest
                {
                    amount = 1,
                    code = string.Join(",", request.SerialNo),
                    store_id = sysWebApiDto.Authorization ?? "",
                    terminal_id = request.PosNo
                };
                dataBody = JsonConvert.SerializeObject(payload);
            }

            var (response, elapsed) = await CallApiUrbox(sysWebApiDto, request.PosNo, route.Route ?? "", dataBody, ct);
            responseTime = elapsed;
            await response.Content.LoadIntoBufferAsync();
            kibanaService.LogResponse(sysWebApiDto.Host + route.Route, request.PosNo, responseTime, "",
                JsonConvert.SerializeObject(request.SerialNo) + "|@|" + await response.Content.ReadAsStringAsync(ct));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var readData = await response.Content.ReadAsStringAsync(ct);
                var resultResponse = JsonConvert.DeserializeObject<List<CheckUrboxResponse>>(readData);

                if (resultResponse != null && resultResponse.Count > 0)
                {
                    var checkProduct = await CheckUrboxWithProducts(sysWebApiDto, route, request, resultResponse, requestID, ct);
                    if (checkProduct.hasProducts && checkProduct.success)
                        return ValidateStatusUrbox(checkProduct.responses!, checkProduct.products, true);
                    else if (checkProduct.hasProducts && !checkProduct.success)
                        return Fail4(checkProduct.errorMsg);
                    // voucher old — no matching products
                    return ValidateStatusUrbox(resultResponse, checkProduct.products, false);
                }
                else
                {
                    var errResponse = JsonConvert.DeserializeObject<CheckUrboxErrorResponse>(readData);
                    string messErr = "Lỗi convert dữ liệu json";
                    if (errResponse?.Zero?.data != null)
                        messErr = errResponse.Zero.data.msg ?? messErr;
                    return Fail4(messErr);
                }
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                return new(false, response.StatusCode.ToString(),
                    [new DataVoucherPartnerResponse { Msg = content }], []);
            }
        }
        catch (Exception ex)
        {
            return Fail4(ex.Message);
        }
    }

    public async Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> PayCodelUrbox(
        UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default)
    {
        long responseTime = 0;
        var sysWebApiDto = await centralMDRepository.GetSysWebApiAsync(PartnerEnum.URBOX.ToString(), ct);
        if (sysWebApiDto == null) return Fail4(NotFoundConfig);
        if (!CheckStoreUsedUrbox(sysWebApiDto, request.StoreNo))
            return Fail4($"Cửa hàng {request.StoreNo} không sử dụng Urbox");

        var route = sysWebApiDto.SysWebApiRoute?.FirstOrDefault(x =>
            string.Equals(x.Name, "PayCodeUrbox", StringComparison.OrdinalIgnoreCase));
        if (route == null) return Fail4(NotFoundConfig);

        string stringResponse = "";
        string dataBody;

        if (string.Equals(sysWebApiDto.Version, "V0", StringComparison.OrdinalIgnoreCase))
        {
            bool isApplySku = false;
            List<UrboxProducts>? skuUrbox = null;

            if (request.Items != null && request.Items.Count > 0)
            {
                var checkRequest = new CheckVoucherPartnerPOSRequest
                {
                    Partner = request.Partner,
                    PosNo = request.PosNo,
                    StoreNo = request.StoreNo,
                    SerialNo = request.SerialNo?.Select(x => x.Code).ToList() ?? [],
                    Items = request.Items
                };
                var checkVoucher = await CheckSerialUrbox(checkRequest, ct);
                if (checkVoucher.Item1)
                {
                    isApplySku = true;
                    skuUrbox = checkVoucher.Item4;
                }
            }

            if (isApplySku && skuUrbox != null && skuUrbox.Count > 0)
            {
                var resultProducts = skuUrbox
                    .GroupBy(p => p.product_code.Trim())
                    .Select(g => new UrboxProducts
                    {
                        product_code = g.Key,
                        quantity = g.Sum(x => x.quantity),
                        total_price = g.Sum(x => x.total_price)
                    }).ToList();

                var payload = new PayCodeToUrboxRequestProducts
                {
                    amount = (int)(request.SerialNo?.Sum(x => x.Amount) ?? 0),
                    code = string.Join(",", request.SerialNo?.Select(x => x.Code) ?? []),
                    brand_id = int.TryParse(sysWebApiDto.Authorization, out var bid) ? bid : 0,
                    store_id = request.StoreNo,
                    terminal_id = request.PosNo,
                    bill_id = request.OrderNo,
                    has_order_detail = 1,
                    products = resultProducts,
                    // inline: DateTimeHelper.UnixTimestampString()
                    bill_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
                };
                dataBody = JsonConvert.SerializeObject(payload);
            }
            else
            {
                var payload = new PayCodeToUrboxRequestV2
                {
                    amount = (int)(request.SerialNo?.Sum(x => x.Amount) ?? 0),
                    code = string.Join(",", request.SerialNo?.Select(x => x.Code) ?? []),
                    brand_id = int.TryParse(sysWebApiDto.Authorization, out var bid2) ? bid2 : 0,
                    store_id = request.StoreNo,
                    terminal_id = request.PosNo,
                    bill_id = request.OrderNo
                };
                dataBody = JsonConvert.SerializeObject(payload);
            }
        }
        else if (string.Equals(sysWebApiDto.Version, "V3", StringComparison.OrdinalIgnoreCase))
        {
            if (request.Items == null || request.Items.Count == 0)
                return Fail4("POS không gửi danh sách sản phẩm");

            var skuUrbox = request.Items.Select(item => new UrboxProducts
            {
                product_code = item.Barcode ?? "",
                quantity = (int)item.Qty,
                total_price = (int)item.LineAmount
            }).ToList();

            var resultProducts = skuUrbox
                .GroupBy(p => p.product_code.Trim())
                .Select(g => new UrboxProducts
                {
                    product_code = g.Key,
                    quantity = g.Sum(x => x.quantity),
                    total_price = g.Sum(x => x.total_price)
                }).ToList();

            var payload = new PayCodeToUrboxRequestProducts
            {
                amount = (int)(request.SerialNo?.Sum(x => x.Amount) ?? 0),
                code = string.Join(",", request.SerialNo?.Select(x => x.Code) ?? []),
                brand_id = int.TryParse(sysWebApiDto.Authorization, out var bid3) ? bid3 : 0,
                store_id = request.StoreNo,
                terminal_id = request.PosNo,
                bill_id = request.OrderNo,
                has_order_detail = 1,
                products = resultProducts,
                bill_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
            };
            dataBody = JsonConvert.SerializeObject(payload);
        }
        else // V2 default
        {
            var payload = new PayCodeToUrboxRequestV2
            {
                amount = (int)(request.SerialNo?.Sum(x => x.Amount) ?? 0),
                code = string.Join(",", request.SerialNo?.Select(x => x.Code) ?? []),
                brand_id = int.TryParse(sysWebApiDto.Authorization, out var bid4) ? bid4 : 0,
                store_id = request.StoreNo,
                terminal_id = request.PosNo,
                bill_id = request.OrderNo
            };
            dataBody = JsonConvert.SerializeObject(payload);
        }

        try
        {
            kibanaService.LogRequest(sysWebApiDto.Host + route.Route, request.PosNo,
                request.OrderNo + "@" + dataBody);

            var (response, elapsed) = await CallApiUrbox(sysWebApiDto, request.PosNo, route.Route ?? "", dataBody, ct);
            responseTime = elapsed;
            await response.Content.LoadIntoBufferAsync();
            kibanaService.LogResponse(sysWebApiDto.Host + route.Route, request.PosNo, responseTime, "",
                request.OrderNo + "@" + await response.Content.ReadAsStringAsync(ct));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                stringResponse = await response.Content.ReadAsStringAsync(ct);
                var resultResponse = JsonConvert.DeserializeObject<List<CheckUrboxResponse>>(stringResponse);
                if (resultResponse != null)
                    return ValidateStatusUrbox(resultResponse);
                return Fail4("Lỗi convert dữ liệu json");
            }
            else
            {
                return Fail4(response.StatusCode.ToString());
            }
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(stringResponse))
            {
                var checkErrorResponse = JsonConvert.DeserializeObject<PayErrorUrboxResponse>(stringResponse);
                if (checkErrorResponse?.Original != null)
                    return ValidateStatusUrbox(checkErrorResponse.Original);
                kibanaService.LogException(sysWebApiDto.Host + route.Route, request.PosNo,
                    (int)responseTime, "", "Exception @" + stringResponse);
                return Fail4("Lỗi convert dữ liệu json");
            }
            kibanaService.LogException(sysWebApiDto.Host + route.Route, request.PosNo,
                (int)responseTime, "", ex.Message);
            return Fail4("Lỗi dữ liệu trả về từ Urbox");
        }
    }

    private Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>> ValidateStatusUrbox(
        List<CheckUrboxResponse> checkUrboxResponses, List<UrboxProducts>? products = null, bool isApplySku = false)
    {
        List<DataVoucherPartnerResponse> codeErrors = [];
        List<DataVoucherPartnerResponse> codeDone = [];

        foreach (var item in checkUrboxResponses)
        {
            if (item.done == 0 || item.status != 101)
            {
                codeErrors.Add(new DataVoucherPartnerResponse
                {
                    Code = item.data?.code ?? "",
                    Amount = item.data?.amount ?? 0,
                    Msg = item.data?.msg ?? "",
                    Remark = "",
                    Status = item.status.ToString(),
                    IsApplySku = isApplySku
                });
            }
            else if (item.done == 1 && item.status == 101)
            {
                codeDone.Add(new DataVoucherPartnerResponse
                {
                    Code = item.data?.code ?? "",
                    Amount = item.data?.amount ?? 0,
                    Msg = item.data?.msg ?? "",
                    Remark = products != null ? JsonConvert.SerializeObject(products) : "",
                    Status = item.status.ToString(),
                    IsApplySku = isApplySku
                });
            }
        }

        if (codeErrors.Count > 0)
            return new(false, codeErrors[0].Msg, codeErrors, []);

        if (codeDone.Count == checkUrboxResponses.Count)
            return new(true, codeDone[0].Msg, codeDone, products ?? []);

        codeErrors.Add(new DataVoucherPartnerResponse
        {
            Code = checkUrboxResponses[0].data?.code ?? "",
            Amount = checkUrboxResponses[0].data?.amount ?? 0,
            Msg = checkUrboxResponses[0].data?.msg ?? "",
            Remark = "",
            Status = checkUrboxResponses[0].status.ToString()
        });
        return new(true, codeErrors[0].Msg, codeErrors, []);
    }

    private bool CheckStoreUsedUrbox(SysWebApiDto sysWebApi, string storeNo)
    {
        if (!string.IsNullOrEmpty(sysWebApi.Description))
        {
            // inline: StringHelper.ConverStringToArray(str, ';')
            var arrayStore = sysWebApi.Description.Split(';');
            if (arrayStore.Length > 0 && arrayStore.Contains(storeNo))
                return false;
        }
        return true;
    }

    private async Task<(HttpResponseMessage Response, long ElapsedMs)> CallApiUrbox(
        SysWebApiDto sysWebApiDto, string posNo, string endPoint, string bodyJson, CancellationToken ct)
    {
        long milliseconds = 0;
        kibanaService.LogRequest(sysWebApiDto.Host + endPoint, posNo, bodyJson);

        // Urbox requires per-call RSA signature header — cannot pre-configure in IHttpClientFactory
        var handler = new HttpClientHandler
        {
            UseProxy = !string.IsNullOrEmpty(sysWebApiDto.HttpProxy),
            // bypass SSL for test env (matches old ServicePointManager.ServerCertificateValidationCallback)
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        if (handler.UseProxy)
            handler.Proxy = new WebProxy(sysWebApiDto.HttpProxy);

        // TODO: refactor — per-request HttpClient risks socket exhaustion; extract to IHttpClientFactory with DelegatingHandler for signature injection
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(sysWebApiDto.Host!),
            Timeout = TimeSpan.FromSeconds(45)
        };

        try
        {
            var signature = CreateRsaSignature(bodyJson, sysWebApiDto.PrivateKey ?? "");
            client.DefaultRequestHeaders.Add("App-Id", sysWebApiDto.UserName);
            client.DefaultRequestHeaders.Add("App-Secret", sysWebApiDto.Password);
            client.DefaultRequestHeaders.Add("Signature", signature);

            var st = Stopwatch.StartNew();
            var response = await client.PostAsync(endPoint,
                new StringContent(bodyJson, Encoding.UTF8, "application/json"), ct);
            milliseconds = st.ElapsedMilliseconds;
            st.Stop();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                await response.Content.LoadIntoBufferAsync();
                var content = await response.Content.ReadAsStringAsync(ct);
                kibanaService.LogInfo(
                    $"[{AppCodeMessageEnum.BLUEPOS_URBOX}] WebApi: {sysWebApiDto.Host} lỗi {response.StatusCode}",
                    posNo,
                    $"Request: {bodyJson}_<<>>_Response: {response.StatusCode}_{response.ReasonPhrase}_{content}");
            }

            return (response, milliseconds);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return (new HttpResponseMessage(HttpStatusCode.RequestTimeout)
            {
                Content = new StringContent("UrBox request timed out.")
            }, 0);
        }
        catch (AggregateException ex)
        {
            kibanaService.LogException(sysWebApiDto.Host ?? "", posNo, (int)milliseconds, endPoint,
                JsonConvert.SerializeObject(ex));
            if (ex.InnerException != null)
                fileLogHelper.WriteLogs("CallApiUrbox InnerException:" + ex.InnerException.Message);
            return (new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), 0);
        }
    }

    private async Task<(bool success, List<CheckUrboxResponse>? responses, List<UrboxProducts>? products, string errorMsg, bool hasProducts)>
        CheckUrboxWithProducts(SysWebApiDto sysWebApiDto, SysWebApiRoute route,
            CheckVoucherPartnerPOSRequest request, List<CheckUrboxResponse> resultResponse,
            string requestID, CancellationToken ct)
    {
        long responseTime = 0;
        List<UrboxProducts> products = [];

        foreach (var item in request.Items ?? [])
            item.Barcode = item.Barcode?.Trim();

        var skuApply = resultResponse.FirstOrDefault()?.data?.sku_apply;
        if (skuApply == null || skuApply.Count == 0 || request.Items == null || request.Items.Count == 0)
            return (false, null, null, "BadRequest", false);

        foreach (var sku in skuApply)
        {
            var matchingItems = request.Items.Where(x => x.Barcode == sku).ToList();
            foreach (var icheck in matchingItems)
            {
                products.Add(new UrboxProducts
                {
                    product_code = sku,
                    quantity = (int)icheck.Qty,
                    total_price = (int)icheck.LineAmount
                });
            }
        }

        if (products.Count == 0)
            return (false, null, null, "Voucher không áp dụng cho sản phẩm trong giỏ hàng", true);

        var resultProducts = products
            .GroupBy(p => p.product_code.Trim())
            .Select(g => new UrboxProducts
            {
                product_code = g.Key.Trim(),
                quantity = g.Sum(x => x.quantity),
                total_price = g.Sum(x => x.total_price)
            }).ToList();

        var payload = new CheckUrboxRequestV2_SKU
        {
            amount = 1,
            code = string.Join(",", request.SerialNo),
            store_id = sysWebApiDto.Authorization ?? "",
            brand_id = int.TryParse(sysWebApiDto.Authorization, out var bid) ? bid : 0,
            terminal_id = request.PosNo,
            has_order_detail = 1,
            products = resultProducts,
            bill_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            bill_id = requestID
        };

        var (responseSku, elapsed) = await CallApiUrbox(sysWebApiDto, request.PosNo, route.Route ?? "",
            JsonConvert.SerializeObject(payload), ct);
        responseTime = elapsed;
        await responseSku.Content.LoadIntoBufferAsync();
        var readData2 = await responseSku.Content.ReadAsStringAsync(ct);
        kibanaService.LogResponse(sysWebApiDto.Host + route.Route, request.PosNo, responseTime, "",
            $"{JsonConvert.SerializeObject(request.SerialNo)}_{responseSku.StatusCode}_{readData2}");

        if (responseSku.StatusCode == HttpStatusCode.OK)
        {
            var resultResponseSku = JsonConvert.DeserializeObject<List<CheckUrboxResponse>>(readData2);
            if (resultResponseSku != null && resultResponseSku.Count > 0)
                return (true, resultResponseSku, products, "OK", true);

            var errResponse = JsonConvert.DeserializeObject<CheckUrboxErrorResponse>(readData2);
            string messErr = errResponse?.Zero?.data?.msg ?? "Lỗi convert dữ liệu json";
            return (false, null, null, messErr, true);
        }
        else
        {
            var checkError = JsonConvert.DeserializeObject<ResponseMessageUrbox>(readData2);
            kibanaService.LogResponse(sysWebApiDto.Host + route.Route, request.PosNo, responseTime, "",
                $"{JsonConvert.SerializeObject(request.SerialNo)}_{responseSku.StatusCode}_{JsonConvert.SerializeObject(checkError)}");
            return (false, null, null, "BadRequest", true);
        }
    }

    // Inline của UrBox.CreateSignature — dùng RSA.Create() thay RSACryptoServiceProvider cho .NET 10 cross-platform
    private static string CreateRsaSignature(string bodyJson, string privateKeyXml)
    {
        if (string.IsNullOrEmpty(privateKeyXml)) return "";
        var parameters = ParseRsaParametersFromXml(privateKeyXml);
        using var rsa = RSA.Create();
        rsa.ImportParameters(parameters);
        return Convert.ToBase64String(
            rsa.SignData(Encoding.UTF8.GetBytes(bodyJson), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    private static RSAParameters ParseRsaParametersFromXml(string contentXml)
    {
        var parameters = new RSAParameters();
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(contentXml);
        if (!xmlDoc.DocumentElement!.Name.Equals("RSAKeyValue"))
            throw new Exception("Invalid XML RSA key.");
        foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
        {
            switch (node.Name)
            {
                case "Modulus":    parameters.Modulus    = ToBytes(node.InnerText); break;
                case "Exponent":   parameters.Exponent   = ToBytes(node.InnerText); break;
                case "P":          parameters.P          = ToBytes(node.InnerText); break;
                case "Q":          parameters.Q          = ToBytes(node.InnerText); break;
                case "DP":         parameters.DP         = ToBytes(node.InnerText); break;
                case "DQ":         parameters.DQ         = ToBytes(node.InnerText); break;
                case "InverseQ":   parameters.InverseQ   = ToBytes(node.InnerText); break;
                case "D":          parameters.D          = ToBytes(node.InnerText); break;
            }
        }
        return parameters;

        static byte[]? ToBytes(string s) => string.IsNullOrEmpty(s) ? null : Convert.FromBase64String(s);
    }

    private static Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>> Fail4(string message)
        => new(false, message, [], []);
}
