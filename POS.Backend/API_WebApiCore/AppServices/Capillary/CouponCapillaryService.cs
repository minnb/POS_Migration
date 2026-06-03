using Elasticsearch.Net;
using Microsoft.Ajax.Utilities;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TCX.API.Common.AppService;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Capillary.Coupons;
using TCX.API.Common.Dtos.Capillary.Redemption;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.Coupon;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Loyalty.CX;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;
using VCM.POSBLUE.Model.SAP;
using VCM.POSBLUE.Model.VINID;

namespace TCX.WebApiCore.AppServices.Coupon
{
    public class CouponCapillaryService
    {
        private readonly RedisCacheService _redisCacheService;
        private readonly KibanaService _kibanaService;
        private readonly CapillaryService _capillaryService;
        private readonly string _connectStringDb;
        public CouponCapillaryService()
        {
            _connectStringDb = ConfigurationManager.ConnectionStrings["DAPPER_CENTRAL_MD"].ConnectionString;
            _kibanaService = new KibanaService();
            _capillaryService = new CapillaryService();
            _redisCacheService = new RedisCacheService();
        }
        private string GetStatus(string isRedeemable)
        {
            if(string.IsNullOrEmpty(isRedeemable)) return "RDM";
            if(isRedeemable.ToUpper() == "TRUE") return "SOLD";
            return "RDM";
        }
        private string GetActicleType(CpnVchBOMHeaderDto cpnVchBOMHeader, string discountCode, string sms_template = "")
        {
            try
            {
                if (cpnVchBOMHeader != null)
                {
                    return cpnVchBOMHeader.ArticleType;
                }

                if (string.IsNullOrEmpty(sms_template)) return DataTypeHelper.GetValueDictionary(VoucherROPHelper.VoucherTypeCAP, "VOUCHER");
                               
                return DataTypeHelper.GetValueDictionary(VoucherROPHelper.VoucherTypeCAP, sms_template);
            }
            catch
            {
                return ArticleTypeEnum.ZVCN.ToString(); // "ZVCN";
            }
        }
        private string GetVoucherType(CpnVchBOMHeaderDto cpnVchBOMHeader, string sms_template = "")
        {
            try
            {
                if (cpnVchBOMHeader != null)
                {
                    if (cpnVchBOMHeader.ArticleType == ArticleTypeEnum.ZVCN.ToString() && sms_template == ArticleTypeEnum.POINTS_VOUCHER.ToString())
                    {
                        return sms_template;
                    }
                    return cpnVchBOMHeader.ArticleType;
                }

                if (string.IsNullOrEmpty(sms_template)) return DataTypeHelper.GetValueDictionary(VoucherROPHelper.VoucherTypeCAP, "VOUCHER");

                return DataTypeHelper.GetValueDictionary(VoucherROPHelper.VoucherTypeCAP, sms_template);
            }
            catch
            {
                return ArticleTypeEnum.ZVCN.ToString(); // "ZVCN";
            }
        }
        private ValidateCouponDataResponse MappingDataVoucherDynamicPricing(RedeemableData redeemableData)
        {
            try
            {
                List<ItemApplyCoupon> itemResult = new List<ItemApplyCoupon>
                {
                    new ItemApplyCoupon
                    {
                        ItemNo = "",
                        Barcode = redeemableData.Series_info.Description,
                        ExpiryDate = redeemableData.Series_info.Terms_and_condition,
                        SellDate = "",
                        ItemName = ""
                    }
                };
                return new ValidateCouponDataResponse
                {
                    Mobile = redeemableData.Mobile,
                    DiscountCode = redeemableData.Code,
                    DiscountType = redeemableData.Series_info.Discount_type,
                    DiscountValue = redeemableData.Series_info.Discount_value,
                    ValidTillDate = redeemableData.Series_info.Valid_till_date,
                    IsRedeemable = redeemableData.Is_redeemable.ToLower() == "true",
                    IsApplySku = !string.IsNullOrEmpty(redeemableData.Series_info.Description),
                    DiscountApply = 0,
                    Items = itemResult
                };
            }
            catch
            {
                return null;
            }
        }
        private Tuple<bool, string, object> CheckRequest(RedeemableData redeemableData, bool isDynamic)
        {
            if(redeemableData.Series_info.Tag.ToUpper() == "DYNAMIC" && isDynamic == false && redeemableData.Series_info.Sms_template == "COUPON")
            {
                return new Tuple<bool, string, object>(false, $"Vui lòng sử dụng Coupon ở chức năng Coupon WINX ", null);
            }

            return new Tuple<bool, string, object>(false, $"Voucher/coupon {redeemableData.Code} không hợp lệ", null);
        }
        public Tuple<bool, string, object> ValidateCoupon(LoyaltyRepository loyaltyRepository, RedisManager redisManager, MemoryCacheService _memoryCacheService, string storeNo, string posNo, string phoneNumber, string code, List<SkuApplyVoucherPartner> items, bool isDynamic = false)
        {
            long responseTime = 0;
            string response = string.Empty;
            ServerErrorResponses responseErr = new ServerErrorResponses();
            phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
            var sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == LoyaltyHelper.CheckSwithLoyaltyTheWINX(redisManager, loyaltyRepository, storeNo).ToUpper()).FirstOrDefault();
            if (sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "ValidateCoupon".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + storeNo, sysWebApiDto.Password);
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" },
                    { "x-winx-loyalty-client", "POS" }
                };
                
                string param = @"?mobile=" + phoneNumber + "&code=" + code + "&details=extended&format=json";
                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );
                _kibanaService.LogRequest(router.Route, posNo, "", $"{router.Route}/{param}");
                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                _kibanaService.LogResponse(router.Route, posNo, responseTime, "",$"{phoneNumber}|{code}|response:" + response);
                //FileHelper.WriteLogs(response);
                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = StringHelper.StringToObject<ValidateCouponResponse>(response);
                    if (dataResponse != null && dataResponse.Response.Status.Code == 200)
                    {
                        var dataRedeemable = dataResponse.Response.Coupons.Redeemable;
                        if (dataRedeemable != null && dataRedeemable.Series_info != null) 
                        {
                            if (!isDynamic && dataRedeemable.Series_info.Tag.ToUpper() != "DYNAMIC")
                            {
                                var fromDate = DateTimeHelper.StringToDateTime(dataRedeemable.Series_info.Created, "yyyy-MM-dd HH:mm:ss");
                                var toDate = DateTimeHelper.StringToDateTime(dataRedeemable.Series_info.Valid_till_date, "yyyy-MM-dd");
                                
                                //check cpnVchBOMHeader
                                var cpnVchBOMHeader = _redisCacheService.GetCpnVchBOMHeader(dataRedeemable.Series_info.Discount_code);
                                if (cpnVchBOMHeader == null && dataRedeemable.Series_info.Sms_template == "COUPON")
                                {
                                    return new Tuple<bool, string, object>(false, $"Coupon {code} có mã SAP code {dataRedeemable.Series_info.Discount_code} không hợp lệ", null);
                                }

                                var susscessData = new VoucherStatusResponse
                                {
                                    Status = GetStatus(dataRedeemable.Is_redeemable),
                                    Return = "0",
                                    ActicleNo = dataRedeemable.Series_info.Discount_code,
                                    ActicleType = GetActicleType(cpnVchBOMHeader, dataRedeemable.Series_info.Discount_code, dataRedeemable.Series_info.Sms_template),
                                    Value = dataRedeemable.Coupon_value,
                                    VoucherNumber = dataRedeemable.Code,
                                    Voucher_Currency = "VND",
                                    Validity_From_Date = fromDate.ToString("dd/MM/yyyy"),
                                    Expiry_Date = toDate.ToString("dd/MM/yyyy"),
                                    CompanyCode = CompanyEnum.WCM.ToString(),
                                    Partner = PartnerEnum.CAP.ToString(),
                                    IsEmployee = false,
                                    PhoneNumber = !string.IsNullOrEmpty(dataRedeemable.Mobile) ? FormatHelper.PhoneNumberVietNam(dataRedeemable.Mobile) : "",
                                    VoucherType = GetVoucherType(cpnVchBOMHeader, dataRedeemable.Series_info.Sms_template)
                                };

                                if (susscessData.Status == "SOLD")
                                {
                                    return new Tuple<bool, string, object>(true, "Success", susscessData);
                                }
                            }
                            else if(isDynamic)
                            {
                                if(string.IsNullOrEmpty(dataRedeemable.Series_info.Description))
                                {
                                    return new Tuple<bool, string, object>(false, $"Voucher/coupon {code} không áp dụng cho sản phẩm", null);
                                }

                                var checkItem = items?.FirstOrDefault(x => x.Barcode == dataRedeemable.Series_info.Description);
                                if (checkItem == null)
                                {
                                    return new Tuple<bool, string, object>(false, $"Đơn hàng không có phẩm được áp dụng Coupon", null);
                                }

                                var dataDynamic = MappingDataVoucherDynamicPricing(dataRedeemable);
                                if( dataDynamic != null)
                                {
                                    return new Tuple<bool, string, object>(true, "Success", dataDynamic);
                                }
                                else
                                {
                                    return new Tuple<bool, string, object>(false, $"Voucher/coupon {code} không hợp lệ", null);
                                }
                            }
                            else
                            {
                                return CheckRequest(dataRedeemable, isDynamic);
                            }
                            return new Tuple<bool, string, object>(false, $"Voucher/coupon {code} không hợp lệ", dataResponse);
                        }

                        return new Tuple<bool, string, object>(false, "Error", dataResponse);
                    }
                    else
                    {
                        var ResponseError = StringHelper.StringToObject<ValidateCouponResponseError>(response); 
                        if (ResponseError.Response != null && ResponseError.Response.Coupons != null && ResponseError.Response.Coupons.Redeemable != null)
                        {
                            var errorData = ResponseError.Response.Coupons.Redeemable;
                            if(errorData.Item_status != null)
                            {
                                return new Tuple<bool, string, object>(false, _capillaryService.GetMessageCapillary(_memoryCacheService.GetNotifyConfig(_connectStringDb), "Item_status", JsonConvert.SerializeObject(errorData.Item_status)) ?? JsonConvert.SerializeObject(errorData.Item_status), null);
                            }
                            return new Tuple<bool, string, object>(false, $"Voucher/coupon {code} không hợp lệ", errorData.Item_status);
                        }
                        return new Tuple<bool, string, object>(false, _capillaryService.GetMessageCapillary(_memoryCacheService.GetNotifyConfig(_connectStringDb), "Warnings", response) ?? response,  null);
                    }
                }
                else
                {
                    return new Tuple<bool, string, object>(false, @"Lỗi kết nối tới Capillary",  null);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("ValidateCoupon.Exception:" + JsonConvert.SerializeObject(ex));
                if (_capillaryService.CheckContainsResponse(response, "Unauthorized"))
                {
                    return new Tuple<bool, string, object>(false, $"(401) Unauthorized. Store {storeNo} chưa được tạo trên Capillary", ex.InnerException);
                }
                if (responseErr != null && responseErr.StatusCode > 0) return new Tuple<bool, string, object>(false, response, responseErr);
                return new Tuple<bool, string, object>(false, ExceptionHelper.GetExceptionMessage(ex), ex.InnerException);
            }
        }
        public async Task<Tuple<bool, string, object>> GetCouponDetails(MemoryCacheService _memoryCacheService, string storeNo, string couponCode)
        {
            try
            {
                var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
                if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);

                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetCouponDetails".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" }
                };

                string param = @"?code=" + couponCode + router.Notes;
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, storeNo, "", param);
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(_sysWebApiDto.UserName + "." + storeNo, _sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(_sysWebApiDto.Host, 
                                                                router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(_sysWebApiDto.Version),
                                                                _sysWebApiDto.HttpProxy,
                                                                _sysWebApiDto.Bypasslist
                                                                );

                var response = await apiCallHelper.HttpClientWithApiAsync();
                StringHelper.Loggingz(JsonConvert.SerializeObject(response.Item1));
                if (!string.IsNullOrEmpty(response.Item1))
                {
                    var dataResponse = StringHelper.StringToObject<CapillaryCouponDetailResponse>(response.Item1);
                    if (dataResponse != null && dataResponse.Response.Status.Code == 200)
                    {
                        var dataRedeemable = dataResponse.Response.Coupons.Coupon.FirstOrDefault();

                        return new Tuple<bool, string, object>(true,  "OK", dataRedeemable);
                    }
                    return new Tuple<bool, string, object>(false, response.Item1, response.Item3);
                }
                return new Tuple<bool, string, object>(false, response.Item1, response.Item3);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public async Task<Tuple<bool, string, object>> GetUserCouponsDetails(MemoryCacheService _memoryCacheService, string storeNo, string userId)
        {
            try
            {
                var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
                if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);

                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetUserCouponsDetails".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" }
                };

                string param = @"?mobile=" + userId + router.Notes;
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, storeNo, "", param);
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(_sysWebApiDto.UserName + "." + storeNo, _sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(_sysWebApiDto.Host,
                                                                router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(_sysWebApiDto.Version),
                                                                _sysWebApiDto.HttpProxy,
                                                                _sysWebApiDto.Bypasslist
                                                                );

                var response = await apiCallHelper.HttpClientWithApiAsync();
                StringHelper.Loggingz(JsonConvert.SerializeObject(response.Item1));
                return new Tuple<bool, string, object>(false, response.Item1, response.Item3);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public async Task<Tuple<bool, string, object>> ReActivateCoupons(MemoryCacheService _memoryCacheService, string storeNo, List<string> couponIds, string userName = "")
        {
            try
            {
                var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
                if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);

                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "ReActivateCoupons".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" }
                };
                
                var requestBody = new ReActivateCouponsRequest()
                {
                    RedemptionIds = couponIds
                };
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, storeNo, "", JsonConvert.SerializeObject(couponIds));
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(_sysWebApiDto.UserName + "." + storeNo, _sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(_sysWebApiDto.Host,
                                                                router.Route,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(requestBody),
                                                                NumberHelper.GetTimeOutApi(_sysWebApiDto.Version),
                                                                _sysWebApiDto.HttpProxy,
                                                                _sysWebApiDto.Bypasslist
                                                                );

                var response = await apiCallHelper.HttpClientWithApiAsync();
                StringHelper.Loggingz(JsonConvert.SerializeObject(response.Item1));
                if (!string.IsNullOrEmpty(response.Item1))
                {
                    var dataResponse = StringHelper.StringToObject<ReActivateCouponsResponse>(response.Item1);
                    if (dataResponse != null && dataResponse.Success)
                    {
                        _kibanaService.LogResponse("", storeNo, response.Item2, "", $"{userName} re-Active: {JsonConvert.SerializeObject(couponIds)} -> response: {JsonConvert.SerializeObject(dataResponse)}");
                        var dataLoyalty = new LoggingLoyaltyDto()
                        {
                            AppCode = AppCodeEnum.Capillary.ToString(),
                            ActionType = ActionTypeCapiEnum.ReActiveCoupon.ToString(),
                            OrderNo = couponIds.FirstOrDefault(),
                            OrigOrderNo = "",
                            MemberCardNo = userName,
                            LoyaltyPoints = 0,
                            Transaction = "",
                            TransactionType = 0,
                            Status = "Success",
                            Request = JsonConvert.SerializeObject(couponIds),
                            Response = JsonConvert.SerializeObject(dataResponse),
                            CrtDate = DateTime.Now,
                            Items = null
                        };
                        LoyaltyRepository loyaltyRepository = new LoyaltyRepository();
                        await loyaltyRepository.InsertLoggingLoyalty(dataLoyalty);
                        return new Tuple<bool, string, object>(true, "OK", null);
                    }
                    return new Tuple<bool, string, object>(false, 
                                                dataResponse.Errors.FirstOrDefault() != null ? dataResponse.Errors.FirstOrDefault().Code + " - " + dataResponse.Errors.FirstOrDefault().Message : "Error", 
                                                dataResponse);
                }
                else if(response.Item3 != null)
                {
                    StringHelper.Loggingz(JsonConvert.SerializeObject(response.Item3));

                }
                return new Tuple<bool, string, object>(false, response.Item1, response.Item3);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public async Task<Tuple<bool, string, object>> GetListCouponByUser(MemoryCacheService _memoryCacheService, string storeNo, string mobile, string status, string userName = "")
        {
            try
            {
                var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
                if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);

                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetCouponsByUser".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" }
                };

                string param = @"?mobile=" + mobile + $"&status={status.ToUpper()}" + router.Notes;
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, storeNo, "", param);
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(_sysWebApiDto.UserName + "." + storeNo, _sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(_sysWebApiDto.Host,
                                                                router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(_sysWebApiDto.Version),
                                                                _sysWebApiDto.HttpProxy,
                                                                _sysWebApiDto.Bypasslist
                                                                );

                var response = await apiCallHelper.HttpClientWithApiAsync();
                StringHelper.Loggingz(JsonConvert.SerializeObject(response.Item1));
                if (!string.IsNullOrEmpty(response.Item1))
                {
                    var dataResponse = StringHelper.StringToObject<CouponCapillaryByUserResponse>(response.Item1);
                    if (dataResponse != null 
                        && dataResponse.Entity != null 
                        && dataResponse.Entity.Customers != null 
                        && dataResponse.Entity.Customers.Count > 0)
                    {
                        return new Tuple<bool, string, object>(true, "OK", dataResponse.Entity.Customers);
                    }
                    return new Tuple<bool, string, object>(false, response.Item1, response.Item3);
                }
                else if (response.Item3 != null)
                {
                    StringHelper.Loggingz(JsonConvert.SerializeObject(response.Item3));
                }
                return new Tuple<bool, string, object>(false, response.Item1, response.Item3);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public async Task<Tuple<HttpStatusCode, string, object>> GetCouponDetailsCapillary(MemoryCacheService _memoryCacheService, string storeNo, string couponCode)
        {
            try
            {
                var getDetails = await GetCouponDetails(_memoryCacheService, storeNo, couponCode);
                if(!getDetails.Item1)
                {
                    return new Tuple<HttpStatusCode, string, object> (HttpStatusCode.BadRequest, "OK", getDetails.Item3);
                }
                return new Tuple<HttpStatusCode, string, object> (HttpStatusCode.OK, "OK", getDetails.Item3);
            }
            catch (Exception ex)
            {
                return new Tuple<HttpStatusCode, string, object> (HttpStatusCode.BadRequest, ex.Message, null);
            }
        }
        public async Task<Tuple<bool, string, object>> GetRedemptions(MemoryCacheService _memoryCacheService, string storeNo, string id)
        {
            try
            {
                var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
                if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);

                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetRedemptions".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" }
                };

                string param = @"?id=" + id + router.Notes;
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, storeNo, "", param);
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(_sysWebApiDto.UserName + "." + storeNo, _sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(_sysWebApiDto.Host,
                                                                router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(_sysWebApiDto.Version),
                                                                _sysWebApiDto.HttpProxy,
                                                                _sysWebApiDto.Bypasslist
                                                                );

                var response = await apiCallHelper.HttpClientWithApiAsync();
                StringHelper.Loggingz(JsonConvert.SerializeObject(response.Item1));
                if (!string.IsNullOrEmpty(response.Item1))
                {
                    return new Tuple<bool, string, object>(true, response.Item1, response.Item3);
                }
                return new Tuple<bool, string, object>(false, response.Item1, response.Item3);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
    }
}
