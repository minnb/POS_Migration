using Newtonsoft.Json;
using System;
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
using TCX.API.Common.Const;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore;
using TCX.WebApiCore.AppServices;
using TCX.WebApiCore.DbContext;
using VCM.POSBLUE.Business.LogServiceBLO;
using VCM.POSBLUE.Model.LogService;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api")]
    public class OfferController : BaseController
    {
        readonly string promotionStaffUrl = WebConfigurationManager.AppSettings["PromotionStaffUrl"];
        readonly string promotionStaffUser = WebConfigurationManager.AppSettings["PromotionStaffUser"];
        readonly string promotionStaffPass = WebConfigurationManager.AppSettings["PromotionStaffPass"];
        private LogServiceBLO logService { get; set; }
        private LogServiceModel logServiceModel { get; set; }
        private readonly OfferEmployeeService _offerEmployeeService;
        private readonly RedisClusterManager _redisClusterManager;
        private readonly KibanaService _kibanaService;
        private readonly RedisManager _redisManager;
        private readonly MemberPointsService _topUpPointsStaffService;
        private readonly MemoryCacheService _memoryCacheService;
        public OfferController()
        {
            logService = new LogServiceBLO();
            _offerEmployeeService = new OfferEmployeeService();
            _redisClusterManager = new RedisClusterManager();
            _kibanaService = new KibanaService();
            _redisManager = new RedisManager();
            _topUpPointsStaffService = new MemberPointsService();
            _memoryCacheService = new MemoryCacheService();
        }

        [HttpPost]
        [Route("v2/offer/staff/points/topup/retry")]
        [ValidateModel]
        public HttpResponseMessage ProcessRetryTopUpPointsStaff(VinIDSalesRequest request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            try
            {
                request.CardNumber = FormatHelper.PhoneNumberWithCountryCode(request.CardNumber);
                RabbitMQProducer.ProducerRabbtMQCluster(_kibanaService, RabitMQConst.Queue_Wincare_TopUpPoints,
                                                          request.VirtualCard,
                                                          JsonConvert.SerializeObject(request));

                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = null,
                    Message = $"Retry orderNo {request.OrderNo} vs {request.CardNumber} success",
                    Status = HttpStatusCode.OK,
                    MessageTechnical = RabitMQConst.Queue_Wincare_TopUpPoints
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Message = ex.Message,
                    Status = HttpStatusCode.BadRequest,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }

        [HttpGet]
        [Route("v2/offer/staff/points/topup")]
        [ValidateModel]
        public async Task<HttpResponseMessage> ProcessTopUpPointsStaff([Required(ErrorMessage = "number is required")] string queueName,
                                                                        [Required(ErrorMessage = "number is required")] int number, int isRetry = 0)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            try
            {
                string running = "";
                if(isRetry == 1)
                {
                    await _topUpPointsStaffService.RetryTopUpPointsToCapillary(_memoryCacheService, _redisManager);
                    running = "RetryTopUpPointsToCapillary";
                }
                else
                {
                    running = queueName.ToLower();
                    if (queueName.ToLower() == "member")
                    {
                        await _topUpPointsStaffService.RabbitMQLoyaltyMemberPoints(_memoryCacheService, _redisManager, RabitMQConst.Queue_Loyalty_Member_Points, number);
                    }
                    else if( queueName.ToLower() == "topup")
                    {
                        await _topUpPointsStaffService.RabbitMQTopUpStaffPoints(_memoryCacheService, _redisManager, number, RabitMQConst.Queue_Wincare_TopUpPoints);
                    }
                    else if (queueName.ToLower() == "refund")
                    {
                        await _topUpPointsStaffService.RabbitMQLoyaltyMemberPoints(_memoryCacheService, _redisManager, RabitMQConst.Queue_Loyalty_Refund_Points, number);
                    }
                    else if (queueName.ToLower() == "logging")
                    {
                        await _topUpPointsStaffService.RabbitMQTopUpStaffPoints(_memoryCacheService, _redisManager, number, RabitMQConst.Queue_Loyalty_LoggingLoyalty);
                    }
                }              
                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Message = "Success",
                    Status = HttpStatusCode.OK,
                    Data = running
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Message = ex.Message,
                    Status = HttpStatusCode.BadRequest,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }

        [HttpGet]
        [Route("v2/offer/staff/check")]
        [ValidateModel]
        public HttpResponseMessage OfferStaffCheck([Required(ErrorMessage = "storeNo is required")] string storeNo, 
                                                    [Required(ErrorMessage = "posNo is required")] string posNo, 
                                                    [Required(ErrorMessage = "phoneNumber is required")]
                                                    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Invalid phone number format")] string phoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            try
            {
                var result = _offerEmployeeService.GetOfferStaffRemnDB(_redisClusterManager, posNo, phoneNumber, "");
                if(result.Item1 == HttpStatusCode.OK && result.Item2 != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Message = "Success",
                        Status = HttpStatusCode.OK,
                        Data = result.Item2
                    });
                }

                return Request.CreateResponse(result.Item1, new ResultResponse
                {
                    Message = result.Item3,
                    Status = result.Item1,
                    Data = null
                });
            }
            catch(Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new ResultResponse
                {
                    Message = ex.Message,
                    Status = HttpStatusCode.ExpectationFailed,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }

        [HttpPost]
        [Route("v2/offer/staff/apply")]
        [ValidateModel]
        public HttpResponseMessage OfferStaffTransaction(OfferStaffTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            try
            {
                if(request.TransactionType > 1 && string.IsNullOrEmpty(request.ReturnedOrderNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Message = $"Không tìm thấy đơn hàng gốc",
                        Status = HttpStatusCode.BadRequest,
                        Data = null
                    });
                }

                var result = _offerEmployeeService.OfferStaffTransaction(_redisClusterManager, request, _kibanaService);
                if (result.Item1 == HttpStatusCode.OK)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Message = "Success",
                        Status = HttpStatusCode.OK,
                        Data = null
                    });
                }

                return Request.CreateResponse(result.Item1, new ResultResponse
                {
                    Message = result.Item2,
                    Status = result.Item1,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new ResultResponse
                {
                    Message = ex.Message,
                    Status = HttpStatusCode.ExpectationFailed,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }

        //old
        [HttpGet]
        [Route("promotion/staff/force-check")]
        public HttpResponseMessage ForceCheck(string storeNo, string posID, string staffCode)
        {
            try
            {

                if (string.IsNullOrEmpty(staffCode))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"staffCode đang bị trống",
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
                var modelPOS = "{ \"storeNo\":" + storeNo + ",\"posID\":" + posID + ",\"staffCode\":" + staffCode + "}";
                var endPoint = $"{promotionStaffUrl}/staff-quota/{staffCode}/force-check";
                logServiceModel = new LogServiceModel
                {

                    StoreNo = storeNo,
                    PosTerminal = posID,
                    RequestPOS = $"{modelPOS}",
                    RequestPartner = $"{endPoint}",
                    ActionController = "ForceCheck",
                    MethodPartner = "GET",
                    EndPointPartner = $"{endPoint}",
                    ResponsePartner = "",
                    ResponsPOS = "",
                    TimeResponse = 0,
                    CreatedDate = DateTime.Now
                };

                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(promotionStaffUrl);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Add("UserName", promotionStaffUser);
                        client.DefaultRequestHeaders.Add("Password", promotionStaffPass);

                        var response = client.GetAsync(endPoint).Result;
                        var readData = response.Content.ReadAsStringAsync();

                        var resultData = readData.Result;
                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        logServiceModel.ResponsePartner = resultData;
                        logServiceModel.TimeResponse = (int)milliseconds;

                        var responsePartner = JsonConvert.DeserializeObject<PromotionStaffResponse>(resultData);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (responsePartner.Status == 1)
                            {
                                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(responsePartner.Data);
                                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.OK,
                                    Data = responsePartner.Data
                                });
                            }
                            else
                            {
                                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.BadRequest
                                });

                                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }
                        else
                        {
                            logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                            {
                                Message = "Có lỗi xãy ra khi kết nối API Partner",
                                Status = HttpStatusCode.BadRequest
                            });

                            Task.Run(() => logService.LogServiceInsert(logServiceModel));

                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Message = "Có lỗi xãy ra khi kết nối API Partner",
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logServiceModel.ResponsePartner = JsonConvert.SerializeObject(ex);
                        logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                        {
                            Message = "Lỗi hệ thống API Partner, Vui lòng liên hệ IT",
                            Status = HttpStatusCode.BadRequest
                        });
                        Task.Run(() => logService.LogServiceInsert(logServiceModel));


                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Message = $"Lỗi hệ thống API Partner, Vui lòng liên hệ IT",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logServiceModel.ResponsePartner = JsonConvert.SerializeObject(ex);
                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                {
                    Message = "Lỗi hệ thống API BLUE, vui lòng liên hệ IT",
                    Status = HttpStatusCode.BadRequest
                });

                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Lỗi hệ thống API BLUE, vui lòng liên hệ IT",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }

        }

        [HttpGet]
        [Route("promotion/staff/check")]
        public HttpResponseMessage Check(string storeNo, string posID, string quota_code)
        {
            try
            {

                if (string.IsNullOrEmpty(quota_code))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"quota_code đang bị trống",
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

                var staffCode = quota_code.Substring(0, 8);
                var modelPOS = "{ \"storeNo\":" + storeNo + ",\"posID\":" + posID + ",\"quota_code\":" + quota_code + "}";


                var endPoint = $"{promotionStaffUrl}/staff-quota/{staffCode}/check?code={quota_code}";
                logServiceModel = new LogServiceModel
                {
                    StoreNo = storeNo,
                    PosTerminal = posID,
                    RequestPOS = $"{modelPOS}",
                    RequestPartner = $"{endPoint}",
                    ActionController = "Check",
                    MethodPartner = "GET",
                    EndPointPartner = $"{endPoint}",
                    ResponsePartner = "",
                    ResponsPOS = "",
                    TimeResponse = 0,
                    CreatedDate = DateTime.Now
                };
                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(promotionStaffUrl);                      

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Add("UserName", promotionStaffUser);
                        client.DefaultRequestHeaders.Add("Password", promotionStaffPass);

                        var response = client.GetAsync(endPoint).Result;
                        var readData = response.Content.ReadAsStringAsync();

                        var resultData = readData.Result;
                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        logServiceModel.ResponsePartner = resultData;
                        logServiceModel.TimeResponse = (int)milliseconds;

                        var responsePartner = JsonConvert.DeserializeObject<PromotionStaffResponse>(resultData);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (responsePartner.Status == 1)
                            {
                                //var resultDataPartner = JsonConvert.DeserializeObject<ForceCheckResponse>(responsePartner.Data);
                                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(responsePartner.Data);
                                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.OK,
                                    Data = responsePartner.Data
                                });
                            }
                            else
                            {
                                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.BadRequest
                                });

                                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }
                        else
                        {
                            logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                            {
                                Message = "Có lỗi xãy ra khi kết nối API Partner",
                                Status = HttpStatusCode.BadRequest
                            });

                            Task.Run(() => logService.LogServiceInsert(logServiceModel));

                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Message = "Có lỗi xãy ra khi kết nối API Partner",
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logServiceModel.ResponsePartner = JsonConvert.SerializeObject(ex);
                        logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                        {
                            Message = "Lỗi hệ thống API Promotion Staff, Vui lòng liên hệ IT",
                            Status = HttpStatusCode.BadRequest
                        });
                        Task.Run(() => logService.LogServiceInsert(logServiceModel));


                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Message = $"Lỗi hệ thống API Promotion Staff, Vui lòng liên hệ IT",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logServiceModel.ResponsePartner = JsonConvert.SerializeObject(ex);
                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                {
                    Message = "Lỗi hệ thống API BLUE, vui lòng liên hệ IT",
                    Status = HttpStatusCode.BadRequest
                });

                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Lỗi hệ thống API BLUE, vui lòng liên hệ IT",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }

        }

        [HttpPost]
        [Route("promotion/staff/redeem")]
        public HttpResponseMessage Redeem(PromotionStaffRedeemPOSRequest model)
        {
            try
            {

                if (string.IsNullOrEmpty(model.StaffCode))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"staffCode đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.StoreNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"storeNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.PosID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"posID đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var modelPOS = JsonConvert.SerializeObject(model);

                var modelPartner = new PromotionStaffRedeemPartnerRequest
                {
                    StaffCode = model.StaffCode,
                    StaffName = model.StaffName,
                    RedeemDate=model.RedeemDate,
                    RefCode=model.OrderNo,
                    Amount=model.Amount
                };

                var JsonPartner = JsonConvert.SerializeObject(modelPartner);

                var endPoint = $"{promotionStaffUrl}/staff-quota/redeem";

                logServiceModel = new LogServiceModel
                {

                    StoreNo = model.StoreNo,
                    PosTerminal = model.PosID,
                    RequestPOS = $"{modelPOS}",
                    RequestPartner = $"{JsonPartner}",
                    ActionController = "Redeem",
                    MethodPartner = "PUT",
                    EndPointPartner = $"{endPoint}",
                    ResponsePartner = "",
                    ResponsPOS = "",
                    TimeResponse = 0,
                    CreatedDate = DateTime.Now
                };

                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(promotionStaffUrl);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Add("UserName", promotionStaffUser);
                        client.DefaultRequestHeaders.Add("Password", promotionStaffPass);

                        var response = client.PutAsync(endPoint, new StringContent(JsonPartner, Encoding.UTF8, "application/json")).Result;
                        var readData = response.Content.ReadAsStringAsync();

                        var resultData = readData.Result;
                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        logServiceModel.ResponsePartner = resultData;
                        logServiceModel.TimeResponse = (int)milliseconds;

                        var responsePartner = JsonConvert.DeserializeObject<PromotionStaffResponse>(resultData);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (responsePartner.Status == 1)
                            {
                                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.OK
                                });

                                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.OK                                 
                                });
                            }
                            else
                            {
                                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.BadRequest
                                });

                                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }
                        else
                        {
                            logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                            {
                                Message = "Có lỗi xãy ra khi kết nối API Partner",
                                Status = HttpStatusCode.BadRequest
                            });

                            Task.Run(() => logService.LogServiceInsert(logServiceModel));

                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Message = "Có lỗi xãy ra khi kết nối API Partner",
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logServiceModel.ResponsePartner = JsonConvert.SerializeObject(ex);
                        logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                        {
                            Message = "Lỗi hệ thống API Partner, Vui lòng liên hệ IT",
                            Status = HttpStatusCode.BadRequest
                        });
                        Task.Run(() => logService.LogServiceInsert(logServiceModel));


                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Message = $"Lỗi hệ thống API Partner, Vui lòng liên hệ IT",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logServiceModel.ResponsePartner = JsonConvert.SerializeObject(ex);
                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                {
                    Message = "Lỗi hệ thống API BLUE, vui lòng liên hệ IT",
                    Status = HttpStatusCode.BadRequest
                });

                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Lỗi hệ thống API BLUE, vui lòng liên hệ IT",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }

        }

        [HttpPost]
        [Route("promotion/staff/refund")]
        public HttpResponseMessage Refund(PromotionStaffRefundPOSRequest model)
        {
            try
            {

                if (string.IsNullOrEmpty(model.StaffCode))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"staffCode đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.StoreNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"storeNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.PosID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"posID đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var modelPOS = JsonConvert.SerializeObject(model);

                var modelPartner = new PromotionStaffRedeemPartnerRequest
                {
                    StaffCode = model.StaffCode,
                    StaffName = model.StaffName,
                    RedeemDate = model.RefundDate,
                    RefCode = model.OrderNo,
                    Amount = model.Amount
                };

                var JsonPartner = JsonConvert.SerializeObject(modelPartner);

                var endPoint = $"{promotionStaffUrl}/staff-quota/refund";

                logServiceModel = new LogServiceModel
                {

                    StoreNo = model.StoreNo,
                    PosTerminal = model.PosID,
                    RequestPOS = $"{modelPOS}",
                    RequestPartner = $"{JsonPartner}",
                    ActionController = "Refund",
                    MethodPartner = "POST",
                    EndPointPartner = $"{endPoint}",
                    ResponsePartner = "",
                    ResponsPOS = "",
                    TimeResponse = 0,
                    CreatedDate = DateTime.Now
                };

                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(promotionStaffUrl);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Add("UserName", promotionStaffUser);
                        client.DefaultRequestHeaders.Add("Password", promotionStaffPass);

                        var response = client.PostAsync(endPoint, new StringContent(JsonPartner, Encoding.UTF8, "application/json")).Result;
                        var readData = response.Content.ReadAsStringAsync();

                        var resultData = readData.Result;
                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        logServiceModel.ResponsePartner = resultData;
                        logServiceModel.TimeResponse = (int)milliseconds;

                        var responsePartner = JsonConvert.DeserializeObject<PromotionStaffResponse>(resultData);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (responsePartner.Status == 1)
                            {
                                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.OK
                                });

                                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.OK
                                });
                            }
                            else
                            {
                                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.BadRequest
                                });

                                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Message = responsePartner.Message,
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }
                        else
                        {
                            logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                            {
                                Message = "Có lỗi xãy ra khi kết nối API Partner",
                                Status = HttpStatusCode.BadRequest
                            });

                            Task.Run(() => logService.LogServiceInsert(logServiceModel));

                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Message = "Có lỗi xãy ra khi kết nối API Partner",
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logServiceModel.ResponsePartner = JsonConvert.SerializeObject(ex);
                        logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                        {
                            Message = "Lỗi hệ thống API Partner, Vui lòng liên hệ IT",
                            Status = HttpStatusCode.BadRequest
                        });
                        Task.Run(() => logService.LogServiceInsert(logServiceModel));


                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Message = $"Lỗi hệ thống API Partner, Vui lòng liên hệ IT",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logServiceModel.ResponsePartner = JsonConvert.SerializeObject(ex);
                logServiceModel.ResponsPOS = JsonConvert.SerializeObject(new ResultResponse
                {
                    Message = "Lỗi hệ thống API BLUE, vui lòng liên hệ IT",
                    Status = HttpStatusCode.BadRequest
                });

                Task.Run(() => logService.LogServiceInsert(logServiceModel));

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Message = $"Lỗi hệ thống API BLUE, vui lòng liên hệ IT",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }

        }
    }
}
