using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos.Capillary;
using TCX.API.Common.Dtos.Capillary.Point;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Loyalty.ProgramPoints;
using TCX.API.Common.Dtos.Loyalty.WinScore;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore;
using TCX.WebApiCore.AppServices;
using TCX.WebApiCore.AppServices.Coupon;
using TCX.WebApiCore.AppServices.TCB;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;
using VCM.POSBLUE.API.Services;
using VCM.POSBLUE.Model.WinLife;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api")]
    public class CapillaryController : BaseController
    {       
        private readonly LoyaltyService _loyaltyService;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly RedisManager _redisManager;
        private readonly LoyaltyOfflineService _loyaltyOfflineService;
        private readonly KibanaService _kibana;
        private readonly CouponCapillaryService _couponCapillaryService;
        private readonly MemberPointsService _memberPointsService;
        private readonly Lazy<LoyaltyRepository> _loyaltyRepository;
        public CapillaryController()
        {
            _loyaltyService = new LoyaltyService();
            _memoryCacheService = new MemoryCacheService();
            _redisManager = new RedisManager();
            _loyaltyOfflineService = new LoyaltyOfflineService(_redisManager);
            _kibana = new KibanaService();
            _couponCapillaryService = new CouponCapillaryService();
            _memberPointsService = new MemberPointsService();
            _loyaltyRepository = new Lazy<LoyaltyRepository>(() => new LoyaltyRepository());
        }

        [HttpPost]
        [Route("v2/loyalty/transaction/points/refund")]
        public async Task<HttpResponseMessage> RetryLoyaltyMemberPoints(VinIDRefundRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                var checkOrderExists = _redisManager.HashGet<string>(RedisConst.GetRedisKeyLoyaltyMemberPoints(model.OrderNo), model.OrderNo);
                if (!string.IsNullOrEmpty(checkOrderExists))
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                    {
                        Data = null,
                        Message = $"OrderNo {model.OrderNo} exists",
                        Status = HttpStatusCode.Conflict
                    });
                }

                var result = await _memberPointsService.IsProcessingRefundPointsCapillary(_memoryCacheService, _redisManager, model);
                if (result)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = null,
                        Message = $"Success {model.OrderNo}",
                        Status = HttpStatusCode.OK
                    });
                }
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Failed",
                    Status = HttpStatusCode.BadRequest
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = ex.Message,
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("v2/loyalty/point/topup/revert")]
        public async Task<HttpResponseMessage> RevertTopUpPoints(RevertTopUpPointsPOSRequest pointTopupPOSRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ExceptionModels();
                }
                var result = await _loyaltyService.RevertTopUpPoints(_memoryCacheService, pointTopupPOSRequest);
                return Request.CreateResponse(result.Status, new ResultResponse
                {
                    Data = result.Data,
                    Message = result.Message,
                    Status = result.Status
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi dữ liệu server {ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }

        [HttpPost]
        [Route("v2/loyalty/point/topup")]
        public async Task<HttpResponseMessage> TopUpPointCapillary(PointTopupPOSRequest pointTopupPOSRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ExceptionModels();
                }
                var result = await _loyaltyService.ToUpPointCapillary(_memoryCacheService, _redisManager, _loyaltyRepository.Value, pointTopupPOSRequest);
                return Request.CreateResponse(result.Status, new ResultResponse
                {
                    Data = result.Data,
                    Message = result.Message,
                    Status = result.Status
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi dữ liệu server {ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }

        [HttpPost]
        [Route("v2/loyalty/transaction/points")]
        public async Task<HttpResponseMessage> LoyaltyMemberPoints(VinIDSalesRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                model.SpendPoints = 0;
                if (!model.IsRetry)
                {
                    var checkOrderExists = _redisManager.HashGet<string>(RedisConst.GetRedisKeyLoyaltyMemberPoints(model.OrderNo), model.OrderNo);
                    if (!string.IsNullOrEmpty(checkOrderExists))
                    {
                        return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                        {
                            Data = null,
                            Message = $"OrderNo {model.OrderNo} exists",
                            Status = HttpStatusCode.Conflict
                        });
                    }
                }

                if (model.IsRetry && model.OrderTime < DateTime.Now && await _memberPointsService.CheckLoggingLoyalty(model.OrderNo, ActionTypeCapiEnum.AddTransaction.ToString()))
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                    {
                        Data = null,
                        Message = $"OrderNo {model.OrderNo} exists",
                        Status = HttpStatusCode.Conflict
                    });
                }

                var result = await _memberPointsService.IsProcessingMemberPointsCapillary(_memoryCacheService, _redisManager, model);
                if (result)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = null,
                        Message = $"Success {model.OrderNo}",
                        Status = HttpStatusCode.OK
                    });
                }
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Failed",
                    Status = HttpStatusCode.BadRequest
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = ex.Message,
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("v2/loyalty/transaction/program-points")]
        public async Task<HttpResponseMessage> AddTransactionProgramPoints(ProgramPointsTransactionDto request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            var result = await _loyaltyService.AddTransactionProgramPointsAsync(_memoryCacheService, request);
            return Request.CreateResponse(result.Status, new ResultResponse
            {
                Data = result.Data,
                Message = result.Message,
                Status = result.Status
            });
        }

        [HttpPost]
        [Route("v2/loyalty/transaction/member-business")]
        public HttpResponseMessage AddTransactionMemberBusiness(MemberBusinessRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            request.CardNumber = FormatHelper.PhoneNumberVietNam(request.CardNumber);

            List<string> status = new List<string>() { "CREATE", "REFUND" };
            if (!status.Contains(request.Status.ToUpper()))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = string.Format("Status {0} không đúng trong giá trị {1}", request.Status, JsonConvert.SerializeObject(status)),
                    Status = HttpStatusCode.BadRequest
                });
            }

            var result = _loyaltyService.TransactionMemberBusiness(request);
            return Request.CreateResponse(result.Status, new ResultResponse
            {
                Data = result.Data,
                Message = result.Message,
                Status = result.Status
            });
        }

        [HttpPost]
        [Route("v2/loyalty/winscore/update")]
        public HttpResponseMessage UpdateWinscore(UpdateStatusWinscorePOS request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                var response = _loyaltyService.UpdateWinscore(request.PosNo, request.PhoneNumber, request.Status);
                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = null,
                    Message = response.Message,
                    Status = HttpStatusCode.OK
                });
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("Exception v2/loyalty/winscore/update", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Exception lỗi xử lý dữ liệu server",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
        
        [HttpGet]
        [Route("v2/loyalty/points/history")]
        public async Task<HttpResponseMessage> GetPointsHistory(string storeNo, string phoneNumber)
        {
            return Request.CreateResponse(HttpStatusCode.OK, await _loyaltyService.GetPointHistory(_memoryCacheService, storeNo, phoneNumber));
        }
       
        [HttpPost]
        [Route("v2/loyalty/capillary/mobile-enroll")]
        public HttpResponseMessage UpdateMobileEnroll(Update_pos_enroll request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                var response = _loyaltyService.UpdateMobileEnroll(_memoryCacheService, request.StoreNo, request.PosNo, request.PhoneNumber, CapillaryHelper.GetTA());
                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = null,
                    Message = response.Message,
                    Status = HttpStatusCode.OK
                });
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("Exception v2/loyalty/winscore/update", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Exception lỗi xử lý dữ liệu server",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet]
        [Route("v2/loyalty/capillary/transactions")]
        public HttpResponseMessage GetTransactionDetailCapillary(string orderNo, string type, string storeNo)
        {
            var result = _loyaltyService.GetTransactionDetailCapillary(_memoryCacheService, storeNo, storeNo + "99", orderNo, type);
            if (result != null && result.Item1)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = JObject.Parse(result.Item2),
                    Message = $"OK",
                    Status = HttpStatusCode.OK
                });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, new ResultResponse
                {
                    Data = result,
                    Message = $"NotFound",
                    Status = HttpStatusCode.NotFound
                });
            }
        }

        [HttpGet]
        [Route("v2/loyalty/capillary/check")]
        public async Task<HttpResponseMessage> IsOfflineCapillary(string status = "")
        {
            return Request.CreateResponse(HttpStatusCode.OK, await _loyaltyOfflineService.CheckIsOfflineCapillary());
        }

        [HttpGet]
        [Route("v2/loyalty/capillary/check/switch/winx")]
        public async Task<HttpResponseMessage> CheckSwitchCapillary()
        {
            var getCacheConfigLoyalty = _loyaltyRepository.Value.GetMemoryCacheConfig(_redisManager, LoyaltyProgramEnum.SwithLoyaltyTheWINX.ToString());

            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
            {
                Data = new
                {
                    Status = getCacheConfigLoyalty.Blocked == true ? "capillary" : "winx"
                },
                Message = getCacheConfigLoyalty.ListStore,
                Status = HttpStatusCode.OK
            });
        }

        [HttpPost]
        [Route("v2/loyalty/capillary/action")]
        public async Task<HttpResponseMessage> SwithIsOfflineCapillary([FromUri] string status, [FromUri] string userName)
        {
            if (string.IsNullOrEmpty(status) || string.IsNullOrEmpty(userName))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Trạng thái or UserName không được để trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            List<string> strings = new List<string>() { "capillary", "winx" };
            if (strings.Contains(status.ToLower()))
            {
                if(status.ToLower() == "capillary")
                {
                   if(await _loyaltyRepository.Value.UpdateMemoryCacheConfig(_redisManager, LoyaltyProgramEnum.SwithLoyaltyTheWINX.ToString(), true))
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = null,
                            Message = "Api Hội viên sẽ chuyển sang gọi trực tiếp capillary",
                            Status = HttpStatusCode.OK
                        });
                    }
                }
                else if (status.ToLower() == "winx")
                {
                    if (await _loyaltyRepository.Value.UpdateMemoryCacheConfig(_redisManager, LoyaltyProgramEnum.SwithLoyaltyTheWINX.ToString(), false))
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = null,
                            Message = "Api Hội viên sẽ chuyển sang gọi trực tiếp TheWINX",
                            Status = HttpStatusCode.OK
                        });
                    }
                }
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi update {status}",
                    Status = HttpStatusCode.BadRequest
                });
            }

            var result = await _loyaltyOfflineService.IsCapillaryAction(status);
            _kibana.LogResponse("SwithIsOfflineCapillary", userName, 0, "", $"{userName} switch {status} successfuly " + JsonConvert.SerializeObject(result));
            _kibana.SendMessageSMS(JsonConvert.SerializeObject(result), "Info", $"{userName} thực hiện chuyển {status} Capillary", appCode: "BLUEPOS_CAP_SWITCH");
            return Request.CreateResponse(result.Status, result);
        }

        [HttpGet]
        [Route("v2/loyalty/capillary/customer/redemptions")]
        public async Task<HttpResponseMessage> GetCustomerRedemptions(string storeNo, string id)
        {
            try
            {
                if (string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(id))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "StoreNo or Id không được để trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                var result = await _couponCapillaryService.GetRedemptions(_memoryCacheService, storeNo, id);
                string msgError = string.IsNullOrEmpty(result?.Item2) ? "Không tìm thấy dữ liệu" : result.Item2;
                if (result != null 
                    && result.Item1 
                    && !string.IsNullOrEmpty(result.Item2)
                    && ValidateHelper.TryParseJObject(result.Item2, out JObject obj, out string err))
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = obj,
                        Message = $"OK",
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new ResultResponse
                    {
                        Data = null,
                        Message = msgError,
                        Status = HttpStatusCode.NotFound
                    });
                }
            }
            catch(Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi dữ liệu server {ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }

        //revert points redeem capillary
        [HttpPost]
        [Route("v2/loyalty/capillary/points/redeem/revert")]
        public async Task<HttpResponseMessage> RevertPointsRedeem(RevertPointsRedeemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ExceptionModels();
                }
                var result = await _loyaltyService.RevertPointsRedeem(_redisManager, _memoryCacheService, request);
                return Request.CreateResponse(result.Status, new ResultResponse
                {
                    Data = result.Data,
                    Message = result.Message,
                    Status = result.Status
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi dữ liệu server {ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }
    }
}
