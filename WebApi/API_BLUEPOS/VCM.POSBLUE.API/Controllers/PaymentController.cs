using Antlr.Runtime;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TCX.API.Common.Dtos.Capillary.Vouchers;
using TCX.API.Common.Dtos.Coupon;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.WebApiCore;
using TCX.WebApiCore.AppServices.Coupon;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;
using VCM.POSBLUE.Data.DataBaseContext;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api/v2/partner")]
    public class PaymentController : BaseController
    {
        private readonly UrboxService _urboxService;
        private readonly GotITService _gotITService;
        private readonly LoyaltyService _lostaltyService;
        private readonly LoyaltyRepository _loyaltyRepository;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly CouponCapillaryService _capillaryCouponService;
        private readonly OneUService _oneUService;
        private readonly WinXService _winXService;
        private readonly RedisManager _redisManager;
        public PaymentController()
        {
            _loyaltyRepository = new LoyaltyRepository();
            _redisManager = new RedisManager();
            _urboxService = new UrboxService();
            _gotITService = new GotITService();
            _lostaltyService = new LoyaltyService();
            _memoryCacheService = new MemoryCacheService();
            _capillaryCouponService = new CouponCapillaryService();
            _oneUService = new OneUService();
            _winXService = new WinXService();
        }

        [HttpPost]
        [Route("voucher/check")]
        public async Task<HttpResponseMessage> ValidateVoucher(CheckVoucherPartnerPOSRequest modelPOS)
        {
            HttpResponseMessage httpResponseMessage;
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                switch (modelPOS.Partner.ToUpper())
                {
                    case "URBOX":
                        var checkUrbox = await _urboxService.CheckSerialUrbox(modelPOS);
                        if (checkUrbox.Item1)
                        {
                            httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK, ResponseHelper.SusscessResponse(HttpStatusCode.OK, checkUrbox.Item3, checkUrbox.Item2));
                        }
                        else
                        {
                            httpResponseMessage = Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, checkUrbox.Item3, checkUrbox.Item2));
                        }
                        break;
                    case "GOTIT":
                        var checkGotIt = await _gotITService.CheckMultiple(modelPOS);
                        if (checkGotIt.Item1)
                        {
                            httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK, ResponseHelper.SusscessResponse(HttpStatusCode.OK, checkGotIt.Item3, checkGotIt.Item2));
                        }
                        else
                        {
                            httpResponseMessage = Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, checkGotIt.Item3, checkGotIt.Item2));
                        }
                        break;
                    case "ONEU":
                        var resultResponse = await _oneUService.Estimate(modelPOS);
                        httpResponseMessage = Request.CreateResponse(resultResponse.Status, resultResponse);
                        break;
                    default:
                        httpResponseMessage = Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, "Tham số PartnerCode truyền vào không đúng " + modelPOS.Partner ?? "", null));
                        break;
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Exception:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = ex.Message
                });
            }
            return httpResponseMessage;
        }

        [HttpPost]
        [Route("voucher/update-status")]
        public async Task<HttpResponseMessage> UpdateStatusVoucher(UpdateStatusVoucherPartnerRequest modelPOS)
        {
            HttpResponseMessage httpResponseMessage;
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                switch (modelPOS.Partner.ToUpper())
                {
                    case "URBOX":
                        var checkUrbox = await _urboxService.PayCodelUrbox(modelPOS);
                        if (checkUrbox.Item1)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, ResponseHelper.SusscessResponse(HttpStatusCode.OK, checkUrbox.Item3, checkUrbox.Item2));
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, checkUrbox.Item3, checkUrbox.Item2));
                        }
                    case "GOTIT":
                        var checkUsedGotIt = await _gotITService.MarkUseMultiple(modelPOS);
                        if (checkUsedGotIt.Item1)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, ResponseHelper.SusscessResponse(HttpStatusCode.OK, checkUsedGotIt.Item3, checkUsedGotIt.Item2));
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, checkUsedGotIt.Item3, checkUsedGotIt.Item2));
                        }
                    case "ONEU":
                        var resultResponse = await _oneUService.Redeem(modelPOS);
                        httpResponseMessage = Request.CreateResponse(resultResponse.Status, resultResponse);
                        break;
                    default:
                        httpResponseMessage = Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, "Tham số PartnerCode truyền vào không đúng " + modelPOS.Partner ?? "", null));
                        break;
                }

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
            return httpResponseMessage;
        }

        [HttpPost]
        [Route("coupon/check")]
        public async Task<HttpResponseMessage> ValidateCouponAsync(CheckVoucherPartnerPOSRequest modelPOS)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                HttpResponseMessage httpResponseMessage;
                switch (modelPOS.Partner.ToUpper())
                {
                    case "CAP":
                        if (string.IsNullOrEmpty(modelPOS.PhoneNumber))
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, $"Nhập số điện thoại hội viên WIN để sử dụng Coupon"));
                        }
                        if (modelPOS.SerialNo.Count > 1)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, $"Giới hạn 1 coupon mỗi lần kiểm tra"));
                        }

                        string voucherCapillary = modelPOS.SerialNo.FirstOrDefault();
                        var checkIsCapillary = ValidateHelper.IsVoucherCapillary(voucherCapillary, "CAP");
                        if (checkIsCapillary.Item1)
                        {
                            if (!string.IsNullOrEmpty(checkIsCapillary.Item2) && checkIsCapillary.Item2 == "WINX")
                            {
                                var dataWinX = await _winXService.ResolveDynamicVouchers(_memoryCacheService, modelPOS.PosNo, voucherCapillary);
                                if (string.IsNullOrEmpty(dataWinX.Item1))
                                {
                                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                    {
                                        Data = null,
                                        Message = dataWinX.Item2,
                                        Status = HttpStatusCode.BadRequest
                                    });
                                }
                                else
                                {
                                    voucherCapillary = dataWinX.Item1;
                                }
                            }
                        }

                        var result = _capillaryCouponService.ValidateCoupon(_loyaltyRepository, _redisManager, _memoryCacheService, modelPOS.StoreNo, modelPOS.PosNo, modelPOS.PhoneNumber, voucherCapillary, modelPOS.Items, true);
                        if (result.Item1)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = result.Item3,
                                Message = result.Item2,
                                Status = HttpStatusCode.OK
                            });
                        }
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = result.Item3,
                            Message = result.Item2,
                            Status = HttpStatusCode.BadRequest,
                            MessageTechnical = result.Item2
                        });

                    default:
                        httpResponseMessage = Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, "Tham số PartnerCode truyền vào không đúng " + modelPOS.Partner ?? "", null));
                        break;
                }
                return httpResponseMessage;
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi Exception:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = $"ValidateCoupon.Exception:{JsonConvert.SerializeObject(ex)}"
                });
            }
        }

        [HttpPost]
        [Route("coupon/redeem")]
        public HttpResponseMessage CouponRedeem(UpdateStatusVoucherPartnerRequest modelPOS)
        {
            HttpResponseMessage httpResponseMessage;
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                switch (modelPOS.Partner.ToUpper())
                {
                    case "CAP":
                        if (string.IsNullOrEmpty(modelPOS.PhoneNumber))
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, $"Nhập số điện thoại hội viên WIN để sử dụng voucher"));
                        }

                        var result = _lostaltyService.RedeemCouponCapillary(_redisManager, _memoryCacheService, modelPOS, false);
                        return Request.CreateResponse(result.Status, result);
                    default:
                        httpResponseMessage = Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, "Tham số PartnerCode truyền vào không đúng " + modelPOS.Partner ?? "", null));
                        break;
                }

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
            return httpResponseMessage;
        }

        [HttpGet]
        [Route("coupon/list/user")]
        public async Task<HttpResponseMessage> GetCouponByUser(string partner, string storeNo, string mobile, string status)
        {
            HttpResponseMessage httpResponseMessage;
            if (string.IsNullOrEmpty(partner) || string.IsNullOrEmpty(mobile) || string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(status))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, $"Tham số truyền vào không đúng"));
            }
            try
            {
                switch (partner.ToUpper())
                {
                    case "CAP":
                        var result = await _capillaryCouponService.GetListCouponByUser(_memoryCacheService, storeNo, mobile, status);
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
                    default:
                        httpResponseMessage = Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, "Tham số PartnerCode truyền vào không đúng " + partner ?? "", null));
                        break;
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
            return httpResponseMessage;
        }

        [HttpGet]
        [Route("coupon/detail")]
        public async Task<HttpResponseMessage> GetCouponDetail(string partner, string storeNo, string serialNo)
        {
            HttpResponseMessage httpResponseMessage;
            if (string.IsNullOrEmpty(partner) || string.IsNullOrEmpty(serialNo) || string.IsNullOrEmpty(storeNo))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, $"Tham số truyền vào không đúng"));
            }
            try
            {
                switch (partner.ToUpper())
                {
                    case "CAP":
                        var result = await _capillaryCouponService.GetCouponDetailsCapillary(_memoryCacheService, storeNo, serialNo);
                        if (result.Item1 == HttpStatusCode.OK)
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
                    default:
                        httpResponseMessage = Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, "Tham số PartnerCode truyền vào không đúng " + partner ?? "", null));
                        break;
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
            return httpResponseMessage;
        }
        
        [HttpPost]
        [Route("coupon/re-active")]
        public async Task<HttpResponseMessage> ReActiveCoupon(ReActiveCouponRequest request)
        {
            HttpResponseMessage httpResponseMessage;
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                switch (request.Partner.ToUpper())
                {
                    case "CAP":

                        var result = await _capillaryCouponService.ReActivateCoupons(_memoryCacheService, request.StoreNo, request.SerialNo);
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
                                Status = HttpStatusCode.BadRequest,
                                MessageTechnical = JsonConvert.SerializeObject(result.Item3)
                            });
                        }
                    default:
                        httpResponseMessage = Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, "Tham số Partner truyền vào không đúng " + request.Partner ?? "", null));
                        break;
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
            return httpResponseMessage;
        }
    }
}