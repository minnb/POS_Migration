using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore;
using TCX.WebApiCore.AppServices;
using TCX.WebApiCore.AppServices.FMV;
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
    public class LoyaltyController : BaseController
    {
        private readonly LoyaltyService _loyaltyService;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly CXService _cXService;

        private readonly RedisManager _redisManager;
        private readonly LoyaltyOfflineService _loyaltyOfflineService;
        private readonly LoyaltyRepository _loyaltyRepository;
        private readonly AkaChainLoyaltyService _akaChainLoyaltyService;

        private static readonly RedisManager _sharedRedisManager = new RedisManager();
        private static readonly LoyaltyOfflineService _sharedLoyaltyOfflineService = new LoyaltyOfflineService(_sharedRedisManager);
        private static readonly MemoryCacheService _sharedMemoryCacheService = new MemoryCacheService();
        private static readonly LoyaltyService _sharedLoyaltyService = new LoyaltyService();
        private static readonly AkaChainLoyaltyService _sharedAkaChainLoyaltyService = new AkaChainLoyaltyService();
        public LoyaltyController()
        {
            _memoryCacheService = _sharedMemoryCacheService;
            _cXService = new CXService();
            _redisManager = _sharedRedisManager;
            _loyaltyService = _sharedLoyaltyService;
            _loyaltyOfflineService = _sharedLoyaltyOfflineService;
            _loyaltyRepository = new LoyaltyRepository();
            _akaChainLoyaltyService = _sharedAkaChainLoyaltyService;
        }

        //member detail
        [HttpGet]
        [Route("v2/loyalty/customer/get")]
        public async Task<HttpResponseMessage> GetCustomerDetail(string numberCard,string posID, string storeNo,string clubCode = "",bool isMobile = false,bool isLog = true)
        {
            if (string.IsNullOrEmpty(numberCard) || numberCard.Length < 9)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Số điện thoại/số thẻ đang bị trống hoặc không đủ chiều dài ký tự",
                    Status = HttpStatusCode.BadRequest
                });
            }

            try
            {
                var res = await _akaChainLoyaltyService.GetMemberProfile(_memoryCacheService, "Phone", numberCard);
                if (res.Status == HttpStatusCode.OK)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = res.Data,
                        Message = "OK",
                        Status = HttpStatusCode.OK,
                        MessageTechnical = clubCode
                    });
                }
                else
                {
                    return Request.CreateResponse(res.Status, new ResultResponse
                    {
                        Data = res.Data,
                        Message = res.Message,
                        Status = res.Status,
                        MessageTechnical = res.MessageTechnical
                    });
                }
            }
            catch (Exception ex) 
            { 
                FileHelper.WriteExpLogs("Exception GetCustomerDetail", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi dữ liệu server {ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }

            /*

            var checkMemberCapillary = NumberHelper.IsMemberCapillary(numberCard, isMobile);
            if(checkMemberCapillary.Item1) //Capillary
            {
                //switch offline loyalty
                if (await _loyaltyOfflineService.IsOfflineCapillary()) 
                {
                    return Request.CreateResponse(_loyaltyOfflineService.GetMemberInfoOfflineSwitch(_redisManager, _loyaltyRepository, numberCard, storeNo, posID));
                }

                var getData = await _loyaltyService.GetInfoMemberCapillary( _redisManager, _memoryCacheService, numberCard, storeNo, posID, checkMemberCapillary.Item2, "WCM", isMobile);
                if(getData.Status == HttpStatusCode.RequestTimeout)
                {
                    return Request.CreateResponse(_loyaltyOfflineService.GetMemberInfoOfflineSwitch(_redisManager, _loyaltyRepository, numberCard, storeNo, posID, getData.Message??""));
                }
                else if(getData.Status == HttpStatusCode.SwitchingProtocols)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = _loyaltyOfflineService.GetMemberInfoOfflineSwitch(_redisManager, _loyaltyRepository, numberCard, storeNo, posID),
                        Message = "OFF",
                        Status = HttpStatusCode.OK,
                        MessageTechnical = "Switch chuyển chế độ offline"
                    });
                }
                else if(getData.Status == HttpStatusCode.OK)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = getData.Data,
                        Message = "OK",
                        Status = HttpStatusCode.OK,
                        MessageTechnical = clubCode
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
                    Message = LoyaltyHelper.MessageNotValidPhone(numberCard),
                    Status = HttpStatusCode.BadRequest
                });
            }
            */
        }
        
        //register
        [HttpPost]
        [Route("v2/loyalty/customer")]
        public HttpResponseMessage CustomerRegistration(WinLife_Register_POS_Request modelPOS)
        {
            try
            {
                modelPOS.phoneNo = FormatHelper.PhoneNumberVietNam(modelPOS.phoneNo);
                if (!ModelState.IsValid)
                {
                    return ExceptionModels();
                }
                else if (NumberHelper.IsPhoneNumber(modelPOS.phoneNo))
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

                    var result = _loyaltyService.CustomerRegistration(_redisManager, _memoryCacheService, modelPOS, ChannelCapEnum.POS.ToString());
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
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = LoyaltyHelper.MessageNotValidPhone(modelPOS.phoneNo),
                        Status = HttpStatusCode.BadRequest
                    });
                }
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
        [Route("v2/loyalty/customer/update")]
        public HttpResponseMessage CustomerUpdate(WinLife_SmartPOS_Update_Customer_Req_POS modelPOS)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ExceptionModels();
                }
                else if (NumberHelper.IsPhoneNumber(modelPOS.phoneNo))
                {
                    var result = _loyaltyService.CustomerUpdate(_memoryCacheService, modelPOS, ChannelCapEnum.POS.ToString());
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
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = LoyaltyHelper.MessageNotValidPhone(modelPOS.phoneNo),
                        Status = HttpStatusCode.BadRequest
                    });
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Error(String.Format("Controller.Update Exception {0}}", ex.Message.ToString()));
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi dữ liệu server {ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }       

        //addTransactions
        [HttpPost]
        [Route("v2/loyalty/transaction/add")]
        public async Task<HttpResponseMessage> AddTransaction(VinIDSalesRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            
            if (NumberHelper.IsPhoneNumber(model.CardNumber))
            {
                var result = await _akaChainLoyaltyService.InputDataAsync(_memoryCacheService, model);
                return Request.CreateResponse(result.Status, new ResultResponse
                {
                    Data = result.Data,
                    Message = result.Message,
                    Status = result.Status
                });

                //switch offline loyalty
                //if (await _loyaltyOfflineService.IsOfflineCapillary())
                //{
                //    return Request.CreateResponse(_loyaltyOfflineService.GetAddTransactionOfflineSwitch(model.OrderNo, model.CardNumber));
                //}

                //var result = await _loyaltyService.AddTransactionCapillaryV2(_memoryCacheService, _redisManager, model);
                //return Request.CreateResponse(result.Item1);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = LoyaltyHelper.MessageNotValidPhone(model.CardNumber),
                    Status = HttpStatusCode.BadRequest
                });
            }
        }
       
        //return
        [HttpPost]
        [Route("v2/loyalty/transaction/refund")]
        public async Task<HttpResponseMessage> RefundTransactionAsync(VinIDRefundRequest model)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            
            if (NumberHelper.IsPhoneNumber(model.CardNumber))
            {
                //switch offline loyalty
                if (await _loyaltyOfflineService.IsOfflineCapillary())
                {
                    return Request.CreateResponse(_loyaltyOfflineService.GetAddTransactionOfflineSwitch(model.OrderNo, model.CardNumber));
                }

                return Request.CreateResponse(await _loyaltyService.RefundPointCapillary(_memoryCacheService, _redisManager, model));
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = LoyaltyHelper.MessageNotValidPhone(model.CardNumber),
                    Status = HttpStatusCode.BadRequest
                });
            }
        }

        //check other-status in member detail
        [HttpPost]
        [Route("v2/loyalty/other-status")]
        public HttpResponseMessage OtherStatusUpdate(OtherStatusUpdate otherStatusUpdate)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                otherStatusUpdate.PhoneNumber = FormatHelper.PhoneNumberVietNam(otherStatusUpdate.PhoneNumber);
                var result = _loyaltyService.OtherStatusUpdate(_memoryCacheService, otherStatusUpdate);
                return Request.CreateResponse(result.Status, new ResultResponse
                {
                    Data = result.Data,
                    Message = result.Message,
                    Status = result.Status
                });
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("Exception v2/loyalty/other-status", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Exception lỗi xử lý dữ liệu server",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

    }
}
