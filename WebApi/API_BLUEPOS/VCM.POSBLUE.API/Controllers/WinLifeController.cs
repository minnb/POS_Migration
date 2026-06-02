using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Services.Description;
using TCX.API.Common.Const;
using TCX.API.Common.Dtos.Capillary.Customer;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Loyalty.CX;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Models.WinLife;
using TCX.API.Common.Shared;
using TCX.WebApiCore;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;
using VCM.POSBLUE.API.Models;
using VCM.POSBLUE.API.Services;
using VCM.POSBLUE.Business.Common;
using VCM.POSBLUE.Business.VINID;
using VCM.POSBLUE.Model.VINID;
using VCM.POSBLUE.Model.WinLife;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api")]
    public class WinLifeController : BaseController
    {
        readonly string CXUrl = WebConfigurationManager.AppSettings["CXUrl"];
        readonly string CXUser = WebConfigurationManager.AppSettings["CXUser"];
        readonly string CXPassword = WebConfigurationManager.AppSettings["CXPassword"];  

        private CX_LogAPIModel modelLogCX { get; set; }
        private VinIDBLO logVID { get; set; }
        private readonly WinLifeService _winLifeService;
        private readonly LoyaltyCapillaryService _loyaltyCapillaryService;       
        private readonly MemoryCacheService _memoryCacheService;
        private readonly CXService _cXService;
        private readonly KibanaService _kibanaService;
        private readonly WincodeService _wincodeService;
        private readonly RedisCacheService _redisCacheService;
        private readonly WinpayService _winpayService;
        private readonly RedisManager _redisManager;
        private readonly LoyaltyRepository _loyaltyRepository;
        private readonly LoyaltyService _loyaltyService;
        public WinLifeController()
        {
            modelLogCX = new CX_LogAPIModel();
            logVID = new VinIDBLO();
            _winLifeService = new WinLifeService(CXUrl, CXUser, CXPassword);
            _loyaltyCapillaryService = new LoyaltyCapillaryService();
            _memoryCacheService = new MemoryCacheService();
            _cXService = new CXService();
            _kibanaService = new KibanaService();
            _wincodeService = new WincodeService();
            _redisCacheService = new RedisCacheService();
            _winpayService = new WinpayService();
            _redisManager = new RedisManager();
            _loyaltyRepository = new LoyaltyRepository();
            _loyaltyService = new LoyaltyService();
        }

        [HttpGet]
        [Route("v2/otp/generate")]
        public HttpResponseMessage GenerateOTPV2([Required] string posNo = "201801", [Required] string phoneNumber = "0948886666", string action = "WIN_MEMBER_REGISTER")
        {
            try
            {
                //check Action Otp
                if(!string.IsNullOrEmpty(action)) // && !lstAction.Contains(action))
                {
                    var checkActionOtp = StringHelper.ConverStringToArray(_memoryCacheService.GetPOSDataSetup("WWW_OTP_ACTION").Value, ';');
                    if(checkActionOtp.Length > 0)
                    {
                        if (!checkActionOtp.Contains(action))
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = string.Format("{0} action không đúng, vui lòng liên hệ IT", action),
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                    }
                }

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Số điện thoại không đúng",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                if (_cXService.IsCheckNoneOtp(phoneNumber))
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = null,
                        Message = "Số điện thoại không cần nhập OTP",
                        Status = HttpStatusCode.NoContent
                    });
                }

                //winpay
                string action_send_otp_winpay = string.Empty;
                if (!string.IsNullOrEmpty(action))
                {
                    Dictionary<string, string> otpWinpay = new Dictionary<string, string>
                        {
                            { ActionWinpayEnum.send_otp.ToString(), "WINPAY_REGISTER" },
                            { ActionWinpayEnum.send_otp2.ToString(), "WINPAY_UPDATE_FP" },
                            { ActionWinpayEnum.send_otp3.ToString(), "WINPAY_DEACTIVE" }
                        };
                     action_send_otp_winpay = otpWinpay.FirstOrDefault(x => x.Value == action.ToUpper()).Key;
                }
                if (!string.IsNullOrEmpty(action_send_otp_winpay))
                {
                    var checkOtp = _winpayService.SendOtpWinpay(posNo, phoneNumber, action_send_otp_winpay);
                    if (checkOtp.Item1)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = new OtpResponse()
                            {
                                DeveloperMessage= "",
                                Message = "OK",
                                Data = new CXResponseData()
                                {
                                    //MessageId = EncryptionHelper.Base64Encode(checkOtp.Item2),
                                    MessageId = checkOtp.Item2,
                                    Status = "PROCESSING"
                                }
                            },
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = checkOtp.Item2,
                            Status = HttpStatusCode.BadRequest
                        });

                    }
                }
                else //CX
                {
                    var result = _cXService.GenerateOTP(posNo, phoneNumber, action);
                    if (result.Item1)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = result.Item3,
                            Message = result.Item2,
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = result.Item2,
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("GenerateOTPV2", posNo, 0, "", JsonConvert.SerializeObject(ex));
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Lỗi kết nối tạo tin nhắn OTP, vui lòng liên hệ IT",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }
        }

        [HttpPost]
        [Route("v2/otp/verify")]
        public HttpResponseMessage VerifyOTPV2(POSVerifyOTPRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                var result = _cXService.VerifyOTP(request.PosNo, request.PhoneNumber, request.Otp, request.Action);
                if (result.Item1)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = result.Item3,
                        Message = result.Item2,
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = result.Item3,
                        Message = "Số Otp nhập vào không đúng, vui lòng thử lại",
                        Status = HttpStatusCode.Conflict
                    });
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("VerifyOTPV2", request.PosNo, 0, "", JsonConvert.SerializeObject(ex));
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Lỗi kết nối tạo tin nhắn OTP, vui lòng liên hệ IT",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }
        }

        [HttpGet]
        [Route("blue/winlife/generateOTP")]
        public HttpResponseMessage GenerateOTP([Required] string storeNo = "2018", [Required] string posID = "201801", [Required] string codeValue = "")
        {
            long responseTime = 0;
            try
            {
                if (string.IsNullOrEmpty(codeValue))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Số điện thoại không đúng",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                HttpStatusCode statusCode = HttpStatusCode.OK;
                var endPoint = $"{UrlCrownX.CXWinLife_OTPV2}?codeValue={codeValue}&type=1";

                var resultData = _winLifeService.CallApiCrowX(posID, endPoint, MethodApiEnum.GET.ToString(), null, ref statusCode, ref responseTime);
                 
                var modelResponseOTP = JsonConvert.DeserializeObject<WinLife_OTP_Response>(resultData);

                modelLogCX.ResponseXML = JsonConvert.SerializeObject(modelResponseOTP);
                modelLogCX.ResponseTotal = responseTime;

                Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                if (statusCode == HttpStatusCode.OK)
                {
                    if (modelResponseOTP.errorCode == "E000")
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Message = modelResponseOTP.errorMessage,
                            Status = HttpStatusCode.OK,
                            Data = null
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Message = $"{modelResponseOTP.errorCode}:{modelResponseOTP.errorMessage}",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
                else
                {
                    return Request.CreateResponse(statusCode, new ResultResponse
                    {
                        Message = resultData.ToString(),
                        Status = statusCode
                    });
                }
            }
            catch (Exception ex)
            {
                modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));
                
                Serilog.Log.Logger.Error(String.Format("GenerateOTP Exception {0}", ex.Message.ToString()));

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Lỗi hệ thống API BLUE, vui lòng liên hệ IT",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }
        }

        [HttpPost]
        [Route("blue/winlife/register")]//Đăng ký thành viên WinLine
        public HttpResponseMessage Register(WinLife_Register_POS_Request modelPOS)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var isE = ModelState.Values.FirstOrDefault();
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                                                    ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, ModelState.Values.FirstOrDefault().Errors[0].ErrorMessage.ToString(), null));
                }
                else
                {
                    var checkStore = _memoryCacheService.GetStores().FirstOrDefault(x => x.No == modelPOS.storeCode);
                    if (checkStore != null && checkStore.PostCode == ChannelCapEnum.WINCARE.ToString())
                    {
                        var checkOtp = _cXService.VerifyOTP(modelPOS.posCode, modelPOS.phoneNo, modelPOS.otp, CXEnum.WIN_MEMBER_REGISTER.ToString());

                        if (!checkOtp.Item1)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = checkOtp.Item3,
                                Message = checkOtp.Item2,
                                Status = HttpStatusCode.Conflict
                            });
                        }

                        var result = _loyaltyCapillaryService.CustomerRegistration(_loyaltyRepository, _redisManager, _memoryCacheService, modelPOS, ChannelCapEnum.POS.ToString());
                        if (result.Item1)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = result.Item3,
                                Message = result.Item2,
                                Status = HttpStatusCode.OK
                            });
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = result.Item3,
                                Message = result.Item2 ?? "BadRequest",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                    }
                    else
                    {
                        var resultRegister = _winLifeService.RegisterWinMember(modelPOS);
                        return Request.CreateResponse(resultRegister.Status, resultRegister);
                    }                   
                }
            }
            catch (Exception ex)
            {  
                Serilog.Log.Logger.Error(String.Format("Controller.Register Exception {0}}", ex.Message.ToString()));
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi dữ liệu server {ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("blue/winlife/update-promotions")]//Ghi nhận giao dịch áp dung CTKM đặc biệt
        public HttpResponseMessage UpdatePromotions(WinLife_UpdatePromotions_POS_Request modelPOS)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ExceptionModels();
                }
                if(string.IsNullOrEmpty(modelPOS.csn))
                {
                    modelPOS.csn = modelPOS.phoneNumber;
                }
                
                //switch CAP
                var dataResult = _wincodeService.InsertWinCodeCustomer(modelPOS);
                if (dataResult.Item1)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = dataResult.Item3,
                        Message = dataResult.Item2,
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = dataResult.Item3,
                        Message = dataResult.Item2,
                        Status = HttpStatusCode.BadRequest
                    });
                }

                /*
                var endPoint = $"{UrlCrownX.CXWinLife_UpdatePromotion}";
                long timeUni = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var modelCX = new WinLife_UpdatePromotions_CX_Request
                {
                    csn = modelPOS.csn,
                    action = modelPOS.action,
                    merchantId = "VCM",
                    storeNo = modelPOS.storeCode,
                    posNo = modelPOS.posCode,
                    appliedCodes = modelPOS.appliedCodes,
                    billNo = modelPOS.orderNo
                };

                var requestPOS = JsonConvert.SerializeObject(modelPOS);
                var requestCX = JsonConvert.SerializeObject(modelCX);

                modelLogCX = new CX_LogAPIModel
                {
                    CardNumber = modelPOS.csn,
                    StoreNo = modelPOS.storeCode,
                    POSTerminal = modelPOS.posCode,
                    ActionType = "WinLife_UpdatePromotions",
                    RequestPOS = requestPOS,
                    RequestXML = requestCX,
                    Source = "WinLife",
                    DateTime = DateTime.Now
                };
                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;
                        Serilog.Log.Logger.Warning(String.Format("PosNo:{0} Request {1}\t\n{1}", modelPOS.posCode, endPoint, requestCX));

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(CXUrl);

                        var byteArray = Encoding.ASCII.GetBytes($"{CXUser}:{CXPassword}");
                        var authenBasic = Convert.ToBase64String(byteArray);
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                        var response = client.PostAsync(endPoint, new StringContent(requestCX, Encoding.UTF8, "application/json")).Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();
                        modelLogCX.ResponseTotal = milliseconds;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {

                            var readData = response.Content.ReadAsStringAsync();
                            Serilog.Log.Logger.Warning(String.Format("PosNo:{0} Response time: {1} milliseconds Path: {2}\t\n{3}", modelPOS.posCode, milliseconds.ToString(), endPoint, readData));

                            var resultData = JsonConvert.DeserializeObject<WinLife_UpdatePromotions_CX_Response>(readData.Result);
                            var responeCX = JsonConvert.SerializeObject(resultData);
                            modelLogCX.ResponseXML = responeCX;

                            Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                            var message = resultData.message;
                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Message = message,
                                Status = HttpStatusCode.OK
                            });
                        }
                        else
                        {
                            //  var responeCX = JsonConvert.SerializeObject(response);
                            var readData = response.Content.ReadAsStringAsync();
                            Serilog.Log.Logger.Warning(String.Format("PosNo:{0} Response time: {1} milliseconds Path: {2}\t\n{3}", modelPOS.posCode, milliseconds.ToString(), endPoint, readData.Result));

                            var resultData = JsonConvert.DeserializeObject<WinLife_UpdatePromotions_CX_Response>(readData.Result);
                            var responeCX = JsonConvert.SerializeObject(resultData);

                            modelLogCX.ResponseXML = responeCX;


                            Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                            var result = new ResultResponse
                            {
                                Data = null,
                                Message = $"{resultData.message}",
                                Status = response.StatusCode
                            };
                            return Request.CreateResponse(response.StatusCode, result);
                        }
                    }

                    catch (Exception ex)
                    {
                        modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                        Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = $"Lỗi hệ thống API CX {ex.Message}",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
                */

            }
            catch (Exception ex)
            {
                modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi dữ liệu server {ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet]
        [Route("blue/winlife/winCode-histories")]//Ghi nhận giao dịch áp dung CTKM đặc biệt
        public HttpResponseMessage WinCodeHistories(string csn, string winCode)
        {
            var endPoint = $"{UrlCrownX.CXWinLife_WinCodeHistories}?csn={csn}&winCode={winCode}";
            try
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;
                        Serilog.Log.Logger.Warning(String.Format("PosNo:{0} Request {1}\t\n{1}", winCode, endPoint, csn + winCode));

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(CXUrl);

                        var byteArray = Encoding.ASCII.GetBytes($"{CXUser}:{CXPassword}");
                        var authenBasic = Convert.ToBase64String(byteArray);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                        var response = client.GetAsync(endPoint).Result;
                        var readData = response.Content.ReadAsStringAsync();
                        var resultData = JsonConvert.DeserializeObject<ResponseCrownX>(readData.Result);

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var dataResult = JsonConvert.SerializeObject(resultData.data);
                            var dataHistory = JsonConvert.DeserializeObject<WinLife_HistoryModel>(dataResult);

                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = dataHistory,
                                Message = "Thành công",
                                Status = HttpStatusCode.OK
                            });
                        }
                        else
                        {

                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Data = null,
                                Message = $"{resultData.message}",
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = $"Lỗi hệ thống API CrownX {JsonConvert.SerializeObject(ex)}",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }
        }

        [HttpGet]
        [Route("blue/winlife/smart-pos/customer-by-last-digits-phone")]
        public async Task<HttpResponseMessage> CustomerByLastDigitsPhoneAsync(string storeNo, string posID, string codeValue)
        {
            try
            {

                if (string.IsNullOrEmpty(codeValue) || string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(posID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"codeValue đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                if (NumberHelper.IsPhoneNumber(codeValue.Trim()))
                {
                    var getData = await _loyaltyService.GetInfoMemberCapillary(_redisManager, _memoryCacheService, codeValue.Trim(), storeNo, posID, MemberCapillaryEnum.PHONE.ToString(), "WCM", false);
                    if (getData.Status == HttpStatusCode.OK)
                    {
                        var dataResult = StringHelper.StringToObject<InfoMemberModel>(JsonConvert.SerializeObject(getData.Data));
                        if (dataResult != null)
                        {
                            List<Winlife_CustomerByLastDigitsPhone> dataRsp = new List<Winlife_CustomerByLastDigitsPhone>
                            {
                                new Winlife_CustomerByLastDigitsPhone()
                                {
                                    Title = dataResult.Title,
                                    PhoneNo = dataResult.PhoneNumber,
                                    Dob = dataResult.Dob,
                                    FullName = dataResult.MemberName,
                                }
                            };
                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = dataRsp,
                                Message = "OK",
                                Status = HttpStatusCode.OK,
                                MessageTechnical = "smart-pos"
                            });
                        }  
                            
                        return Request.CreateResponse(HttpStatusCode.NotFound, new ResultResponse
                        {
                            Data = null,
                            Message = $"OK",
                            Status = HttpStatusCode.NotFound,
                            MessageTechnical = "smart-pos"
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(getData.Status, new ResultResponse
                        {
                            Data = getData.Data,
                            Message = getData.Message,
                            Status = getData.Status,
                            MessageTechnical = getData.MessageTechnical
                        });
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Số điện thoại {codeValue} không hợp lệ",
                        Status = HttpStatusCode.BadRequest
                    });
                }
             }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Exception: {ex.Message}",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }

        }

        //Cập nhật thông tin khách hàng
        [HttpPost]
        [Route("blue/winlife/smart-pos/update-customer-info")]
        public HttpResponseMessage UpdateCustomerInfo(WinLife_SmartPOS_Update_Customer_Req_POS modelPOS)
        {
            try
            {
                var result = _loyaltyCapillaryService.CustomerUpdate(_memoryCacheService, modelPOS, ChannelCapEnum.POS.ToString());
                if (result.Item1)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = result.Item3,
                        Message = result.Item2,
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = result.Item3,
                        Message = result.Item2 ?? "BadRequest",
                        Status = HttpStatusCode.BadRequest
                    });
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Exception {ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
    }
}
