using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore;
using VCM.POSBLUE.API.Models;
using VCM.POSBLUE.Business.VINID;
using VCM.POSBLUE.Model.WinLife;

namespace VCM.POSBLUE.API.Services
{
    public class WinLifeService //: IWinLifeService
    {
        private CX_LogAPIModel modelLogCX { get; set; }
        readonly string CXUrl; //= WebConfigurationManager.AppSettings["CXUrl"];
        readonly string CXUser; //= WebConfigurationManager.AppSettings["CXUser"];
        readonly string CXPassword; //= WebConfigurationManager.AppSettings["CXPassword"];
        readonly string requestId;
        private readonly KibanaService _kibana;
        private VinIDBLO logginAPI ;//{ get; set; }
        public WinLifeService(string _CXUrl, string _CXUser, string _CXPassword)
        {
            logginAPI = new VinIDBLO();
            CXUrl = _CXUrl;
            CXUser = _CXUser;
            CXPassword = _CXPassword;
            requestId = _CXUrl;
            modelLogCX = new CX_LogAPIModel();
            _kibana = new KibanaService();
            logginAPI = new VinIDBLO();
        }
        public string CallApiCrowX(string posNo, string endPoint, string method, string payload, ref HttpStatusCode status, ref long milliseconds)
        {
            try
            {
                long timeUni = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string result = string.Empty;
                using (var client = new HttpClient())
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    _kibana.LogRequest(requestId, posNo, endPoint, payload);

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(CXUrl);

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", EncryptionHelper.Base64Encode($"{CXUser}:{CXPassword}"));

                    Task<string> messageResponse = null;
                    if (method.ToUpper() == "POST")
                    {
                        var responsePOST = client.PostAsync(endPoint, new StringContent(payload, Encoding.UTF8, "application/json")).Result;
                        messageResponse = responsePOST.Content.ReadAsStringAsync();
                        status = responsePOST.StatusCode;
                    }
                    else if (method.ToUpper() == "GET")
                    {
                        string path = endPoint + payload;
                        var responseGET = client.GetAsync(path).Result;
                        messageResponse = responseGET.Content.ReadAsStringAsync();
                        status = responseGET.StatusCode;
                    }

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    _kibana.LogResponse(requestId, posNo, milliseconds, endPoint, messageResponse.Result);
                    return messageResponse.Result;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Error(String.Format("{0} CallApiCrowX  Exception {1}\t\n{2}", requestId, endPoint, ex.Message.ToString()));
                return null;
            }
        }
        public ResultResponse RegisterTempWinMember(WinLife_Register_POS_Request modelPOS)
        {
            var endPoint = $"{UrlCrownX.CXWinLife_Register_Temp}";
            long timeUni = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var modelCX = new WinLife_Register_CX_Request
            {
                fullName = modelPOS.fullName,
                phoneNo = modelPOS.phoneNo,
                gender = modelPOS.gender,
                storeNo = modelPOS.storeCode,
                staffEntityId = "WCM",
                merchantId = "VCM",
                refStaffId = modelPOS.userPOS,
                otp = modelPOS.otp,
                source = "POS",
                dob = modelPOS.dob,
                identifierCardid = modelPOS.identifierCardid,       //Bo Sung Ngay 26/09/2022
                issuedDate = modelPOS.issuedDate,                   //yyyy-MM-dd //Bo Sung Ngay 26/09/2022
                issuedPlace = modelPOS.issuedPlace,                  //Bo Sung Ngay 26/09/2022
                title = modelPOS.title                              //Bổ sung ngày 07/03/2023                    

            };

            var requestPOS = JsonConvert.SerializeObject(modelPOS);
            var requestCX = JsonConvert.SerializeObject(modelCX);

            using (var client = new HttpClient())
            {
                try
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;
                    Serilog.Log.Logger.Warning(String.Format("{0} Request {1}\t\n{2}", requestId, endPoint, requestCX));


                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(CXUrl);

                    var byteArray = Encoding.ASCII.GetBytes($"{CXUser}:{CXPassword}");
                    var authenBasic = Convert.ToBase64String(byteArray);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                    var response = client.PostAsync(endPoint, new StringContent(requestCX, Encoding.UTF8, "application/json")).Result;

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    Serilog.Log.Logger.Warning(String.Format("{0} Response {1}\t\n{2}", requestId, endPoint, JsonConvert.SerializeObject(response.Content)));

                    //modelLogCX.ResponseTotal = milliseconds;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {

                        var readData = response.Content.ReadAsStringAsync();

                        var resultData = JsonConvert.DeserializeObject<WinLife_Register_Response>(readData.Result);
                        var responeCX = JsonConvert.SerializeObject(resultData);
                        //modelLogCX.ResponseXML = responeCX;

                        //Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                        var data = resultData.data;
                        var result = new WinLife_Register_POS_Response
                        {
                            fullName = data.fullName,
                            phoneNo = data.phoneNo,
                            gender = data.gender,
                            csn = data.csn
                        };

                        return ResponseHelper.SusscessResponse(HttpStatusCode.OK, result, "Đăng ký thành công thành viên WinLife");
                    }
                    else
                    {
                        var readData = response.Content.ReadAsStringAsync().Result;

                        var resultData = JsonConvert.DeserializeObject<WinLife_Register_Response>(readData);
                        var responeCX = JsonConvert.SerializeObject(resultData);

                        //modelLogCX.ResponseXML = responeCX;
                        //Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                        if (resultData.code == "E011")
                        {
                            return ResponseHelper.ErrorMessageResponse(HttpStatusCode.Conflict, null, $"{resultData.message}");
                            //return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                            //{
                            //    Data = null,
                            //    Message = $"{resultData.message}",
                            //    Status = HttpStatusCode.Conflict
                            //});
                        }

                        return new ResultResponse
                        {
                            Data = null,
                            Message = $"{resultData.message}",
                            Status = response.StatusCode
                        };

                        //return Request.CreateResponse(response.StatusCode, result);
                    }
                }

                catch (Exception ex)
                {
                    //modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                    //Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                    return ResponseHelper.ExceptionMessageResponse(HttpStatusCode.InternalServerError, ex);

                    //return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                    //{
                    //    Data = null,
                    //    Message = $"Lỗi hệ thống API CX {ex.Message}",
                    //    Status = HttpStatusCode.InternalServerError
                    //});
                }
            }
        }
        public ResultResponse RegisterWinMember(WinLife_Register_POS_Request modelPOS)
        {
            HttpStatusCode statusCode = HttpStatusCode.OK;
            string endPoint;
            long responseTime = 0;
            //CHECK STATUS
            string actionType = "WinLife_Register";
            string merchantId = "VCM";

            if (modelPOS.status == WinLifeRegisterEnum.REGISTER.ToString() || string.IsNullOrEmpty(modelPOS.status))
            {
                endPoint = $"{UrlCrownX.CXWinLife_Register}";
            }
            else if (modelPOS.status == WinLifeRegisterEnum.TEMP.ToString())
            {
                endPoint = $"{UrlCrownX.CXWinLife_Register_Temp}";
                actionType = "WinLife_Register_Temp";
            }
            else
            {
                return ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadGateway, "Status đăng ký thành viên không đúng", null);
            }

            var modelCX = new WinLife_Register_CX_Request
            {
                fullName = modelPOS.fullName,
                phoneNo = modelPOS.phoneNo,
                gender = modelPOS.gender,
                storeNo = modelPOS.storeCode,
                staffEntityId = "WCM",
                merchantId = merchantId,
                refStaffId = modelPOS.userPOS,
                otp = modelPOS.otp,
                source = "POS",
                dob = modelPOS.dob,
                identifierCardid = modelPOS.identifierCardid,       //Bo Sung Ngay 26/09/2022
                issuedDate = modelPOS.issuedDate,                   //yyyy-MM-dd //Bo Sung Ngay 26/09/2022
                issuedPlace = modelPOS.issuedPlace,                  //Bo Sung Ngay 26/09/2022
                title = modelPOS.title                              //Bổ sung ngày 07/03/2023                    
            };
            var requestCX = JsonConvert.SerializeObject(modelCX);

            modelLogCX = new CX_LogAPIModel
            {
                CardNumber = modelPOS.phoneNo,
                StoreNo = modelPOS.storeCode,
                POSTerminal = modelPOS.posCode,
                ActionType = actionType,
                RequestPOS = JsonConvert.SerializeObject(modelPOS),
                RequestXML = requestCX,
                Source = "WinLife",
                DateTime = DateTime.Now
            };

            try
            {
                var resultResponse = CallApiCrowX(modelPOS.posCode, endPoint, MethodApiEnum.POST.ToString(), requestCX, ref statusCode, ref responseTime);
                modelLogCX.ResponseTotal = responseTime;

                if (statusCode == HttpStatusCode.OK)
                {
                    var resultData = JsonConvert.DeserializeObject<WinLife_Register_Response>(resultResponse);
                    var responeCX = JsonConvert.SerializeObject(resultData);

                    var data = resultData.data;
                    var result = new WinLife_Register_POS_Response
                    {
                        fullName = data.fullName,
                        phoneNo = data.phoneNo,
                        gender = data.gender,
                        csn = data.csn
                    };

                    modelLogCX.ResponseXML = responeCX;
                    //Task.Run(() => logginAPI.WriteLogAPI_CX(modelLogCX));
                    return ResponseHelper.SusscessResponse(HttpStatusCode.OK, result, "Đăng ký thành công thành viên WinLife");
                }
                else
                {
                    var resultData = JsonConvert.DeserializeObject<WinLife_Register_Response>(resultResponse);
                    var responeCX = JsonConvert.SerializeObject(resultData);

                    if (resultData.code == "E011")
                    {
                        return ResponseHelper.ErrorMessageResponse(HttpStatusCode.Conflict, $"{resultData.message}", null);
                    }
                    else if (string.IsNullOrEmpty(resultData.code) && resultData.errors != null && resultData.errors.Count > 0)
                    {
                        return ResponseHelper.ErrorMessageResponse(HttpStatusCode.Conflict, $"{resultData.errors.FirstOrDefault().field + ": " + resultData.errors.FirstOrDefault().message}", null);
                    }

                    modelLogCX.ResponseXML = responeCX;
                    //Task.Run(() => logginAPI.WriteLogAPI_CX(modelLogCX));

                    return new ResultResponse
                    {
                        Data = null,
                        Message = $"{resultData.message}",
                        Status = statusCode
                    };
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Error(String.Format("{0} RegisterWinMember Exception {1}\t\n{2}", requestId, endPoint, ex.Message.ToString()));
                modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                //Task.Run(() => logginAPI.WriteLogAPI_CX(modelLogCX));

                return ResponseHelper.ExceptionMessageResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        public ResultResponse UpdateMemberCX(WinLife_SmartPOS_Update_Customer_Req_POS modelPOS)
        {
            var endPoint = $"{UrlCrownX.CXWinLife_SmartPOS_UpdateCustomer}";
            var modelCX = new WinLife_SmartPOS_Update_Customer_Req_CX
            {
                fullName = modelPOS.fullName,
                phoneNo = modelPOS.phoneNo,
                gender = modelPOS.gender,
                title = modelPOS.title,
                dob = modelPOS.dob
            };
            using (var client = new HttpClient())
            {
                try
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;
                    Serilog.Log.Logger.Warning(String.Format("PosNo:{0} Request {1}\t\n{1}", modelPOS.posCode, endPoint, JsonConvert.SerializeObject(modelCX)));

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(CXUrl);

                    var byteArray = Encoding.ASCII.GetBytes($"{CXUser}:{CXPassword}");
                    var authenBasic = Convert.ToBase64String(byteArray);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                    var response = client.PutAsync(endPoint, new StringContent(JsonConvert.SerializeObject(modelCX), Encoding.UTF8, "application/json")).Result;

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();
                    modelLogCX.ResponseTotal = milliseconds;

                    var readData = response.Content.ReadAsStringAsync();
                    var resultData = JsonConvert.DeserializeObject<ResponseCrownX>(readData.Result);
                    var responeCX = JsonConvert.SerializeObject(resultData);
                    modelLogCX.ResponseXML = responeCX;
                    
                    Serilog.Log.Logger.Warning(String.Format("PosNo:{0} Response time: {1} milliseconds Path: {2}\t\n{3}", modelPOS.posCode, milliseconds.ToString(), endPoint, readData.Result));

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var data = resultData.data;
                        var message = "Cập nhật thành công";
                        return ResponseHelper.SusscessResponse(HttpStatusCode.OK, data, message);
                    }
                    else
                    {
                        return new ResultResponse
                        {
                            Data = null,
                            Message = $"{resultData.code}:{resultData.message}",
                            Status = response.StatusCode
                        };                       
                    }
                }

                catch (Exception ex)
                {
                    modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                    return new ResultResponse
                    {
                        Data = null,
                        Message = $"Lỗi hệ thống API CX {ex.Message}",
                        Status = HttpStatusCode.InternalServerError
                    };
                }
            }
        }
       
    }
}
