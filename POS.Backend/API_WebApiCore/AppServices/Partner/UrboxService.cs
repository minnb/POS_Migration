
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Security;
using System.Net;
using System.Text;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using TCX.API.Common.Models;
using TCX.API.Common.Helpers;
using TCX.API.Common.Enums;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using System.Threading.Tasks;


namespace TCX.WebApiCore
{
    public interface IUrboxService
    {
        Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> CheckSerialUrbox(CheckVoucherPartnerPOSRequest request);
        Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> PayCodelUrbox(UpdateStatusVoucherPartnerRequest request);
    }
    public class UrboxService : IUrboxService
    {
        private readonly KibanaService _kibanaService;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly string _connectStringDb;
        public UrboxService()
        {
            _kibanaService = new KibanaService();
            _memoryCacheService = new MemoryCacheService();
            _connectStringDb = _memoryCacheService.GetConnectStringCentralMD();
        }
        public async Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> CheckSerialUrbox(CheckVoucherPartnerPOSRequest request)
        {
            long responseTime = 0;
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == PartnerEnum.URBOX.ToString()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, MessConst.NotFounDataConfig, null, null);
            if (!CheckStoreUsedUrbox(_sysWebApiDto, request.StoreNo))
            {
                return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, string.Format("Cửa hàng {0} không sử dụng Urbox", request.StoreNo), null, null);
            }
            var sysWebApiRoute = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "CheckCodeUrbox".ToUpper());
            if (sysWebApiRoute == null) return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, MessConst.NotFounDataConfig, null, null);

            try
            {
                string requestID = StringHelper.RandomRequestID(request.PosNo, true);
                string dataBody = "";
                if (_sysWebApiDto.Version.ToUpper() == "V2" || _sysWebApiDto.Version.ToUpper() == "V3")
                {
                    var dataCheckUrbox = new CheckUrboxRequestV2()
                    {
                        amount = 1,
                        code = StringHelper.ConvertArrayToString(request.SerialNo.ToArray(), ','),
                        brand_id = int.Parse(_sysWebApiDto.Authorization),
                        store_id = request.StoreNo,
                        terminal_id = request.PosNo
                    };
                    dataBody = JsonConvert.SerializeObject(dataCheckUrbox);
                }
                else
                {
                    var dataCheckUrbox = new CheckUrboxRequest()
                    {
                        amount = 1,
                        code = StringHelper.ConvertArrayToString(request.SerialNo.ToArray(), ','),
                        store_id = _sysWebApiDto.Authorization,
                        terminal_id = request.PosNo
                    };
                    dataBody = JsonConvert.SerializeObject(dataCheckUrbox);
                }

                var response = await CallApiUrbox(_sysWebApiDto, request.PosNo, sysWebApiRoute.Route, dataBody);
                responseTime = response.Item2;
                _kibanaService.LogResponse(_sysWebApiDto.Host + sysWebApiRoute.Route, request.PosNo, responseTime, "", JsonConvert.SerializeObject(request.SerialNo) + "|@|" + response.Item1.Content.ReadAsStringAsync());
                if (response.Item1 != null && response.Item1.StatusCode == HttpStatusCode.OK)
                {
                    var readData = response.Item1.Content.ReadAsStringAsync();
                    var resultResponse = ConvertHelper.ToObject<List<CheckUrboxResponse>>(readData.Result);

                    if (resultResponse != null && resultResponse.Count > 0)
                    {
                        var checkProduct = await CheckUrboxWithProducts(_sysWebApiDto, sysWebApiRoute, request, resultResponse, requestID);
                        if (checkProduct.Item1 && checkProduct.Item5)
                        {
                            return ValidateStatusUrbox(checkProduct.Item2, checkProduct.Item3, true);
                        }
                        else if(!checkProduct.Item1 && checkProduct.Item5)
                        {
                            return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, checkProduct.Item4, null, null);
                        }
                        //voucher old
                        return ValidateStatusUrbox(resultResponse, checkProduct.Item3, false);
                    }
                    else
                    {
                        var resultErrResponse = JsonConvert.DeserializeObject<CheckUrboxErrorResponse>(readData.Result);
                        string messErr = "Lỗi convert dữ liệu json";
                        if (resultErrResponse.Zero != null && resultErrResponse.Zero.data != null)
                        {
                            messErr = resultErrResponse.Zero.data.msg;
                        }
                        return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, messErr, null, null);
                    }
                }
                else
                {
                    var rspStr = new List<DataVoucherPartnerResponse>
                                {
                                    new DataVoucherPartnerResponse() { Msg = response.Item1.Content.ReadAsStringAsync().Result }
                                };
                    return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, response.Item1.StatusCode.ToString(), rspStr, null);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, ex.Message.ToString(), null, null);
            }
        }
        private Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>> ValidateStatusUrbox(List<CheckUrboxResponse> checkUrboxResponses, List<UrboxProducts> products = null, bool isApplySku = false)
        {
            List<DataVoucherPartnerResponse> codeErrors = new List<DataVoucherPartnerResponse>();
            List<DataVoucherPartnerResponse> codeDone = new List<DataVoucherPartnerResponse>();
            foreach (var item in checkUrboxResponses)
            {
                if (item.done == 0 || item.status != 101)
                {
                    codeErrors.Add(new DataVoucherPartnerResponse()
                    {
                        Code = item.data.code,
                        Amount = item.data.amount,
                        Msg = item.data.msg,
                        Remark = "",
                        Status = item.status.ToString(),
                        IsApplySku = isApplySku
                    });
                }
                else if (item.done == 1 && item.status == 101)
                {
                    codeDone.Add(new DataVoucherPartnerResponse()
                    {
                        Code = item.data.code,
                        Amount = item.data.amount,
                        Msg = item.data.msg,
                        Remark = products != null ? JsonConvert.SerializeObject(products) : "",
                        Status = item.status.ToString(),
                        IsApplySku = isApplySku
                    });
                }
            }

            if (codeErrors.Count > 0)
            {
                return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, codeErrors.FirstOrDefault().Msg ?? "", codeErrors, null);
            }
            else if (codeDone.Count == checkUrboxResponses.Count)
            {
                return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(true, codeDone.FirstOrDefault().Msg ?? "", codeDone, products);
            }
            else
            {
                codeErrors.Add(new DataVoucherPartnerResponse()
                {
                    Code = checkUrboxResponses.FirstOrDefault().data.code,
                    Amount = checkUrboxResponses.FirstOrDefault().data.amount,
                    Msg = checkUrboxResponses.FirstOrDefault().data.msg,
                    Remark = "",
                    Status = checkUrboxResponses.FirstOrDefault().status.ToString()
                });
                return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(true, codeErrors.FirstOrDefault().Msg ?? "", codeErrors, null);
            }
        }
        public async Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> PayCodelUrbox(UpdateStatusVoucherPartnerRequest request)
        {
            long responseTime = 0;
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == PartnerEnum.URBOX.ToString()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, MessConst.NotFounDataConfig, null,null);

            if (!CheckStoreUsedUrbox(_sysWebApiDto, request.StoreNo))
            {
                return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, string.Format("Cửa hàng {0} không sử dụng Urbox", request.StoreNo), null, null);
            }

            var sysWebApiRoute = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "PayCodeUrbox".ToUpper());
            if (sysWebApiRoute == null) return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, MessConst.NotFounDataConfig, null, null);

            string stringResponse = "";
            string dataBody = "";
            if (_sysWebApiDto.Version.ToUpper() == "V0")
            {
                bool isApplySku = false;
                List<UrboxProducts> skuUrbox = null;
                if (request.Items != null && request.Items.Count > 0)
                {
                    var dataCheckUrbox = new CheckVoucherPartnerPOSRequest()
                    {
                        Partner = request.Partner,
                        PosNo = request.PosNo,
                        StoreNo = request.StoreNo,
                        SerialNo = request.SerialNo.Select(x=>x.Code).ToList(),
                        Items = request.Items
                    };
                    var checkVoucher = await CheckSerialUrbox(dataCheckUrbox);
                    //if (!checkVoucher.Item1)
                    //{
                    //    return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, checkVoucher.Item2, null);
                    //}
                    if (checkVoucher.Item1)
                    {
                        isApplySku = true;
                        skuUrbox  = checkVoucher.Item4;
                    }
                }
                
                if (isApplySku && skuUrbox != null && skuUrbox.Count > 0)
                {
                    var resultProducts = skuUrbox
                        .GroupBy(p => p.product_code.Trim())
                        .Select(g => new UrboxProducts()
                        {
                            product_code = g.Key,
                            quantity = g.Sum(x => x.quantity),
                            total_price = g.Sum(x => x.total_price)
                        }).ToList();

                    var dataCheckUrbox = new PayCodeToUrboxRequestProducts()
                    {
                        amount = (int) request.SerialNo.Sum(x => x.Amount),
                        code = StringHelper.ConvertArrayToString(request.SerialNo.Select(x => x.Code).ToArray(), ','),
                        brand_id = ParseHelper.IntOrZero(_sysWebApiDto.Authorization),
                        store_id = request.StoreNo,
                        terminal_id = request.PosNo,
                        bill_id = request.OrderNo,
                        has_order_detail = 1,
                        products = resultProducts,
                        bill_at = DateTimeHelper.UnixTimestampString(),
                    };
                    dataBody = JsonConvert.SerializeObject(dataCheckUrbox);
                }
                else
                {
                    var dataCheckUrbox = new PayCodeToUrboxRequestV2()
                    {
                        amount = (int)request.SerialNo.Sum(x => x.Amount),
                        code = StringHelper.ConvertArrayToString(request.SerialNo.Select(x => x.Code).ToArray(), ','),
                        brand_id = ParseHelper.IntOrZero(_sysWebApiDto.Authorization),
                        store_id = request.StoreNo,
                        terminal_id = request.PosNo,
                        bill_id = request.OrderNo
                    };
                    dataBody = JsonConvert.SerializeObject(dataCheckUrbox);
                }
            }
            //product
            else if (_sysWebApiDto.Version.ToUpper() == "V3") 
            {
                if (request.Items == null || request.Items.Count == 0)
                {
                    return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, $"POS không gửi danh sách sản phẩm", null, null);
                }

                List<UrboxProducts> skuUrbox = new List<UrboxProducts>();
                foreach (var item in request.Items)
                {
                    skuUrbox.Add(new UrboxProducts()
                    {
                        product_code = item.Barcode,
                        quantity = (int)item.Qty,
                        total_price = (int)item.LineAmount
                    });
                }
               
                var resultProducts = skuUrbox
                        .GroupBy(p => p.product_code.Trim())
                        .Select(g => new UrboxProducts()
                        {
                            product_code = g.Key,
                            quantity = g.Sum(x => x.quantity),
                            total_price = g.Sum(x => x.total_price)
                        }).ToList();

                var dataCheckUrbox = new PayCodeToUrboxRequestProducts()
                {
                    amount = (int)request.SerialNo.Sum(x => x.Amount),
                    code = StringHelper.ConvertArrayToString(request.SerialNo.Select(x => x.Code).ToArray(), ','),
                    brand_id = int.Parse(_sysWebApiDto.Authorization),
                    store_id = request.StoreNo,
                    terminal_id = request.PosNo,
                    bill_id = request.OrderNo,
                    has_order_detail = 1,
                    products = resultProducts,
                    bill_at = DateTimeHelper.UnixTimestampString(),
                };
                dataBody = JsonConvert.SerializeObject(dataCheckUrbox);
            }
            else if(_sysWebApiDto.Version.ToUpper() == "V2")
            {
                var dataCheckUrbox = new PayCodeToUrboxRequestV2()
                {
                    amount = (int)request.SerialNo.Sum(x => x.Amount),
                    code = StringHelper.ConvertArrayToString(request.SerialNo.Select(x => x.Code).ToArray(), ','),
                    brand_id = int.Parse(_sysWebApiDto.Authorization),
                    store_id = request.StoreNo,
                    terminal_id = request.PosNo,
                    bill_id = request.OrderNo
                };
                dataBody = JsonConvert.SerializeObject(dataCheckUrbox);
            }
            try
            {
                _kibanaService.LogRequest(_sysWebApiDto.Host + sysWebApiRoute.Route, request.PosNo, "", request.OrderNo + "@" + JsonConvert.SerializeObject(dataBody));
                var response = await CallApiUrbox(_sysWebApiDto, request.PosNo, sysWebApiRoute.Route, dataBody);
                _kibanaService.LogResponse(_sysWebApiDto.Host + sysWebApiRoute.Route, request.PosNo, responseTime, "", request.OrderNo + "@" + JsonConvert.SerializeObject(response.Item1.Content.ReadAsStringAsync()));
                if (response.Item1 != null && response.Item1.StatusCode == HttpStatusCode.OK)
                {
                    var readData = response.Item1.Content.ReadAsStringAsync();
                    stringResponse = readData.Result;
                    var resultResponse = JsonConvert.DeserializeObject<List<CheckUrboxResponse>>(stringResponse);

                    if (resultResponse != null)
                    {
                        return ValidateStatusUrbox(resultResponse);
                    }
                    else
                    {
                        return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, "Lỗi convert dữ liệu json", null, null);
                    }
                }
                else
                {
                    return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, response.Item1.StatusCode.ToString(), null, null);
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(stringResponse))
                {
                    var checkErrorResponse = JsonConvert.DeserializeObject<PayErrorUrboxResponse>(stringResponse);
                    if (checkErrorResponse != null)
                    {
                        var resultResponse = checkErrorResponse.Original;
                        if (resultResponse != null)
                        {
                            return ValidateStatusUrbox(resultResponse);
                        }
                        else
                        {
                            return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, "Lỗi convert dữ liệu json", null, null);
                        }
                    }
                    _kibanaService.LogException(_sysWebApiDto.Host + sysWebApiRoute.Route, request.PosNo, responseTime, "", "Exception @" + stringResponse);
                    return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, "Lỗi convert dữ liệu json", null, null);
                }
                else
                {
                    _kibanaService.LogException(_sysWebApiDto.Host + sysWebApiRoute.Route, request.PosNo, responseTime, "", ex.Message.ToString());
                    return new Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>(false, "Lỗi dữ liệu trả về từ Urbox", null, null);
                }
            }
        }
        private bool CheckStoreUsedUrbox(SysWebApiDto sysWebApi, string storeNo)
        {
            if (sysWebApi != null && !string.IsNullOrEmpty(sysWebApi.Description))
            {
                var arrayStore = StringHelper.ConverStringToArray(sysWebApi.Description, ';');
                if (arrayStore.Length > 0 && arrayStore.Contains(storeNo))
                {
                    return false;
                }
            }
            return true;
        }
        private async Task<(HttpResponseMessage, long)> CallApiUrbox(SysWebApiDto sysWebApiDto, string posNo, string endPoint, string bodyJson)
        {
            long milliseconds = 0;
            var proxiedHttpClientHandler = new HttpClientHandler
            {
                UseProxy = !string.IsNullOrEmpty(sysWebApiDto.HttpProxy)
            };

            if (proxiedHttpClientHandler.UseProxy)
            {
                proxiedHttpClientHandler.Proxy = new WebProxy(sysWebApiDto.HttpProxy);
            }

            _kibanaService.LogRequest(sysWebApiDto.Host + endPoint, posNo, "", bodyJson);
            using (var client = new HttpClient(proxiedHttpClientHandler))
            {
                try
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    var signature = UrBox.CreateSignature(bodyJson, sysWebApiDto.PrivateKey);
                    //var signature = UrBox.SignData(bodyJson);
                    client.Timeout = TimeSpan.FromSeconds(45);
                    client.BaseAddress = new Uri(sysWebApiDto.Host);

                    client.DefaultRequestHeaders.Add("App-Id", sysWebApiDto.UserName);
                    client.DefaultRequestHeaders.Add("App-Secret", sysWebApiDto.Password);
                    client.DefaultRequestHeaders.Add("Signature", signature);

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                    var response = await client.PostAsync(endPoint, new StringContent(bodyJson, Encoding.UTF8, "application/json"));
                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();
                  
                    if(response.StatusCode != HttpStatusCode.OK)
                    {
                        var content = response.Content.ReadAsStringAsync();
                        _kibanaService.SendMessageSMS($"Request: {bodyJson}_<<>>_Response: {response.StatusCode}_{response.ReasonPhrase}_{content}", MessageTypeEnum.Warning.ToString(), $"WebApi: {sysWebApiDto.Host} lỗi {response.StatusCode}", Guid.NewGuid().ToString(), bodyJson, AppCodeMessageEnum.BLUEPOS_URBOX.ToString());
                    }
                    
                    return (response, milliseconds);
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    var timeoutResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                    {
                        Content = new StringContent("UrBox request timed out.")
                    };
                    return (timeoutResponse, 0);
                }
                catch (AggregateException ex)
                {
                    _kibanaService.LogException(sysWebApiDto.Host, posNo, milliseconds, endPoint, JsonConvert.SerializeObject(ex));
                    if (ex.InnerException != null)
                    {
                        FileHelper.WriteLogs("CallApiUrbox InnerException:" + ex.InnerException.Message.ToString());
                    }
                    return (new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), 0);
                }
            }
        }
        private async Task<(bool, List<CheckUrboxResponse>, List<UrboxProducts>, string, bool)> CheckUrboxWithProducts(SysWebApiDto _sysWebApiDto, SysWebApiRoute sysWebApiRoute, CheckVoucherPartnerPOSRequest request, List<CheckUrboxResponse> resultResponse, string requestID = "")
        {
            long responseTime = 0;
            List<UrboxProducts> products = new List<UrboxProducts>();
            foreach(var item in request.Items)
            {
                item.Barcode = item.Barcode.Trim();
            }
            if (resultResponse.FirstOrDefault().data.sku_apply != null
                            && resultResponse.FirstOrDefault().data.sku_apply.Count > 0
                            && request.Items != null && request.Items.Count > 0)
            {
                foreach (var item in resultResponse.FirstOrDefault().data.sku_apply)
                {
                    var itemCheck = request.Items.Where(x => x.Barcode == item).ToList();
                    if (itemCheck != null && itemCheck.Count > 0)
                    {
                        foreach (var icheck in itemCheck)
                        {
                            products.Add(new UrboxProducts()
                            {
                                product_code = item,
                                quantity = (int)icheck.Qty,
                                total_price = (int)icheck.LineAmount
                            });
                        }
                    }
                }
                if (products != null && products.Count > 0)
                {
                    var resultProducts = products
                        .GroupBy(p => p.product_code.Trim())
                        .Select(g => new UrboxProducts()
                        {
                            product_code = g.Key.Trim(),
                            quantity = g.Sum(x => x.quantity),
                            total_price = g.Sum(x => x.total_price)
                        }).ToList();

                    var dataSkuCheckUrbox = new CheckUrboxRequestV2_SKU()
                    {
                        amount = 1,
                        code = StringHelper.ConvertArrayToString(request.SerialNo.ToArray(), ','),
                        store_id = _sysWebApiDto.Authorization,
                        brand_id = int.Parse(_sysWebApiDto.Authorization),
                        terminal_id = request.PosNo,
                        has_order_detail = 1,
                        products = resultProducts,
                        bill_at = DateTimeHelper.UnixTimestampString(),
                        bill_id = requestID
                    };
                    var responseSku = await CallApiUrbox(_sysWebApiDto, request.PosNo, sysWebApiRoute.Route, JsonConvert.SerializeObject(dataSkuCheckUrbox));
                    var readData2 = responseSku.Item1.Content.ReadAsStringAsync();
                    _kibanaService.LogResponse(_sysWebApiDto.Host + sysWebApiRoute.Route, request.PosNo, responseTime, "", $"{JsonConvert.SerializeObject(request.SerialNo)}_{responseSku.Item1.StatusCode}_{readData2.Result}");
                    if (responseSku.Item1 != null && responseSku.Item1.StatusCode == HttpStatusCode.OK)
                    {
                        _kibanaService.LogResponse(_sysWebApiDto.Host + sysWebApiRoute.Route, request.PosNo, responseTime, "", JsonConvert.SerializeObject(request.SerialNo) + "@" + readData2.Result);
                        var resultResponseSku = ConvertHelper.ToObject<List<CheckUrboxResponse>>(readData2.Result);
                        if (resultResponseSku != null && resultResponseSku.Count > 0)
                        {
                            return (true, resultResponseSku, products, "OK", true);
                        }
                        else
                        {
                            var resultErrResponse = JsonConvert.DeserializeObject<CheckUrboxErrorResponse>(readData2.Result);
                            string messErr = "Lỗi convert dữ liệu json";
                            if (resultErrResponse.Zero != null && resultErrResponse.Zero.data != null)
                            {
                                messErr = resultErrResponse.Zero.data.msg;
                            }
                            return (false, null, null, "Lỗi convert dữ liệu json", true); 
                        }
                    }
                    else
                    {
                        var checkError = StringHelper.StringToObject<ResponseMessageUrbox>(readData2.Result);
                        _kibanaService.LogResponse(_sysWebApiDto.Host + sysWebApiRoute.Route, request.PosNo, responseTime, "", $"{JsonConvert.SerializeObject(request.SerialNo)}_{responseSku.Item1.StatusCode}_{JsonConvert.SerializeObject(checkError)}");
                    }
                }
                else
                {
                    return (false, null, null, $"Voucher không áp dụng cho sản phẩm trong giỏ hàng", true); 
                }
            }
            return (false, null, null, "BadRequest", false);
        }
    }
}