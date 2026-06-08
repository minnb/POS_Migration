using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;
using TCX.API.Common.Const;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos.Capillary;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Loyalty.MemberBusiness;
using TCX.API.Common.Dtos.Ops;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;
using TCX.WebApiCore.AppServices.FMV.Dtos;
using TCX.WebApiCore.Shared;
using VCM.POSBLUE.Model.GiftBox;
using VCM.POSBLUE.Model.VINID;

namespace TCX.WebApiCore.AppServices.FMV
{
    public static class AkaChainHelper
    {
        public static MemberInputDataAsyncRequest MappingInputDataRequest(VinIDSalesRequest model, string activityCode)
        {
            List<CartItem> cartItems = new List<CartItem>();
            foreach (var item in model.TransLine)
            {
                cartItems.Add(new CartItem
                {
                    ProductCode = item.ItemCode,
                    ProductName = item.Description,
                    ProductCategory = item.Size,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalAmount = item.LineAmountIncVAT
                });
            }
            return new MemberInputDataAsyncRequest
            {
                MemberKeys = new MemberKeys
                {
                    Phone = "+" + FormatHelper.PhoneNumberWithCountryCode(model.CardNumber)
                },
                UsePoint = 0,
                IsSimulation = false,
                SimulationOfferId = null,
                CustomEntityDataId = null,
                ActivityCode = activityCode,
                State = "Open",
                CouponCode = new List<object>(),
                ActivityData = new ActivityData
                {
                    BusinessTime = model.OrderTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    Description = $"Bán hàng ngày {model.OrderTime:yyyy-MM-ddTHH:mm:ss.fffffffZ}",
                    TotalCalcAmount = model.BillAmount,
                    OrderCode = model.OrderNo,
                    CartItems = cartItems,
                    StoreCode = model.MerchantId,
                    OriginalOrderCode = null
                },
                TouchPointCode = null,
                UseBasePromotionSchemes = false
            };
        }
        public static InfoMemberModel MappingInfoMember(MemberProfile memberProfile)
        {
            return new InfoMemberModel
            {
                CardNumber = FormatHelper.PhoneNumberVietNam(memberProfile.Phone),
                VirtualCard = memberProfile.ReferralCode,
                CMND = "",
                MemberName = memberProfile.FullName,
                Title = "",
                CardLevel = "",
                MemberCSN = FormatHelper.PhoneNumberVietNam(memberProfile.Phone),
                PhoneNumber = FormatHelper.PhoneNumberVietNam(memberProfile.Phone),
                OtherInfo = "",
                QRCode = "",
                Dob = "",
                DateOfBirth = "",
                BirthdayGiftInd = false,//Quà tặng sinh nhật
                MemberPoint = memberProfile.TotalPoint,
                TotalPoint = memberProfile.TotalPoint,
                RedemptionValue = memberProfile.TotalPoint,
                ExtraPoint = false,
                CurrentRate = 0,
                IsOfflineVinID = false,
                IsShowMessage = false,
                IsRedeem = true,
                Status = "Hoạt động",
                System = MemberCapillaryEnum.FMV.ToString(),//KM HV WIN
                ClubCode = MemberCapillaryEnum.FMV.ToString(),//KM HV WIN
                Email = "",
                Gender = "",
                Address = "",
                ExternalId = "",
                AvailablePromotion = null,
                MemberBusiness = null,
                OtherStatus = null,
                ExtendedFields = null,
                MemberType = MemberCapillaryEnum.FMV.ToString(),
                Source = null,
                PointsSummaries = null,
            };
        }
        public static async Task<(HttpStatusCode, string)> CallApiAsync(MemoryCacheService memoryCacheService, string function, MethodApiEnum method, string dataRaw, string param = null)
        {
            try
            {
                var sysWebApiDto = memoryCacheService.GetSysWebApi(memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.FMV.ToString()).FirstOrDefault();
                string url = sysWebApiDto?.Host;
                if (sysWebApiDto == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig);

                var sysWebApiRoute = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == function.ToUpper());
                if (sysWebApiRoute == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig);

                int timeout = NumberHelper.TryParseInt(sysWebApiDto.Version, 30);

                var tokenResponse = await GetAccessTokenAsync(memoryCacheService);
                
                if(tokenResponse.Item1 != HttpStatusCode.OK)
                {
                    return (HttpStatusCode.Unauthorized, MessConst.ApiNotLogin);
                }
                
                Dictionary<string, string> Headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {tokenResponse.Item2}" },
                    {"__merchant", sysWebApiDto.PublicKey},
                    {"__tenant", sysWebApiDto.PrivateKey },
                };

                var route = sysWebApiRoute.Route;
                if (method == MethodApiEnum.GET && !string.IsNullOrEmpty(param))
                {
                    route = sysWebApiRoute.Route + param;
                }

                StringHelper.Loggingz($"AkaChain.CallApiAsync request:" + dataRaw);
                ApiHttpClientHelper apiHttpClientHelper = new ApiHttpClientHelper(
                    sysWebApiDto.Host,
                    route,
                    Headers,
                    null,
                    null,
                    method.ToString(),
                    dataRaw,
                    timeout,
                    sysWebApiDto.HttpProxy
                    );

                var curl = apiHttpClientHelper.ToCurlCommand();
                StringHelper.Loggingz(curl);
                var response = await apiHttpClientHelper.HttpClientWithApiAsync();
                StringHelper.Loggingz($"AkaChain.CallApiAsync response:" + response.Item1);

                return ((HttpStatusCode)response.Item3, response.Item1);
            }
            catch (Exception ex)
            {
                return (HttpStatusCode.Conflict, ex.Message);
            }
        }
        private static async Task<(HttpStatusCode, string)> GetAccessTokenAsync(MemoryCacheService memoryCacheService)
        {
            try
            {
                var sysWebApiDto = memoryCacheService.GetSysWebApi(memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.FMV.ToString()).FirstOrDefault();
                string url = sysWebApiDto?.Host;
                if (sysWebApiDto == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig);

                var sysWebApiRoute = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetToken".ToUpper());
                if (sysWebApiRoute == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig);

                int timeout = NumberHelper.TryParseInt(sysWebApiDto.Version, 30);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Culture=c%3Dvi%7Cuic%3Dvi; __tenant={sysWebApiDto.UserName}");

                    var formData = new Dictionary<string, string>
                        {
                            { "grant_type", "client_credentials" },
                            { "client_id", sysWebApiDto.UserName },
                            { "client_secret", sysWebApiDto.Password }, 
                            { "scope", "CustomerJourneyService MasterDataService MemberService TransactionService" }
                        };

                    var content = new FormUrlEncodedContent(formData);
                    var response = await client.PostAsync(url + sysWebApiRoute.Route, content);
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseString))
                    {
                        var dataToken = StringHelper.StringToObject<AccessTokenData>(responseString);
                        return (response.StatusCode, dataToken.Access_token);
                    }
                    return (response.StatusCode, $"NotFound access_token");
                }
            }
            catch (Exception ex)
            {
                return (HttpStatusCode.Conflict, ex.Message);
            }
        }
    }
}