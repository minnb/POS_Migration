using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Capillary.Customer;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using VCM.POSBLUE.Model.Dtos.ROP;
using VTCX.API.Common.Enums;

namespace TCX.WebApiCore.Shared
{
    public static class WincareAppHelper
    {
        private readonly static List<int> httpStatusErrors = new List<int>() { 500, 502, 503, 504, 408 };
        private readonly static string subjectSMS = "Call dịch vụ Wincare App";
        private static ResponseAppWincare RspDataWincareApp(string response, string url = "", string code = "")
        {
            var result = StringHelper.StringToObject<ResponseAppWincare>(response);
            if (result == null)
            {
                var respWarning = new ResponseAppWincare()
                {
                    ResponseCode = (int)EStatusResponse.ServiceUnavailable,
                    TechnicalMessage = new string[] { response },
                    Message = $"Lỗi kết nối tới api Wincare App, vui lòng liên hệ IT",
                    Data = null
                };
                Task.Run(() => HttpClientService.SendMessageSMS($"Lỗi kết nối tới WebApi app Wincare để check barcode: {code} link webApi: {url}", MessageTypeEnum.Warning.ToString(), subjectSMS, appCode: AppCodeMessageEnum.WINCARE_APP_ERROR.ToString()));
                return respWarning;
            }
            return result;
        }
        public static async Task<(bool, ResponseAppWincare, string)> CallApiWincareApp(SysWebApiDto sysWebApiDto, string method, string function, string bodyJson, string param, string code="")
        {
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == function.ToUpper() && x.Blocked == false);
                if (router == null) return (false, null, $"Chưa cấu hình WebApi {function}");
                var routeUrl = router.Route;
                if(!string.IsNullOrEmpty(param) && method.ToUpper() == "GET")
                {
                    routeUrl += param;
                }
                var api = new ApiHttpClientHelper(
                                sysWebApiDto.Host ,
                                routeUrl,
                                null,
                                "Basic",
                                EncryptionHelper.Base64Encode($"{sysWebApiDto.UserName}:{sysWebApiDto.Password}"),
                                method,
                                bodyJson,
                                NumberHelper.TryParseInt(sysWebApiDto.Version, 30),
                                sysWebApiDto.HttpProxy
                                );

                var response = await api.HttpClientWithApiAsync();
                StringHelper.Loggingz($"{router.Route + param} ===>response: {JsonConvert.SerializeObject(response)}");
                if (httpStatusErrors.Contains(response.Item3))
                {
                    await Task.Run(() => HttpClientService.SendMessageSMS($"Lỗi kết nối tới WebApi Wincare App: {function} link webApi: {sysWebApiDto.Host + router.Route + param} MsgError: {response.Item1}", MessageTypeEnum.Warning.ToString(), subjectSMS, appCode: AppCodeMessageEnum.WINCARE_APP_ERROR.ToString()));
                }
                if (!string.IsNullOrEmpty(response.Item1))
                {
                    return (true, RspDataWincareApp(response.Item1, sysWebApiDto.Host + router.Route + param, code), response.Item3.ToString());
                }
                else
                {
                    return (false, null, JsonConvert.SerializeObject(response));
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("GetCustomerDetail.Exception:" + JsonConvert.SerializeObject(ex));
                return (false, null, $"CallApiWincareApp.Exception:" + ex.Message);
            }
        }
    }
}
