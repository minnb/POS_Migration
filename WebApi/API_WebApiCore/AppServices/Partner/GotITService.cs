using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Results;
using System.Web.UI.WebControls;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.GotIT;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using static Confluent.Kafka.ConfigPropertyNames;

namespace TCX.WebApiCore
{
    public class GotITService
    {
        private readonly KibanaService _kibanaService;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly string _connectStringDb;
        public GotITService()
        {
            _kibanaService = new KibanaService();
            _memoryCacheService = new MemoryCacheService();
            _connectStringDb = _memoryCacheService.GetConnectStringCentralMD();
        }
        private string GetPin(SysWebApiDto sysWebApiDto, string storeNo)
        {
            return sysWebApiDto.Authorization + storeNo;
        }
        private string GetSKU(string barcode)
        {
            if(_memoryCacheService.GetEnvironment() == "PRD")
            {
                return barcode;
            }
            return "SKU" + barcode;
        }
        public async Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> CheckMultiple(CheckVoucherPartnerPOSRequest request)
        {
            try
            {
                string function = "CheckMultiple";
                var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == PartnerEnum.GOTIT.ToString()).FirstOrDefault();
                if (_sysWebApiDto == null) return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, MessConst.NotFounDataConfig, null);

                if(_sysWebApiDto.Version == "V6")
                {
                    function = "CheckMultipleV6";
                }
                var sysWebApiRoute = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == function.ToUpper());
                
                if (sysWebApiRoute == null) return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, MessConst.NotFounDataConfig, null);

                List<Skus_info_gotit> skus_info = new List<Skus_info_gotit>();
                if (request.Items != null && request.Items.Count > 0)
                {
                    foreach (var item in request.Items)
                    {
                        skus_info.Add(new Skus_info_gotit()
                        {
                            Sku = GetSKU(item.Barcode),
                            Price = (int)item.UnitPrice,
                            Quantity =  item.Qty
                        });
                    }
                }

                var skus_info_groupby = skus_info
                        .GroupBy(p => p.Sku.Trim())
                        .Select(g => new Skus_info_gotit()
                        {
                            Sku = g.Key,
                            Price = g.First().Price,
                            Quantity = g.Sum(p => p.Quantity)
                        })
                        .ToList();

                var dataVoucher = new CheckMultipleDto()
                {
                    Pin = GetPin(_sysWebApiDto, request.StoreNo),
                    Codes = request.SerialNo,
                    Bill_number = request.PosNo + "_" + request.SerialNo[0],
                    Skip_reserved_when_mark_used = true,
                    Total_bill = skus_info_groupby.Sum(x=>x.Quantity*x.Price),
                    Skus_info = skus_info_groupby
                };
                
                var response = await CallApiGotIT(_sysWebApiDto, request.PosNo, sysWebApiRoute.Route, StringHelper.ObjectToStringLowercase(dataVoucher));
                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    var readData = response.Content.ReadAsStringAsync().Result;
                    //FileHelper.WriteLogs(readData);
                    var resultResponse = StringHelper.StringToObject<GotITResponse>(readData);
                    if(resultResponse == null || resultResponse.Data == null)
                    {
                        return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, "Mã code không hợp lệ hoặc không đúng.", null);
                    }
                    else if(!resultResponse.Success)
                    {
                        return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, resultResponse.Message_vi, null);
                    }
                    return ValidateStatusGotIt(resultResponse, _sysWebApiDto.Version);
                }
                else if(response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, string.Format("InternalServerError: {0}", _sysWebApiDto.Host), null);
                }
                else
                {
                    var rspStr = new List<DataVoucherPartnerResponse>
                    {
                        new DataVoucherPartnerResponse
                        {
                            Code = "",
                            Msg = response.Content.ReadAsStringAsync().Result
                        }
                    };
                    return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, response.StatusCode.ToString(), rspStr);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, ResponseHelper.ExceptionResponse(ex).Message, null);
            }
        }
        public async Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> MarkUseMultiple(UpdateStatusVoucherPartnerRequest request)
        {
            string function = "MarkUseMultiple";
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == PartnerEnum.GOTIT.ToString()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, MessConst.NotFounDataConfig, null);

            if (_sysWebApiDto.Version == "V6")
            {
                function = "MarkUseMultipleV6";
            }
            var sysWebApiRoute = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == function.ToUpper());
            if (sysWebApiRoute == null) return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, MessConst.NotFounDataConfig, null);

            string stringResponse = "";
            decimal totalAmount = 1100;
            List<Skus_info_gotit> skus_info = new List<Skus_info_gotit>();
            if (request.Items != null && request.Items.Count > 0)
            {
                totalAmount = request.Items.Sum(x => x.LineAmount);
                foreach (var item in request.Items)
                {
                    skus_info.Add(new Skus_info_gotit()
                    {
                        Sku = GetSKU(item.Barcode),
                        Price = item.UnitPrice,
                        Quantity = item.Qty
                    });
                }
            }

            var skus_info_groupby = skus_info
                .GroupBy(p => p.Sku.Trim())
                .Select(g => new Skus_info_gotit()
                {
                    Sku = g.Key,
                    Price = g.First().Price,
                    Quantity = g.Sum(p => p.Quantity)
                })
                .ToList();

            var dataMarkUseMultiple = new MarkUseMultipleDto()
            {
                Pin = GetPin(_sysWebApiDto, request.StoreNo),
                Codes = request.SerialNo.Select(x => x.Code).ToList(),
                Total_bill = totalAmount,
                Bill_number = request.OrderNo,
                Skip_reserved_when_mark_used = true,
                Skus_info = skus_info_groupby
            };

            try
            {
                var response = await CallApiGotIT(_sysWebApiDto, request.PosNo, sysWebApiRoute.Route, StringHelper.ObjectToStringLowercase(dataMarkUseMultiple));
                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    var readData = response.Content.ReadAsStringAsync();
                    stringResponse = readData.Result;
                    var resultResponse = JsonConvert.DeserializeObject<GotITResponse>(stringResponse);

                    if (resultResponse == null || resultResponse.Data == null)
                    {
                        return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, "Mã code không hợp lệ hoặc không đúng.", null);
                    }
                    else if (!resultResponse.Success)
                    {
                        return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, resultResponse.Message_vi, null);
                    }
                    return ValidateStatusGotIt(resultResponse, _sysWebApiDto.Version);
                }
                else
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var rspStr = new List<DataVoucherPartnerResponse>
                    {
                        new DataVoucherPartnerResponse
                        {
                            Code = "",
                            Msg = content
                        }
                    };

                    _kibanaService.SendMessageSMS($"{response.StatusCode}_{response.ReasonPhrase}_{content}", MessageTypeEnum.Warning.ToString(), "Lỗi GotIt trả về khi thanh toán", request.OrderNo, JsonConvert.SerializeObject(request), "BLUEPOS_GOTIT");
                    return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, response.StatusCode.ToString(), rspStr);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("MarkUseMultiple Exception", ex);
                return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, ResponseHelper.ExceptionResponse(ex).Message, null);
            }
        }
        private async Task<HttpResponseMessage> CallApiGotIT(SysWebApiDto sysWebApiDto, string posNo, string endPoint, string bodyJson)
        {
            try
            {
                var client = HttpClientProvider.GetClient(sysWebApiDto.HttpProxy, 35, sysWebApiDto.Host);
                var st1 = new Stopwatch();
                st1.Start();
                _kibanaService.LogRequest(sysWebApiDto.Host + endPoint, posNo, "", bodyJson);
                var response = await client.PostAsync(endPoint, new StringContent(bodyJson, Encoding.UTF8, "application/json"));
                st1.Stop();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _kibanaService.SendMessageSMS($"Request: {bodyJson}_:::_Response: {response.StatusCode}_{response.ReasonPhrase}_{content}", MessageTypeEnum.Warning.ToString(), $"WebApi {sysWebApiDto.Host} lỗi {response.StatusCode}_{response.ReasonPhrase}", Guid.NewGuid().ToString(), bodyJson, AppCodeMessageEnum.BLUEPOS_GOTIT.ToString());
                }
                _kibanaService.LogResponse(sysWebApiDto.Host + endPoint, posNo, st1.ElapsedMilliseconds, "", $"{bodyJson} response: httpsStatus: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
                return response;
            }
            catch (AggregateException ex)
            {
                _kibanaService.LogException(sysWebApiDto.Host, posNo, 0, endPoint, JsonConvert.SerializeObject(ex));
                if (ex.InnerException != null)
                {
                    FileHelper.WriteExpLogs("CallApiGotIT InnerException:", ex);
                }
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        private Tuple<bool, string, List<DataVoucherPartnerResponse>> ValidateStatusGotIt(GotITResponse responses, string version = "V4")
        {
            List<DataVoucherPartnerResponse> codeErrors = new List<DataVoucherPartnerResponse>();
            List<DataVoucherPartnerResponse> codeDone = new List<DataVoucherPartnerResponse>();
            
            if(responses.Data == null)
            {
                return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, $"{responses.Return_code}: {responses.Message_vi}" ?? "Mã code không hợp lệ hoặc không đúng.", null);
            }
            if (responses.Success)
            {
                if (version == "V6")
                {
                    var voucherCodeData = StringHelper.StringToObject<List<VoucherCodeDataV2>>(JsonConvert.SerializeObject(responses.Data));
                    if (voucherCodeData == null)
                    {
                        return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, $"{responses.Return_code}: {responses.Message_vi}" ?? "Mã code không hợp lệ hoặc không đúng.", null);
                    }
                    foreach (var item in voucherCodeData)
                    {
                        var voucherAmount = item.Value;
                        if(item.Redemptions != null && item.Redemptions.Redemption_value > 0)
                        {
                            voucherAmount = item.Redemptions.Redemption_value;
                        }
                        var dataVoucher = new DataVoucherPartnerResponse()
                        {
                            Code = item.Code,
                            Amount = voucherAmount,
                            Msg = responses.Message_vi,
                            Status = item.State != null ? item.State.ToString() : "00"
                        };
                        if (item.Voucher_type.ToUpper() == VoucherTypeGotIt.conditional.ToString().ToUpper())
                        {
                            dataVoucher.Remark = JsonConvert.SerializeObject(item.Conditions.Redeemable_skus);
                            dataVoucher.IsApplySku = true;
                        }
                        codeDone.Add(dataVoucher);
                    }
                }
                else
                {
                    var voucherCodeData = StringHelper.StringToObject<List<VoucherCodeData>>(JsonConvert.SerializeObject(responses.Data));
                    if (voucherCodeData == null)
                    {
                        return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, $"{responses.Return_code}: {responses.Message_vi}" ?? "Mã code không hợp lệ hoặc không đúng.", null);
                    }
                    foreach (var item in voucherCodeData)
                    {
                        var voucherAmount = 0;
                        if (item.Product.Value != null)
                        {
                            voucherAmount = (int)item.Product.Value;
                        }
                        codeDone.Add(new DataVoucherPartnerResponse()
                        {
                            Code = item.Code,
                            Amount = voucherAmount,
                            Msg = item.Product.Product_name_vi,
                            Remark = "",
                            Status = item.State != null ? item.State.ToString() : "00"
                        });
                    }
                }
                return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(true, responses.Message_vi ?? "OK", codeDone);
            }
            else
            {
                //foreach (var item in voucherCodeData)
                //{
                //    codeErrors.Add(new DataVoucherPartnerResponse()
                //    {
                //        Code = item.Code,
                //        Amount = 0,
                //        Msg = responses.Message_en,
                //        Remark = responses.Message_vi,
                //        Status = "00"
                //    });
                //}
                return new Tuple<bool, string, List<DataVoucherPartnerResponse>>(false, responses.Message_vi ?? "Voucher không hợp lệ", codeErrors);
            }
        }
    }
}