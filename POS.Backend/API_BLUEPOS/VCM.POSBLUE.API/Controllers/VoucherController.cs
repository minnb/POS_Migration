using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;
using TCX.API.Common.Models;
using VCM.POSBLUE.API.Models;
using VCM.POSBLUE.Business.VINID;
using VCM.POSBLUE.Model.CXVoucher;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api/vc")]
    public class VoucherController : ApiController
    {
        readonly string CXUrl = WebConfigurationManager.AppSettings["CXUrl"];
        readonly string CXUser = WebConfigurationManager.AppSettings["CXUser"];
        readonly string CXPassword = WebConfigurationManager.AppSettings["CXPassword"];
        private CX_LogAPIModel modelLogCX { get; set; }
        private VinIDBLO logVID { get; set; }
        public VoucherController()
        {
            modelLogCX = new CX_LogAPIModel();
            logVID = new VinIDBLO();
        }

        [HttpGet]
        [Route("generateOTP")]
        public HttpResponseMessage GenerateOTP(string storeNo, string posID, string codeValue)
        {
            try
            {

                if (string.IsNullOrEmpty(codeValue))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"codeValue đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"storeNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"posID đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var endPoint = $"{UrlCrownX.CXUrlGenerateOTP}?codeValue={codeValue}";

                modelLogCX = new CX_LogAPIModel
                {
                    CardNumber = codeValue,
                    StoreNo = storeNo,
                    POSTerminal = posID,
                    ActionType = "GenerateOTP",
                    RequestPOS = $"{endPoint}",
                    RequestXML = $"{CXUrl}{endPoint}",
                    Source = "CX",
                    DateTime = DateTime.Now
                };

                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(CXUrl);

                        var byteArray = Encoding.ASCII.GetBytes($"{CXUser}:{CXPassword}");
                        var authenBasic = Convert.ToBase64String(byteArray);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                        var response = client.GetAsync(endPoint).Result;
                        var readData = response.Content.ReadAsStringAsync();
                        var resultData = readData.Result;
                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();
                        var modelResponseOTP = JsonConvert.DeserializeObject<GenerateOTPResponse>(resultData);

                        modelLogCX.ResponseXML = JsonConvert.SerializeObject(modelResponseOTP);
                        modelLogCX.ResponseTotal = milliseconds;
                        Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            // var dataResult = JsonConvert.SerializeObject(resultData.data);


                            if (modelResponseOTP.errorCode == "E000")
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Message = modelResponseOTP.errorMessage,
                                    Status = HttpStatusCode.OK
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
                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Message = resultData.ToString(),
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                        Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Message = $"Lỗi hệ thống API CrownX, Vui lòng liên hệ IT",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Lỗi hệ thống API Blue, vui lòng liên hệ IT",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }

        }

        [HttpGet]
        [Route("getVoucherSerial")]
        public HttpResponseMessage GetVoucherSerial(string storeNo, string posID, string codeValue, string otp)
        {
            try
            {
                if (string.IsNullOrEmpty(codeValue))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"codeValue đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(otp))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"otp đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"storeNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"posID đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var endPoint = $"{UrlCrownX.CXUrlGetVoucher}?codeValue={codeValue}&otp={otp}";
                modelLogCX = new CX_LogAPIModel
                {
                    CardNumber = codeValue,
                    StoreNo = storeNo,
                    POSTerminal = posID,
                    ActionType = "GetVoucherSerial",
                    RequestPOS = $"{endPoint}",
                    RequestXML = $"{CXUrl}{endPoint}",
                    Source = "CX",
                    DateTime = DateTime.Now
                };

                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(CXUrl);

                        var byteArray = Encoding.ASCII.GetBytes($"{CXUser}:{CXPassword}");
                        var authenBasic = Convert.ToBase64String(byteArray);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                        var response = client.GetAsync(endPoint).Result;
                        var readData = response.Content.ReadAsStringAsync();
                        var resultData = readData.Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();
                        var responseDetail = JsonConvert.DeserializeObject<GetVoucherSerialResponse>(resultData);
                        modelLogCX.ResponseXML = JsonConvert.SerializeObject(responseDetail);
                        modelLogCX.ResponseTotal = milliseconds;
                        Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (responseDetail.errorCode == "E000")
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Data = responseDetail.data,
                                    Message = responseDetail.errorMessage,
                                    Status = HttpStatusCode.OK
                                });
                            }
                            else
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Message = $"{responseDetail.errorCode}:{responseDetail.errorMessage}",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }
                        else
                        {
                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Message = resultData.ToString(),
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                        Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Message = $"Lỗi hệ thống API CrownX, Vui lòng liên hệ IT",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Lỗi hệ thống API Blue, Vui lòng liên hệ IT",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }

        }

        [HttpPost]
        [Route("UpdateVoucherStatus")] //Cập nhật trạng thái Voucher
        public HttpResponseMessage UpdateVoucherStatus(UpdateVoucherStatusPosRequest req)
        {
            try
            {
                if (string.IsNullOrEmpty(req.StoreNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"StoreNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(req.PosID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"PosID đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var listSerial = req.ListSerial;

                if (listSerial.Any(d => string.IsNullOrEmpty(d.VoucherSerial)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"VoucherSerial đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var endPoint = $"{UrlCrownX.CXUrlUpdateVoucherStatus}";

                var modelCX = new UpdateVoucherStatusCXRequest
                {
                    data = new VoucherSerialDataCXRequest
                    {
                        voucherDetails = listSerial.Select(a => new VoucherSerialCXRequest
                        {
                            voucherSerial = a.VoucherSerial,
                            voucherStatus = "USED"

                        }).ToList()
                    }
                };
                var jsonCX = JsonConvert.SerializeObject(modelCX);
                modelLogCX = new CX_LogAPIModel
                {
                    CardNumber = "",
                    StoreNo = req.StoreNo,
                    POSTerminal = req.PosID,
                    ActionType = "UpdateVoucherStatus",
                    RequestPOS = JsonConvert.SerializeObject(req),
                    RequestXML = jsonCX,
                    Source = "CX",
                    DateTime = DateTime.Now
                };

                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(CXUrl);

                        var byteArray = Encoding.ASCII.GetBytes($"{CXUser}:{CXPassword}");
                        var authenBasic = Convert.ToBase64String(byteArray);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                        var response = client.PostAsync(endPoint, new StringContent(jsonCX, Encoding.UTF8, "application/json")).Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        modelLogCX.ResponseTotal = milliseconds;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var readData = response.Content.ReadAsStringAsync();
                            var resultData = readData.Result;

                            var responseDetail = JsonConvert.DeserializeObject<UpdateVoucherStatusResponse>(resultData);
                            modelLogCX.ResponseXML = JsonConvert.SerializeObject(responseDetail);

                            Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                            if (responseDetail.errorCode == "E000")
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Message = responseDetail.errorMessage,
                                    Status = HttpStatusCode.OK
                                });
                            }
                            else
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Message = $"{responseDetail.errorCode}:{responseDetail.errorMessage}",
                                    Status = HttpStatusCode.BadRequest,
                                    Data = responseDetail.data
                                });
                            }
                        }
                        else
                        {
                            modelLogCX.ResponseXML = JsonConvert.SerializeObject(response);

                            Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Message = "Gọi API CX thất bại, vui lòng liên hệ IT hỗ trợ",
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                        Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Message = $"Lỗi hệ thống API CrownX, Vui lòng liên hệ IT",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Lỗi hệ thống API Blue, Vui lòng liên hệ IT",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }

        }
    }
}
