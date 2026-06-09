using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Results;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Loyalty.CX;
using TCX.API.Common.Dtos.Vouchers;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore.DbContext;
using VCM.POSBLUE.Model.SAP;

namespace TCX.WebApiCore.AppServices.FMV
{
    public class AkaChainLoyaltyService
    {
        public async Task<ResultResponse> GetMemberProfile(MemoryCacheService _memoryCacheService, string key, string value)
        {
            try
            {
                value = "+" + FormatHelper.PhoneNumberWithCountryCode(value);
                var param = $"?memberKey.key={Uri.EscapeDataString(key)}&memberKey.value={Uri.EscapeDataString(value)}";
                var response = await AkaChainHelper.CallApiAsync(_memoryCacheService, "GetMemberProfile", MethodApiEnum.GET, null, param);
                if (response.Item1 == System.Net.HttpStatusCode.OK)
                {
                    var data = StringHelper.StringToObject<MemberProfileAkaChain>(response.Item2);
                    if (data != null)
                    {
                        return ResponseHelper.ResponseData(HttpStatusCode.OK, "Success", AkaChainMapping.MappingInfoMember(data), "AkaChainLoyalty");
                    }

                    return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, $"JSON conversion error", response);
                }
                else
                {
                    return ResponseHelper.ResponseData(response.Item1, response.Item1.ToString(), response);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.ResponseData(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
            }
        }
        public async Task<ResultResponse> AddTransactionAsync(MemoryCacheService _memoryCacheService, VinIDSalesRequest model)
        {
            try
            {
                var sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.FMV.ToString()).FirstOrDefault();
                var bodyJson = AkaChainMapping.MappingInputDataRequest(model, sysWebApiDto.Description);
                var response = await AkaChainHelper.CallApiAsync(_memoryCacheService, "InputDataAsync", MethodApiEnum.POST,JsonConvert.SerializeObject(bodyJson), null);
                if (response.Item1 == System.Net.HttpStatusCode.OK)
                {
                    var data = StringHelper.StringToObject<AddTransactionAkaChainResponse>(response.Item2);
                    if (data != null) 
                    {
                        var dataResponse = AkaChainMapping.MappingAddTransaction(data, model);
                        return ResponseHelper.ResponseData(HttpStatusCode.OK, "Success", dataResponse, "AkaChainLoyalty");
                    }
                    return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, $"JSON conversion error", null, response.Item2);
                }
                else
                {
                    var responseError = StringHelper.StringToObject<AkaChainErrorResponse>(response.Item2);
                    return ResponseHelper.ResponseData(response.Item1, responseError!= null ? responseError.Error.Message : response.Item1.ToString(), responseError);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.ResponseData(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
            }
        }
        public async Task<ResultResponse> ReturnTransactionAsync(MemoryCacheService _memoryCacheService, VinIDRefundRequest model)
        {
            try
            {
                var sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.FMV.ToString()).FirstOrDefault();
                var bodyJson = AkaChainMapping.MappingReturnInputDataRequest(model, sysWebApiDto.Description);
                var response = await AkaChainHelper.CallApiAsync(_memoryCacheService, "InputReturnDataAsync", MethodApiEnum.POST, JsonConvert.SerializeObject(bodyJson), null);
                if (response.Item1 == System.Net.HttpStatusCode.OK)
                {
                    //var data = StringHelper.StringToObject<AddTransactionAkaChainResponse>(response.Item2);
                    //if (data != null)
                    //{
                    //    var dataResponse = AkaChainMapping.MappingAddTransaction(data);
                    //    return ResponseHelper.ResponseData(HttpStatusCode.OK, "Success", dataResponse, "AkaChainLoyalty");
                    //}
                    return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, $"JSON conversion error", null, response.Item2);
                }
                else
                {
                    var responseError = StringHelper.StringToObject<AkaChainErrorResponse>(response.Item2);
                    return ResponseHelper.ResponseData(response.Item1, responseError != null ? responseError.Error.Message : response.Item1.ToString(), responseError);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.ResponseData(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
            }
        }
        public async Task<ResultResponse> CheckCoupon(MemoryCacheService _memoryCacheService, CheckVoucherPartnerPOSRequest modelPOS)
        {
            try
            {
                var sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.FMV.ToString()).FirstOrDefault();
                var bodyJson = AkaChainMapping.MappingCheckCouponRequest(modelPOS, sysWebApiDto.Description);
                var response = await AkaChainHelper.CallApiAsync(_memoryCacheService, "InputDataAsync", MethodApiEnum.POST, JsonConvert.SerializeObject(bodyJson), null);
                if (response.Item1 == System.Net.HttpStatusCode.OK)
                {
                    var data = StringHelper.StringToObject<AddTransactionAkaChainResponse>(response.Item2);
                    if (data != null)
                    {
                        if(data.CouponCode != null && data.CouponCode.Any())
                        {
                            var checkCouponData = data.CouponCode.FirstOrDefault();
                            var susscessData = new VoucherStatusResponse
                            {
                                Status = checkCouponData.CanUse == true ? "SOLD" : "UNSOLD",
                                Return = "0",
                                ActicleNo = "",
                                ActicleType = "",
                                Value = data.TotalDiscount.ToString(),
                                VoucherNumber = checkCouponData.Code,
                                Voucher_Currency = "VND",
                                Validity_From_Date = "",
                                Expiry_Date = "",
                                CompanyCode = "",
                                Partner = PartnerEnum.FMV.ToString(),
                                IsEmployee = false,
                                PhoneNumber = modelPOS.PhoneNumber,
                                VoucherType = checkCouponData.Type
                            };

                            if (!checkCouponData.CanUse)
                            {
                                return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, $"Coupon {checkCouponData.Code} {checkCouponData.Reason}", susscessData, "AkaChainLoyalty.CheckCoupon");
                            }
                            else
                            {
                                return ResponseHelper.ResponseData(HttpStatusCode.OK, "Success", susscessData, "AkaChainLoyalty.CheckCoupon");
                            }                            
                        }

                        return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, "Coupon không hợp lệ", null, JsonConvert.SerializeObject(data));
                    }
                    return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, $"JSON conversion error", null, response.Item2);
                }
                else
                {
                    var responseError = StringHelper.StringToObject<AkaChainErrorResponse>(response.Item2);
                    return ResponseHelper.ResponseData(response.Item1, responseError != null ? responseError.Error.Message : response.Item1.ToString(), responseError);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.ResponseData(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
            }
        }
    }
}