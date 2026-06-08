using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Results;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Loyalty.CX;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore.AppServices.FMV.Dtos;
using TCX.WebApiCore.DbContext;

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
                    var data = StringHelper.StringToObject<MemberProfile>(response.Item2);
                    if (data != null)
                    {
                        return ResponseHelper.ResponseData(HttpStatusCode.OK, "OK", AkaChainHelper.MappingInfoMember(data), "AkaChainLoyalty");
                    }

                    return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, "OK", response);
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
        public async Task<ResultResponse> InputDataAsync(MemoryCacheService _memoryCacheService, VinIDSalesRequest model)
        {
            try
            {
                var sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.FMV.ToString()).FirstOrDefault();
                var bodyJson = AkaChainHelper.MappingInputDataRequest(model, sysWebApiDto.Description);
                var response = await AkaChainHelper.CallApiAsync(_memoryCacheService, "InputDataAsync", MethodApiEnum.POST,JsonConvert.SerializeObject(bodyJson), null);
                if (response.Item1 == System.Net.HttpStatusCode.OK)
                {
                    return ResponseHelper.ResponseData(HttpStatusCode.OK, "OK", response.Item2, "AkaChainLoyalty");
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
    }
}