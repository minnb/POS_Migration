using Confluent.Kafka;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Const;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Loyalty.CX;
using TCX.API.Common.Dtos.Vouchers;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Shared;

namespace TCX.WebApiCore
{
    public class OneUService
    {
        private readonly string _merchant_code;
        private readonly string _usecase = "WCM";
        private readonly string _redisKeyToken = "BLUEPOS:OneUToken";
        private readonly KibanaService _kibanaService;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly RedisManager _redisManager;
        private readonly SysWebApiDto sysWebApiDto;
        public OneUService()
        {
            _kibanaService = new KibanaService();
            _memoryCacheService = new MemoryCacheService();
            _redisManager = new RedisManager();
            sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.ONEU.ToString()).FirstOrDefault();
            _merchant_code = sysWebApiDto.Description.Trim();
        }
        public async Task<(bool, string)> GetToken()
        {
            try
            {
                var token = await _redisManager.GetStringAsync(_redisKeyToken);
                if(!string.IsNullOrEmpty(token))
                {
                    return (true, token);
                }
                string function = "Token";
                if (sysWebApiDto == null) return (false, MessConst.NotFounDataConfig);
                var sysWebApiRoute = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == function.ToUpper());
                if (sysWebApiRoute == null) return (false, MessConst.NotFounDataConfig);

                var request = new TokenOneURequest
                {
                    Client_id = sysWebApiDto.UserName,
                    Client_secret = sysWebApiDto.Password,
                    Audience = sysWebApiRoute.Notes.Trim()
                };
                var response = await CallApiOneU(
                    null,
                    function,
                    "GetToken",
                    request
                    );

                if(response.Item3 == (int)HttpStatusCode.OK)
                {
                    var dataToken = StringHelper.StringToObject<TokenOneUResponse>(response.Item1);
                    if(!string.IsNullOrEmpty(dataToken?.Data?.Access_token))
                    {
                        _redisManager.StringSetString(_redisKeyToken, dataToken.Data.Access_token, (long)dataToken.Data.Expires_in - 60);
                        return (true, dataToken.Data.Access_token);
                    }
                }
                
                return (false,$"HttpStatus: {response.Item3}  response: {response.Item1}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public async Task<ResultResponse> Estimate(CheckVoucherPartnerPOSRequest request)
        {
            try
            {
                var tokenData = await GetToken();
                if (!tokenData.Item1)
                {
                    return ResponseHelper.SusscessResponse(
                        HttpStatusCode.BadRequest,
                        null,
                        $"Lỗi không lấy được tokent OneU",
                        tokenData.Item2);
                }

                decimal totalAmount = 0;
                if(request.Items != null && request.Items.Count > 0)
                {
                    totalAmount = request.Items.Sum(x => x.LineAmount);
                }
                List<VoucherDataItem> vouchers = new List<VoucherDataItem>();
                foreach (var item in request.SerialNo)
                {
                    vouchers.Add(new VoucherDataItem
                    {
                        Type = ArticleTypeEnum.VOUCHER.ToString(),
                        Serial = item,
                        Merchant_code = _merchant_code
                    });
                }
                var moneySourcesOneU = new List<MoneySourcesOneU>
                {
                    new MoneySourcesOneU
                    {
                        Payment_method_code = "ZVID"
                    }
                };
                var checkoutData = new CheckoutDataOneU
                {
                    Total_amount = totalAmount,
                    Id = int.Parse(DateTimeHelper.UnixTimestampString()),
                    Merchant = new MerchantOunU
                    {
                        Code = _merchant_code,
                        Name = "Wincommerce"
                    },
                    Store = new MerchantOunU
                    {
                        Code = request.StoreNo,
                        Name = ""
                    },
                    Money_sources = moneySourcesOneU,
                    Order_details = new OrderDetailsOneU
                    {
                    }
                };

                var estimateRequest =  new EstimateRequest
                {
                    Usecase = _usecase,
                    Apply_items = vouchers,
                    Checkout_data = checkoutData
                };

                var response = await CallApiOneU(tokenData.Item2,"Estimate",request.PosNo,estimateRequest);
                StringHelper.Loggingz($"Response estimate OneU: " + response.Item1);
                if (!string.IsNullOrEmpty(response.Item1))
                {
                    var estimateResponse = StringHelper.StringToObject<EstimateResponseOneU>(response.Item1);
                    if (estimateResponse.Meta != null)
                    {
                        if(estimateResponse.Meta.Code == 200 && response.Item3 == (int)HttpStatusCode.OK)
                        {
                            List<DataVoucherPartnerResponse> dataVouchers = new List<DataVoucherPartnerResponse>();
                            var item_detail = estimateResponse.Data.Item_details.FirstOrDefault();
                            var voucherAmount = (int)estimateResponse.Data.Total_discount;
                            if(item_detail != null && item_detail.Item_type == VoucherTypeOneUEnum.FIXED.ToString())
                            {
                                voucherAmount = (int)item_detail.Item_value;
                            }
                            if (estimateResponse.Data != null && estimateResponse.Data.Total_discount > 0)
                            {
                                dataVouchers.Add(new DataVoucherPartnerResponse()
                                {
                                    Status = "200",
                                    Code = estimateRequest.Apply_items.FirstOrDefault().Serial,
                                    VoucherAmount = 0,
                                    Amount = voucherAmount,
                                    Msg = estimateResponse.Meta.Message,
                                    Remark = "",
                                    IsApplySku = false
                                });
                                return ResponseHelper.SusscessResponse(HttpStatusCode.OK, dataVouchers, response.Item1, null);
                            }
                        }
                        string mess = NotifyConfigHelper.GetNotifyConfigByStatus(_memoryCacheService, PartnerEnum.ONEU.ToString(), estimateResponse.Meta.Code.ToString(), request.SerialNo[0]) ?? $"Code {estimateResponse.Meta.Code} - {estimateResponse.Meta.Message}";
                        return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, mess, JsonConvert.SerializeObject(estimateResponse));
                    }
                    return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, $"{response.Item3}_{response.Item1}", JsonConvert.SerializeObject(response));
                }
                return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, $"{response.Item3}_{response.Item1}", JsonConvert.SerializeObject(response));
            }
            catch (Exception ex)
            {
                return ResponseHelper.SusscessResponse(HttpStatusCode.ServiceUnavailable,null,ex.Message,JsonConvert.SerializeObject(ex));
            }
        }
        public async Task<ResultResponse> Redeem(UpdateStatusVoucherPartnerRequest request)
        {
            try
            {
                var tokenData = await GetToken();
                if (!tokenData.Item1)
                {
                    return ResponseHelper.SusscessResponse(
                        HttpStatusCode.BadRequest,
                        null,
                        $"Lỗi không lấy được tokent OneU",
                        tokenData.Item2);
                }

                decimal totalAmount = 0;
                if (request.Items != null && request.Items.Count > 0)
                {
                    totalAmount = request.Items.Sum(x => x.LineAmount);
                }
                List<VoucherDataItem> vouchers = new List<VoucherDataItem>();
                foreach (var item in request.SerialNo)
                {
                    vouchers.Add(new VoucherDataItem
                    {
                        Type = ArticleTypeEnum.VOUCHER.ToString(),
                        Serial = item.Code,
                        Merchant_code = _merchant_code
                    });
                }
                var checkoutData = new CheckoutDataOneU
                {
                    Total_amount = totalAmount,
                    Id = int.Parse(DateTimeHelper.UnixTimestampString()),
                    Merchant = new MerchantOunU
                    {
                        Code = _merchant_code,
                        Name = "Wincommerce"
                    },
                    Store = new MerchantOunU
                    {
                        Code = request.StoreNo,
                        Name = ""
                    },
                    Money_sources = new List<MoneySourcesOneU>
                    {
                        new MoneySourcesOneU
                        {
                            Payment_method_code = "ZVID"
                        }
                    },
                    Order_details = new OrderDetailsOneU
                    {
                        Code = request.OrderNo
                    }
                };

                var redeemRequest = new EstimateRequest
                {
                    Usecase = _usecase,
                    Apply_items = vouchers,
                    Invoice_no = request.OrderNo,
                    Checkout_data = checkoutData
                };

                var response = await CallApiOneU(tokenData.Item2, "Redeem", request.PosNo, redeemRequest);
                StringHelper.Loggingz($"Response redeem OneU: " + response.Item1);
                if (!string.IsNullOrEmpty(response.Item1))
                {
                    var estimateResponse = StringHelper.StringToObject<RedeemResponseOneU>(response.Item1);
                    if (estimateResponse.Meta != null)
                    {
                        if (estimateResponse.Meta.Code == 200 && response.Item3 == (int)HttpStatusCode.OK)
                        {
                            List<DataVoucherPartnerResponse> dataVouchers = new List<DataVoucherPartnerResponse>();
                            if (estimateResponse.Data != null && !string.IsNullOrEmpty(estimateResponse.Data.Transaction_id))
                            {
                                foreach (var item in redeemRequest.Apply_items)
                                {
                                    var dataVoucherRedeem = request.SerialNo.FirstOrDefault(x => x.Code == item.Serial);
                                    dataVouchers.Add(new DataVoucherPartnerResponse()
                                    {
                                        Status = "200",
                                        Code = item.Serial,
                                        VoucherAmount = (int)estimateResponse.Data.Total_discount,
                                        Amount = dataVoucherRedeem!= null ? (int) dataVoucherRedeem.Amount : 0,
                                        Msg = estimateResponse.Data.Transaction_id,
                                        Remark = "",
                                        IsApplySku = false
                                    });
                                }
                                return ResponseHelper.SusscessResponse(HttpStatusCode.OK, dataVouchers, response.Item1, null);
                            }
                        }
                        string mess = NotifyConfigHelper.GetNotifyConfigByStatus(_memoryCacheService, PartnerEnum.ONEU.ToString(), estimateResponse.Meta.Code.ToString(), request.SerialNo.FirstOrDefault().Code, "Redeem") ?? $"Code {estimateResponse.Meta.Code} - {estimateResponse.Meta.Message}";
                        return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, mess, JsonConvert.SerializeObject(estimateResponse));
                    }
                    return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, $"{response.Item3}_{response.Item1}", JsonConvert.SerializeObject(response));
                }
                return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, $"{response.Item3}_{response.Item1}", JsonConvert.SerializeObject(response));
            }
            catch (Exception ex)
            {
                return ResponseHelper.SusscessResponse(HttpStatusCode.ServiceUnavailable, null, ex.Message, JsonConvert.SerializeObject(ex));
            }
        }
        private async Task<(string, long, int)> CallApiOneU(string token, string function, string posNo, object bodyJson)
        {
            string subjectSMS = $"{AppGlobals.WebApiSystem} - sử dụng dịch vụ voucher OneU";
            string url = string.Empty;
            try
            {
                string RequestId = Guid.NewGuid().ToString();
                var sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.ONEU.ToString()).FirstOrDefault();
                url = sysWebApiDto?.Host;
                if (sysWebApiDto == null) return (MessConst.NotFounDataConfig, 0, (int)HttpStatusCode.BadRequest);
                var sysWebApiRoute = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == function.ToUpper());
                if (sysWebApiRoute == null) return (MessConst.NotFounDataConfig, 0, (int)HttpStatusCode.BadRequest);
                
                var bodyJsonStr = StringHelper.ObjectToStringLowercase(bodyJson);
                var timestamp = DateTimeHelper.UnixTimestampString(60);
                var rawData = $"{sysWebApiDto.Description.Trim()}:{sysWebApiDto.PublicKey.Trim()}:{timestamp}:{bodyJsonStr}";
                var signature = OneUleHelper.SignatureOneU(rawData, sysWebApiDto.PrivateKey);
                
                Dictionary<string, string> Headers = new Dictionary<string, string>
                {
                    { "X-Merchant-ID", sysWebApiDto.Description.Trim() },
                    { "X-Key-ID", sysWebApiDto.PublicKey },
                    { "X-Timestamp", timestamp },
                    { "X-Signature", signature },
                    { "X-Request-ID", RequestId }
                };
                StringHelper.Loggingz($"Request redeem:" + bodyJsonStr);
                ApiHttpClientHelper apiHttpClientHelper = new ApiHttpClientHelper(
                    sysWebApiDto.Host,
                    sysWebApiRoute.Route,
                    Headers,
                    TokenTypeEnum.Bearer.ToString(),
                    token,
                    MethodApiEnum.POST.ToString(),
                    bodyJsonStr,
                    NumberHelper.TryParseInt(sysWebApiDto.Version, 30),
                    sysWebApiDto.HttpProxy
                    );
                _kibanaService.LogRequest(sysWebApiDto.Host + sysWebApiRoute.Route, posNo, "", bodyJsonStr);
                var response = await apiHttpClientHelper.HttpClientWithApiAsync();
                StringHelper.Loggingz($"Response redeem:" + response.Item1);
                _kibanaService.LogResponse(sysWebApiDto.Host + sysWebApiRoute.Route, posNo, response.Item2, "", bodyJsonStr + "@response: " + response.Item1);
                
                if(HttpStatusConst.HttpSatusServerError.Contains(response.Item3))
                {
                    await Task.Run(() => HttpClientService.SendMessageSMS(
                                            $"{function} lỗi httpStatus: {response.Item3} kết nối tới WebApi OneU. MsgError: {response.Item1}", 
                                            MessageTypeEnum.Warning.ToString(), 
                                            subjectSMS, appCode: AppCodeMessageEnum.BLUEPOS_ONEU.ToString()));
                }

                return response;
            }
            catch (Exception ex)
            {
                _kibanaService.LogException(url, posNo, 0, "", JsonConvert.SerializeObject(ex));
                if (ex.InnerException != null)
                {
                    FileHelper.WriteExpLogs("CallApiOneU InnerException:", ex);
                }
                await Task.Run(() => HttpClientService.SendMessageSMS(
                                        $"{function} lỗi kết nối tới WebApi OneU. MsgError: {ex.Message}", 
                                        MessageTypeEnum.Warning.ToString(), 
                                        subjectSMS, appCode: AppCodeMessageEnum.BLUEPOS_ONEU.ToString()));
                return (ex.Message, 0, (int) HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}