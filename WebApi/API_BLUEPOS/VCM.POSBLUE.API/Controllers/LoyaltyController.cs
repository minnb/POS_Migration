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
        readonly string clubCodeWin = "WIN";
        readonly string linkAPI_VINID = WebConfigurationManager.AppSettings["LinkAPI"];
        readonly string linkAPIScanAndGo = WebConfigurationManager.AppSettings["APISCANGO"];
        readonly string API_KEY = WebConfigurationManager.AppSettings["API_KEY"];
        readonly string API_SECRET = WebConfigurationManager.AppSettings["API_SECRET"];
        readonly string configTimeZone = WebConfigurationManager.AppSettings["TimeZone"];
        readonly string configVersionNo = WebConfigurationManager.AppSettings["VersionNo"];
        readonly string configAppVersion = WebConfigurationManager.AppSettings["AppVersion"];
        readonly string configMerchantId = WebConfigurationManager.AppSettings["MerchantId"];
        readonly string configTerminalId = WebConfigurationManager.AppSettings["TerminalId"];
        readonly string configOperatorId = WebConfigurationManager.AppSettings["OperatorId"];
        readonly string configPassword = WebConfigurationManager.AppSettings["Password"];
        readonly string CXUrl = WebConfigurationManager.AppSettings["CXUrl"];
        readonly string CXUser = WebConfigurationManager.AppSettings["CXUser"];
        readonly string CXPassword = WebConfigurationManager.AppSettings["CXPassword"];
        private readonly Cache cache = HttpContext.Current.Cache;
        private OperatorLoginRs loginCache = new OperatorLoginRs();
        private VinIDBLO _logVID { get; set; }
        private LogAPIModel _modelLog { get; set; }
        private CX_LogAPIModel _modelLogCX { get; set; }
        private LogTransactionLoyaltyModel modelTrans { get; set; }
        private readonly CommonBLO _CommonBLO;
        private readonly OneFlexiAxisService.OneFlexiAxisService wsVinID;
        private readonly KibanaService _kibana;
        private readonly WinLifeService _winLifeService;
        private readonly LoyaltyService _loyaltyService;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly CXService _cXService;
        private readonly WinCustomerService _winCustomerService;
        private readonly RedisManager _redisManager;
        private readonly LoyaltyOfflineService _loyaltyOfflineService;
        private readonly LoyaltyRepository _loyaltyRepository;

        private static readonly RedisManager _sharedRedisManager = new RedisManager();
        private static readonly LoyaltyOfflineService _sharedLoyaltyOfflineService = new LoyaltyOfflineService(_sharedRedisManager);
        private static readonly KibanaService _sharedKibana = new KibanaService();
        private static readonly MemoryCacheService _sharedMemoryCacheService = new MemoryCacheService();
        private static readonly LoyaltyService _sharedLoyaltyService = new LoyaltyService();
        public LoyaltyController()
        {
            _logVID = new VinIDBLO();
            _modelLog = new LogAPIModel();
            _modelLogCX = new CX_LogAPIModel();
            modelTrans = new LogTransactionLoyaltyModel();
            _CommonBLO = new CommonBLO();
            wsVinID = new OneFlexiAxisService.OneFlexiAxisService();
            _winLifeService = new WinLifeService(CXUrl, CXUser, CXPassword);
            _memoryCacheService = _sharedMemoryCacheService;
            _cXService = new CXService();
            _redisManager = _sharedRedisManager;
            _kibana = _sharedKibana;
            _loyaltyService = _sharedLoyaltyService;
            _winCustomerService = new WinCustomerService();
            _loyaltyOfflineService = _sharedLoyaltyOfflineService;
            _loyaltyRepository = new LoyaltyRepository();
        }

        //Link api mới
        [HttpGet]
        [Route("v2/loyalty/retry")]
        public async Task<HttpResponseMessage> LoyaltyRetry()
        {
            await _loyaltyService.LoyaltyRetry(_memoryCacheService, _redisManager);
            await Task.Delay(100);
            await LoyaltyExtentionService.RetryTransactionOffline(_memoryCacheService,_redisManager,_loyaltyService,_winCustomerService);
            Thread.Sleep(100);
            _loyaltyService.RetryUpdateStatusWinscore();
            //retry cashback winpay
            using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
            {
                _winCustomerService.RetryWinpayAccumulationError(db, _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD()));
            }

            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
            {
                Data = null,
                Message = $"Success",
                Status = HttpStatusCode.OK
            });
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

            var checkMemberCapillary = NumberHelper.IsMemberCapillary(numberCard, isMobile);
            if (LoyaltyHelper.IsVINID(numberCard) && !checkMemberCapillary.Item1)
            {
                var isCheckAwardSpend = _memoryCacheService.GetStores().Where(x => x.No == storeNo && x.BusinessAreaNo == "HF").FirstOrDefault();
                if (isCheckAwardSpend != null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã CH/ST {storeNo} HF không được tích/tiêu VINID",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                return VINIDGetInfoMember(numberCard, posID, storeNo, isLog);
            }
            else if(checkMemberCapillary.Item1) //Capillary
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
            if ((model.CardNumber.Substring(0, 4) == "8888" || model.CardNumber.Substring(0, 5) == "66668") && model.CardNumber.Length == 16)
            {
                if (!_CommonBLO.IsCheckAwardSpend(model.MerchantId))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã CH/ST {model.MerchantId} không được tích/tiêu VINID",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                return VINIDSales(model.QRCode, model.CardNumber, model.MerchantId, model.TerminalId, model.InvoiceNo, (int)model.SpendPoints, model.BillAmount, model.OrderNo, model.IsOffline, model.OrderAmount, model.VirtualCard, false, model.AmountToEarn);
            }
            else if (NumberHelper.IsPhoneNumber(model.CardNumber))
            {
                //switch offline loyalty
                if (await _loyaltyOfflineService.IsOfflineCapillary())
                {
                    return Request.CreateResponse(_loyaltyOfflineService.GetAddTransactionOfflineSwitch(model.OrderNo, model.CardNumber));
                }

                var result = await _loyaltyService.AddTransactionCapillaryV2(_memoryCacheService, _redisManager, model);
                return Request.CreateResponse(result.Item1);
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

            if ((model.CardNumber.Substring(0, 4) == "8888" || model.CardNumber.Substring(0, 5) == "66668") && model.CardNumber.Length == 16)
            {
                return VINIDRefund(model.QRCode, model.CardNumber, model.MerchantId, model.TerminalId, model.InvoiceNo, model.OrderNo, model.OrigOrderNo, (int) model.SpendPoints, model.RefundAmount, model.IsOffline, model.OrderAmount, model.IsScanAndGo, model.CXRefundAmount);
            }
            else if (NumberHelper.IsPhoneNumber(model.CardNumber))
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


        // VINID============================================
        [HttpGet]
        [Route("vinid/GetInfoMember")]//Thông tin khách hàng
        public async Task<HttpResponseMessage> GetInfoMember(string numberCard, string posID, string storeNo, string clubCode = "", bool isLog = true)
        {
            if (string.IsNullOrEmpty(numberCard))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"numberCard đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (((numberCard.Substring(0, 4) == "8888" || numberCard.Substring(0, 5) == "66668") && numberCard.Length == 16) || numberCard.Substring(0, 1) == "P")
            {
                var isCheckAwardSpend = _memoryCacheService.GetStores().FirstOrDefault(x => x.No == storeNo && x.BusinessAreaNo == "HF");
                if (isCheckAwardSpend != null && isCheckAwardSpend.BusinessAreaNo == "HF") //(!_CommonBLO.IsCheckAwardSpend(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã CH/ST {storeNo} HF không được tích/tiêu VINID",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                return VINIDGetInfoMember(numberCard, posID, storeNo, isLog);
            }
            else 
            {
                return Request.CreateResponse(await _loyaltyService.GetInfoMemberCapillary(_redisManager, _memoryCacheService, numberCard, storeNo, posID, "PHONE"));
            }
        }

        [HttpGet]
        [Route("vinid/Sales")]
        public HttpResponseMessage Sales(string QRCode, string CardNumber, string MerchantId, string TerminalId, string InvoiceNo, int SpendPoints, decimal BillAmount, string OrderNo, bool IsOffline, decimal OrderAmount, string VirtualCard = "")
        {
            if (string.IsNullOrEmpty(MerchantId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã cửa hàng đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(TerminalId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã POS đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(InvoiceNo))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "InvoiceNo đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (OrderAmount <= 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "OrderAmount không hợp lệ",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if ((CardNumber.Substring(0, 4) == "8888" || CardNumber.Substring(0, 5) == "66668") && CardNumber.Length == 16)
            {
                if (!_CommonBLO.IsCheckAwardSpend(MerchantId))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã CH/ST {MerchantId} không được tích/tiêu VINID",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                return VINIDSales(QRCode, CardNumber, MerchantId, TerminalId, InvoiceNo, SpendPoints, BillAmount, OrderNo, IsOffline, OrderAmount, VirtualCard);
            }
            else//CrownX
            {
                return CXSales(CardNumber, MerchantId, TerminalId, InvoiceNo, SpendPoints, (double)BillAmount, OrderNo, IsOffline, (double)OrderAmount);
            }
        }

        [HttpPost]
        [Route("vinid/SalesV2")]
        public HttpResponseMessage SalesV2(VinIDSalesRequest model)
        {
            if (string.IsNullOrEmpty(model.MerchantId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã cửa hàng đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(model.TerminalId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã POS đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(model.InvoiceNo))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "InvoiceNo đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (model.OrderAmount <= 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "OrderAmount không hợp lệ",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if ((model.CardNumber.Substring(0, 4) == "8888" || model.CardNumber.Substring(0, 5) == "66668") && model.CardNumber.Length == 16)
            {
                if (!_CommonBLO.IsCheckAwardSpend(model.MerchantId))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã CH/ST {model.MerchantId} không được tích/tiêu VINID",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                return VINIDSales(model.QRCode, model.CardNumber, model.MerchantId, model.TerminalId, model.InvoiceNo, (int)model.SpendPoints, model.BillAmount, model.OrderNo, model.IsOffline, model.OrderAmount, model.VirtualCard, false, model.AmountToEarn);
            }
            else //CrownX
            {
                if (model.AmountToEarn == null || model.AmountToEarn.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Vui lòng tính giá trị tích điểm theo merchanId",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var modelV2 = new PLSalePOSV2RequestModel
                {
                    storeCode = model.MerchantId,
                    posCode = model.TerminalId,
                    cardNumber = model.CardNumber,
                    orderNo = model.OrderNo,
                    orderAmount = (double)model.OrderAmount,
                    spendPoints = (int)model.SpendPoints,
                    amountToEarn = model.AmountToEarn
                };

                return CXSalesV2(modelV2);
            }

        }

        [HttpGet]
        [Route("vinid/Refund")]//Trả hàng81910022
        public HttpResponseMessage Refund(string QRCode, string CardNumber, string MerchantId, string TerminalId, string InvoiceNo, string OrderNo, string OrigOrderNo, int SpendPoints, decimal RefundAmount, bool IsOffline, decimal OrderAmount, bool IsScanAndGo = false)
        {
            if (string.IsNullOrEmpty(CardNumber))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Số thẻ khách hàng đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrEmpty(MerchantId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã cửa hàng đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrEmpty(TerminalId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã POS đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(InvoiceNo))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "InvoiceNo đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrEmpty(OrigOrderNo))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Đơn hàng bán đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (OrderAmount <= 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "OrderAmount không được bằng 0",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if ((CardNumber.Substring(0, 4) == "8888" || CardNumber.Substring(0, 5) == "66668") && CardNumber.Length == 16)
            {
                return VINIDRefund(QRCode, CardNumber, MerchantId, TerminalId, InvoiceNo, OrderNo, OrigOrderNo, SpendPoints, RefundAmount, IsOffline, OrderAmount, IsScanAndGo);
            }
            else
            {
                return CXRefund(CardNumber, MerchantId, TerminalId, InvoiceNo, OrderNo, OrigOrderNo, SpendPoints, RefundAmount, IsOffline, OrderAmount);
            }

            //if (CardNumber.Substring(0, 4) == "6868" || CardNumber.Substring(0, 2) == "84")//CX
            //{

            //    return CXRefund(CardNumber, MerchantId, TerminalId, InvoiceNo, OrderNo, OrigOrderNo, SpendPoints, RefundAmount, IsOffline, OrderAmount);

            //}

            //else
            //{
            //    return VINIDRefund(QRCode, CardNumber, MerchantId, TerminalId, InvoiceNo, OrderNo, OrigOrderNo, SpendPoints, RefundAmount, IsOffline, OrderAmount, IsScanAndGo);
            //}

        }

        [HttpPost]
        [Route("vinid/RefundV2")]
        public async Task<HttpResponseMessage> RefundV2(VinIDRefundRequest model)
        {
            if (string.IsNullOrEmpty(model.CardNumber))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Số thẻ khách hàng đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrEmpty(model.MerchantId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã cửa hàng đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrEmpty(model.TerminalId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã POS đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(model.InvoiceNo))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "InvoiceNo đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrEmpty(model.OrigOrderNo))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Đơn hàng bán đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (model.OrderAmount <= 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "OrderAmount không được bằng 0",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if ((model.CardNumber.Substring(0, 4) == "8888" || model.CardNumber.Substring(0, 5) == "66668") && model.CardNumber.Length == 16)
            {
                return VINIDRefund(model.QRCode, model.CardNumber, model.MerchantId, model.TerminalId, model.InvoiceNo, model.OrderNo, model.OrigOrderNo, (int)model.SpendPoints, model.RefundAmount, model.IsOffline, model.OrderAmount, model.IsScanAndGo, model.CXRefundAmount);
            }
            else
            {
                var checkStore = _memoryCacheService.GetStores().FirstOrDefault(x => x.No == model.MerchantId);
                if (checkStore != null && checkStore.PostCode == ChannelCapEnum.WINCARE.ToString())
                {
                    return Request.CreateResponse(await _loyaltyService.RefundPointCapillary(_memoryCacheService, _redisManager, model));
                }

                if (model.CXRefundAmount == null || model.CXRefundAmount.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Vui lòng tính giá trị trả điểm theo mechantId",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                var modelV2 = new CXRefundPOSV2Request
                {
                    cardNumber = model.CardNumber,
                    storeCode = model.MerchantId,
                    posCode = model.TerminalId,
                    orderNo = model.OrderNo,
                    origOrderNo = model.OrigOrderNo,
                    spendPoints = (int)model.SpendPoints,
                    refundAmount = model.CXRefundAmount
                };
                return CXRefundV2(modelV2);
            }

        }

        [HttpPost]
        [Route("vinid/InitTransaction")]//Thanh toán Ví VINID
        public HttpResponseMessage InitTransaction(POS_TransactionRequest pos)
        {

            var posStoreNo = pos.MerchantId;
            var posTerminal = pos.TerminalId;

            if (pos == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Dữ liệu đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.QRCode))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã QRCode đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.MerchantId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã siêu thị đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.TerminalId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã POS đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (pos.QRCode.Substring(0, 4) == "8888" || pos.QRCode.Substring(0, 5) == "66668")
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Mã QRCode {pos.QRCode} không hợp lệ",
                    Status = HttpStatusCode.BadRequest
                });
            }

            var storeMapping = _logVID.StoreMapping(pos.MerchantId, pos.TerminalId);
            if (storeMapping != null)
            {
                pos.TerminalId = storeMapping?.OldVinPayTerminalID;
                pos.MerchantId = storeMapping?.OldVinPayStoreID;
            }

            if (string.IsNullOrEmpty(pos.InvoiceNo))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "InvoiceNo đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (pos.Amount <= 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Amount nhỏ hơn 0",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (pos.BillAmount <= 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "BillAmount nhỏ hơn 0",
                    Status = HttpStatusCode.BadRequest
                });
            }
            int billAmount = Convert.ToInt32(pos.BillAmount);
            int amount = Convert.ToInt32(pos.Amount);

            var currency = "VND";
            var channel_code = "VINMART";
            var description = "Thanh toán ví VINID";
            Random rnd = new Random();
            var transaction_ref_number = pos.InvoiceNo + "" + rnd.Next(0, 9999);

            Int32 current_timestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day))).TotalSeconds;
            var order_created_at = current_timestamp.ToString();
            string plainTextData = "|" + pos.QRCode + "|" + channel_code + "|" + pos.MerchantId + "|" + pos.TerminalId + "|" + transaction_ref_number + "|" + pos.InvoiceNo + "|" + currency + "|" + order_created_at + "|" + billAmount + "|" + amount;
            string signature = VINIDHelper.VinPaySignature(plainTextData);
            TransactionRequest model = new TransactionRequest
            {
                transaction_ref_number = transaction_ref_number,
                code_value = pos.QRCode,
                description = description,
                merchant_id = pos.MerchantId,
                terminal_id = pos.TerminalId,
                order_created_at = order_created_at,
                currency = currency,
                invoice_no = pos.InvoiceNo,
                bill_amount = billAmount.ToString(),
                amount = amount.ToString(),
                channel_code = channel_code,
                signature = signature,
            };
            var modelPos = new POS_TransactionRequest
            {
                QRCode = pos.QRCode,
                InvoiceNo = pos.InvoiceNo,
                MerchantId = posStoreNo,
                TerminalId = posTerminal,
                Amount = pos.Amount,
                BillAmount = pos.BillAmount
            };
            string requestPOS = JsonConvert.SerializeObject(modelPos);
            try
            {
                var serialize = new JavaScriptSerializer
                {
                    MaxJsonLength = int.MaxValue
                };
                string json = serialize.Serialize(model);
                string subUrl = "/internal/checkout/v1/transactions";
                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPI_VINID);
                        client.DefaultRequestHeaders.Add("X-Api-Key", API_KEY);
                        client.DefaultRequestHeaders.Add("X-Api-Secret", API_SECRET);
                        client.DefaultRequestHeaders.Add("X-Lang", "vn");
                        client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        var response = client.PostAsync(subUrl, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        if (response.IsSuccessStatusCode)
                        {
                            var content = response.Content.ReadAsStringAsync();
                            InitTransactionResponse resp = JsonConvert.DeserializeObject<InitTransactionResponse>(content.Result);
                            _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "InitTransaction", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = serialize.Serialize(resp), ResponseTotal = milliseconds, DateTime = DateTime.Now };


                            ResultTransactionResponse result = new ResultTransactionResponse
                            {
                                Code = resp.meta.code,
                                TransactionID = resp.data?.transaction_id,
                                Message = resp.meta.message,
                                TransactionStatus = resp.data?.transactionStatus,
                                TransactionWalletNumber = resp.data?.transaction_wallet_number
                            };

                            if (result.Code == 200)
                            {
                                _modelLog.StatusVINID = true;
                                _logVID.WriteLogAPI(_modelLog);

                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Data = result,
                                    Message = "Thành công",
                                    Status = HttpStatusCode.OK
                                });
                            }
                            else
                            {
                                _modelLog.StatusVINID = false;
                                _logVID.WriteLogAPI(_modelLog);

                                var resultMessage = resp.meta.message;
                                var spMessage = resp.meta.message.Split('\t');
                                if (spMessage.Length > 1)
                                    resultMessage = spMessage[1];
                                return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                {
                                    Data = null,
                                    Message = resultMessage,
                                    Status = HttpStatusCode.Conflict
                                });
                            }
                        }
                        else
                        {
                            _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "InitTransaction", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = response.ToString(), ResponseTotal = milliseconds, StatusVINID = false, DateTime = DateTime.Now };
                            _logVID.WriteLogAPI(_modelLog);
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = "Lỗi hệ thống VINPAY, Vui lòng liên hệ IT để được hỗ trợ",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "InitTransaction", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = ex.Message, StatusVINID = false, DateTime = DateTime.Now };
                        _logVID.WriteLogAPI(_modelLog);
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = "Lỗi hệ thống VINPAY, Vui lòng liên hệ IT để được hỗ trợ",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _modelLog = new LogAPIModel { CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "InitTransaction", RequestPOS = ex.Message, RequestXML = ex.Message, ResponseXML = ex.Message, StatusVINID = false, DateTime = DateTime.Now };
                _logVID.WriteLogAPI(_modelLog);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = "Lỗi hệ thống VINPAY API, Vui lòng liên hệ IT để được hỗ trợ",
                    Status = HttpStatusCode.InternalServerError
                });
                //return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }

        [HttpGet]
        [Route("vinid/GetTransaction")]//GetTransaction
        public HttpResponseMessage GetTransaction(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã giao dịch không được rỗng",
                    Status = HttpStatusCode.BadRequest
                });

            }

            // var currency = "VND";
            //var channel_code = "VINMART";
            try
            {
                // var serialize = new JavaScriptSerializer();
                // serialize.MaxJsonLength = int.MaxValue;
                //string json = serialize.Serialize(model);
                //string subUrl = "/internal/checkout/v1/transactions";
                string subUrl = "/internal/checkout/v1/transactions/?transaction_id=" + transactionId;
                using (var client = new HttpClient())
                {
                    try
                    {
                        //var st1 = new Stopwatch();
                        //st1.Start();
                        //long milliseconds = 0;

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPI_VINID);
                        client.DefaultRequestHeaders.Add("X-Api-Key", API_KEY);
                        client.DefaultRequestHeaders.Add("X-Api-Secret", API_SECRET);
                        client.DefaultRequestHeaders.Add("X-Lang", "vn");
                        client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        //var test = client.GetAsync(subUrl);
                        var response = client.GetAsync(subUrl).Result;

                        //milliseconds = st1.ElapsedMilliseconds;
                        //st1.Stop();

                        if (response.IsSuccessStatusCode)
                        {

                            var content = response.Content.ReadAsStringAsync();
                            TransactionDetailResponse resp = JsonConvert.DeserializeObject<TransactionDetailResponse>(content.Result);
                            //modelLog = new LogAPIModel { CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "InitTransaction", RequestXML = json, ResponseXML = serialize.Serialize(resp), DateTime = DateTime.Now };
                            //logVID.WriteLogAPI(modelLog);

                            ResultTransactionResponse result = new ResultTransactionResponse
                            {
                                Code = resp.meta.code,
                                TransactionID = resp.data?.transaction_id,
                                Message = resp.meta.message,
                                TransactionStatus = resp.data?.transaction_status
                            };


                            if (resp.meta.code == 200)
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Data = result,
                                    Message = "Thành công",
                                    Status = HttpStatusCode.OK
                                });
                            }
                            else
                            {
                                var resultMessage = resp.meta.message;
                                var spMessage = resp.meta.message.Split('\t');
                                if (spMessage.Length > 1)
                                    resultMessage = spMessage[1];
                                return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                {
                                    Data = null,
                                    Message = resultMessage,
                                    Status = HttpStatusCode.Conflict
                                });
                            }
                        }
                        else
                        {
                            // modelLog = new LogAPIModel { CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "InitTransaction", RequestXML = json, ResponseXML = response.ToString(), DateTime = DateTime.Now };
                            // logVID.WriteLogAPI(modelLog);

                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = "Lỗi hệ thống VINPAY, Vui lòng liên hệ IT để được hỗ trợ",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        //modelLog = new LogAPIModel { CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "InitTransaction", RequestXML = json, ResponseXML = ex.Message, DateTime = DateTime.Now };
                        // logVID.WriteLogAPI(modelLog);
                        Serilog.Log.Logger.Information("Exception: " + ex.Message.ToString());
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = "Lỗi hệ thống VINPAY, Vui lòng liên hệ IT để được hỗ trợ",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                //modelLog = new LogAPIModel { CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "InitTransaction", RequestXML = ex.Message, ResponseXML = ex.Message, DateTime = DateTime.Now };
                //logVID.WriteLogAPI(modelLog);
                Serilog.Log.Logger.Information("Exception: " + ex.Message.ToString());
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = "Lỗi hệ thống VINPAY API, Vui lòng liên hệ IT để được hỗ trợ",
                    Status = HttpStatusCode.InternalServerError
                });
                // return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }

        [HttpPut]
        [Route("vinid/CancelTransaction")]//Hủy thanh toán
        public HttpResponseMessage CancelTransaction(POS_CancelTransactionRequest pos)
        {
            if (pos == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Dữ liệu đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.TransactionId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = "Mã giao dịch không được trống.",
                    Message = "Thất bại",
                    Status = HttpStatusCode.BadRequest
                });
            }
            string requestPOS = JsonConvert.SerializeObject(pos);
            var channel_code = "VINMART";
            string plainTextData = pos.TransactionId + "|" + channel_code;
            CancelTransactionRequest model = new CancelTransactionRequest
            {
                channel_code = channel_code,
                transaction_id = pos.TransactionId,
                signature = VINIDHelper.VinPaySignature(plainTextData)
            };
            try
            {
                var serialize = new JavaScriptSerializer
                {
                    MaxJsonLength = int.MaxValue
                };
                string json = serialize.Serialize(model);
                string subUrl = "/internal/checkout/v1/transactions/cancel";
                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPI_VINID);
                        client.DefaultRequestHeaders.Add("X-Api-Key", API_KEY);
                        client.DefaultRequestHeaders.Add("X-Api-Secret", API_SECRET);
                        client.DefaultRequestHeaders.Add("X-Lang", "vn");
                        client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
                        var response = client.PostAsync(subUrl, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        if (response.IsSuccessStatusCode)
                        {
                            var content = response.Content.ReadAsStringAsync();
                            InitTransactionResponse resp = JsonConvert.DeserializeObject<InitTransactionResponse>(content.Result);
                            _modelLog = new LogAPIModel { StoreNo = pos.StoreNo, POSTerminal = pos.PosTerminal, InvoiceNo = pos.OrderNo, ActionType = "CancelTransaction", RequestPOS = requestPOS, RequestXML = json, ResponseXML = serialize.Serialize(resp), ResponseTotal = milliseconds, DateTime = DateTime.Now };

                            if (resp.meta.code == 200)
                            {
                                _modelLog.StatusVINID = true;
                                _logVID.WriteLogAPI(_modelLog);

                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Data = new CancelTransactionResponse { Status = true },
                                    Message = "Thành công",
                                    Status = HttpStatusCode.OK
                                });
                            }
                            else
                            {
                                _modelLog.StatusVINID = false;
                                _logVID.WriteLogAPI(_modelLog);

                                var resultMessage = resp.meta.message;
                                var spMessage = resp.meta.message.Split('\t');
                                if (spMessage.Length > 1)
                                    resultMessage = spMessage[1];
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = new CancelTransactionResponse { Status = false },
                                    Message = resultMessage,
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }
                        else
                        {
                            _modelLog = new LogAPIModel { StoreNo = pos.StoreNo, POSTerminal = pos.PosTerminal, InvoiceNo = pos.OrderNo, RequestPOS = requestPOS, ActionType = "CancelTransaction", RequestXML = json, ResponseXML = response.ToString(), ResponseTotal = milliseconds, StatusVINID = false, DateTime = DateTime.Now };
                            _logVID.WriteLogAPI(_modelLog);

                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = new CancelTransactionResponse { Status = false },
                                Message = "Lỗi hệ thống VINPAY, Vui lòng liên hệ IT để được hỗ trợ",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _modelLog = new LogAPIModel { StoreNo = pos.StoreNo, POSTerminal = pos.PosTerminal, InvoiceNo = pos.OrderNo, RequestPOS = requestPOS, ActionType = "CancelTransaction", RequestXML = json, ResponseXML = JsonConvert.SerializeObject(ex), StatusVINID = false, DateTime = DateTime.Now };
                        _logVID.WriteLogAPI(_modelLog);
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = new CancelTransactionResponse { Status = false },
                            Message = "Lỗi hệ thống VINPAY, Vui lòng liên hệ IT để được hỗ trợ",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _modelLog = new LogAPIModel { StoreNo = pos.StoreNo, POSTerminal = pos.PosTerminal, InvoiceNo = pos.OrderNo, ActionType = "CancelTransaction", RequestXML = JsonConvert.SerializeObject(ex), ResponseXML = JsonConvert.SerializeObject(ex), StatusVINID = false, DateTime = DateTime.Now };
                _logVID.WriteLogAPI(_modelLog);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = new CancelTransactionResponse { Status = false },
                    Message = "Lỗi hệ thống VINPAY API, Vui lòng liên hệ IT để được hỗ trợ",
                    Status = HttpStatusCode.InternalServerError
                });
                //return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }

        [HttpGet]
        [Route("vinid/ScanAndGo")]
        public HttpResponseMessage ScanAndGo(string barcode)
        {
            //ultil.Info("ScanAndGo", $"Barcode {barcode}");
            string subUrl = "/vincart/v1/vcm?barcode=" + barcode;
            //ultil.Info("ScanAndGo", $"Url:{linkAPIScanAndGo}{subUrl}");

            var serialize = new JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue
            };
            using (var client = new HttpClient())
            {
                _modelLog = new LogAPIModel { StoreNo = "", POSTerminal = "", CardNumber = "", InvoiceNo = barcode, ActionType = "ScanAndGo", RequestPOS = $"{linkAPI_VINID}/{subUrl}", RequestXML = $"{linkAPI_VINID}/{subUrl}", ResponseXML = "", DateTime = DateTime.Now };
                try
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(linkAPIScanAndGo);//link that                  

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    var response = client.GetAsync(subUrl).Result;

                    //ultil.Info("ScanAndGo", $"Response from API: {JsonConvert.SerializeObject(response)}");

                    var content = response.Content.ReadAsStringAsync();

                    //ultil.Info("ScanAndGo", $"ReadAsStringAsync: {content.Result}");
                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    ScanAndGoToVinResponse resp = JsonConvert.DeserializeObject<ScanAndGoToVinResponse>(content.Result);

                    //ultil.Info("ScanAndGo", $"Json object:{JsonConvert.SerializeObject(resp)}");

                    _modelLog.StoreNo = resp.data != null ? resp.data.site_code ?? "" : "";
                    _modelLog.CardNumber = resp.data != null ? resp.data.vin_id_card_number ?? "" : "";
                    _modelLog.ResponseXML = JsonConvert.SerializeObject(resp) ?? "";
                    _modelLog.ResponseTotal = milliseconds;



                    if (resp.meta.code == 200)
                    {
                        _modelLog.StatusVINID = true;
                        _logVID.WriteLogAPI(_modelLog);

                        if (resp.data.is_expired == true)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = "Đơn hàng đã hết hiệu lực thanh toán",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                        if (resp.data.status != 1)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = "Không thể thực hiện tính tiền cho đơn hàng này",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                        var result = new ScanAndGoPOSResponse
                        {
                            OrderNo = resp.data != null ? resp.data.order_no : "",
                            CustomerId = resp.data != null ? resp.data.customer_id : "",
                            CustomerName = resp.data != null ? resp.data.customer_name : "",
                            CustomerPhone = resp.data != null ? resp.data.customer_phone : "",
                            OrderDate = resp.data?.order_date,
                            Amount = resp.data != null ? resp.data.amount : 0,
                            AmountDeducted = resp.data != null ? resp.data.amount_deducted : 0,
                            VinIdPaid = resp.data != null ? resp.data.vin_id_paid : 0,
                            AmountCollect = resp.data != null ? resp.data.amount_collect : 0,
                            VoucherId = resp.data != null ? resp.data.voucher_id : "",
                            VoucherName = resp.data != null ? resp.data.voucher_name : "",
                            VinIdCardNumber = resp.data != null ? resp.data.vin_id_card_number : "",
                            VinIdCardName = resp.data != null ? resp.data.vin_id_card_name : "",
                            DeliveryType = resp.data != null ? resp.data.delivery_type : 0,
                            DeliveryTypeName = resp.data != null ? resp.data.delivery_type_name : "",
                            Status = resp.data != null ? resp.data.status : 0,
                            Address = resp.data != null ? resp.data.address : "",
                            Fee = resp.data != null ? resp.data.fee : 0,
                            IsExpired = resp.data?.is_expired,
                            SiteCode = resp.data != null ? resp.data.site_code : "",
                            PricingStoreCode = resp.data != null ? resp.data.pricing_store_code : "",
                            VinIdEarn = resp.data != null ? resp.data.vin_id_earn : 0,
                            HasVatInvoice = resp.data != null ? resp.data.has_vat_invoice : false,
                            ListPayments = resp.data != null ? resp.data.payments.Select(a => new POSScanPayments
                            {
                                PaymentMethod = a.payment_method,
                                PaidAmount = a.paid_amount,
                                TransactionId = a.transaction_id
                            }).ToList() : new List<POSScanPayments>(),
                            ListItems = resp.data.items.Select(a => new POSScanItems
                            {
                                Id = a.id,
                                ArticleId = a.article_id,
                                ArticleName = a.article_name,
                                SoldPrice = a.sold_price,
                                Quantity = a.quantity,
                                UnitCode = a.unit_code,
                                ProductBarcode = a.product_barcode,
                                OriginBarcode = a.origin_barcode,
                                ItemType = a.item_type,
                                VatGroup = a.vat_group,
                                VatPercent = a.vat_percent,
                                NetPrice = a.net_price,
                                PromotionCode = a.product_barcode,
                                PromotionValue = a.promotion_value,
                                UnitQuantity = a.unit_quantity,
                                SaleQuantity = a.sale_quantity,
                                IsFreshfood = a.is_freshfood,
                                UnitSoldPrice = a.unit_sold_price,
                                TotalSoldPrice = a.total_sold_price

                            }).ToList(),
                            BillingInfo = resp.data.billing_info == null ? new POSBillInfo() : new POSBillInfo
                            {
                                CustomerName = resp.data.billing_info.customer_name,
                                Address = resp.data.billing_info.address,
                                Email = resp.data.billing_info.email,
                                CompanyName = resp.data.billing_info.company_name,
                                TaxCode = resp.data.billing_info.tax_code,
                                PhoneNumber = resp.data.billing_info.phone_number,
                                SiteCode = resp.data.billing_info.site_code
                            }
                        };

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = result,
                            Message = "Thành công",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        _modelLog.StatusVINID = false;
                        _logVID.WriteLogAPI(_modelLog);

                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = resp.meta.message,
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
                catch (Exception ex)
                {

                    _modelLog.StoreNo = "";
                    _modelLog.CardNumber = "";
                    _modelLog.RequestPOS = $"{linkAPIScanAndGo}{subUrl}";
                    _modelLog.RequestXML = $"{linkAPIScanAndGo}{subUrl}";
                    _modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                    _modelLog.StatusVINID = false;

                    _logVID.WriteLogAPI(_modelLog);

                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                    {
                        Data = null,
                        Message = "Lỗi hệ thống ScanAndGo API, Vui lòng liên hệ IT để được hỗ trợ",
                        Status = HttpStatusCode.Conflict
                    });
                }
            }
        }

        [HttpPut]
        [Route("vinid/ScanAndGo/UpdateStatusOrder")]
        public async Task<HttpResponseMessage> UpdateStatusOrder(StatusOrderRequest req)
        {
            //string subUrl = "https://api-uat.int.vinid.dev/vincart/v1/vcm/update-order-status?orderNo=" + req.ScanAndGoOrderNo + "&status=" + req.Status + "&collectedAmount=" + req.CollectedAmount;
            var subUrl = $"{linkAPIScanAndGo}/vincart/v1/vcm/update-order-status?orderNo={req.ScanAndGoOrderNo}&status={req.Status}&collectedAmount={req.CollectedAmount}";


            //var serialize = new JavaScriptSerializer();
            //serialize.MaxJsonLength = int.MaxValue;
            using (var client = new HttpClient())
            {
                _modelLog = new LogAPIModel { StoreNo = req.StoreNo, POSTerminal = req.POSTerminal, CardNumber = req.CardNumber, InvoiceNo = req.OrderNo, ActionType = "UpdateStatusOrder", RequestPOS = JsonConvert.SerializeObject(req), RequestXML = subUrl, ResponseXML = string.Empty, DateTime = DateTime.Now };

                try
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.DefaultRequestHeaders.Add("accept", "application/json");
                    var method = new HttpMethod("PATCH");
                    var request = new HttpRequestMessage(method, subUrl);
                    try
                    {
                        var response = await client.SendAsync(request);

                        var resultData = response.Content.ReadAsStringAsync();

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        //modelLog.ResponseXML = JsonConvert.SerializeObject(resultData.Result);
                        //logVID.WriteLogAPI(modelLog);

                        UpdateStatusOrderResponse resp = JsonConvert.DeserializeObject<UpdateStatusOrderResponse>(resultData.Result);

                        _modelLog.ResponseXML = JsonConvert.SerializeObject(resp);
                        _modelLog.ResponseTotal = milliseconds;


                        if (resp.meta.code == 200)
                        {
                            _modelLog.StatusVINID = true;
                            _logVID.WriteLogAPI(_modelLog);
                            /////////////////////////////////////////////////////////////////////
                            var award = resp.data.vin_id_earn;
                            modelTrans = new LogTransactionLoyaltyModel
                            {
                                CardNumber = req.CardNumber,
                                QRCode = resp.data.receipt_number,
                                InvoiceNo = string.IsNullOrEmpty(resp.data.reference_number) ? req.ScanAndGoOrderNo : resp.data.reference_number,//Nếu có tiêu điểm(req.SpendPoints>0) thì k update cột này
                                ScanAndGoOrderNo = req.ScanAndGoOrderNo,
                                ScanAndGoReferenceNumber = resp.data.reference_number,
                                AccessToken = "f96c49c7c69d07c9af7cdc6f66720f75",
                                StoreID = req.StoreNo,//son truyen
                                TerminalId = req.POSTerminal,//son truyen
                                ChannelCode = "SCAN&GO",
                                DateTime = DateTime.Now,
                                TransactionType = "SALE",
                                TransactionName = "Bán",
                                AmountOnOrderPOS = (double)req.CollectedAmount,//Son truyen
                                BillAmount = (double)req.CollectedAmount,
                                SpendPoints = req.SpendPoints,
                                RefundAmount = 0,
                                Operator = "+",
                                OrderNo = req.OrderNo,
                                OrigOrderNo = req.OrderNo,
                                OrigInvoiceNo = req.OrderNo,
                                OrigBillAmount = req.CollectedAmount,
                                Source = "SnG",
                                NettAmtOE = 0,
                                GrossTransactionAmountOE = resp.data.total_bill_amount,// lấy Cột total_bill_amount
                                AwardPointOE = award,
                                AwardAmountOE = award * 1000,
                                RedeemPointOE = req.SpendPoints,
                                RedeemAmountOE = req.SpendPoints * 1000,
                                PointPoolIDToRedeem = "P01",
                                IsOffline = false,//son truyen
                                ErrorCode = "",
                                ErrorMessage = "",
                                ResponseCode = "00"
                            };
                            _logVID.WriteLogTransaction(modelTrans);
                            ////////////////////////////////////////////////////////////////


                            var result = new StatusPOSResponse
                            {
                                ReceiptNumber = resp.data.receipt_number,
                                TransactionType = resp.data.transaction_type,
                                StoreCode = resp.data.store_code,
                                PosCode = resp.data.pos_code,
                                CashierCode = resp.data.pos_code,
                                MerchantId = resp.data.merchant_id,
                                TerminalId = resp.data.terminal_id,
                                CustomerName = resp.data.customer_name,
                                VinidCardNumber = resp.data.vinid_card_number,
                                VinidCSN = resp.data.vinid_csn,
                                TotalBillAmount = resp.data.total_bill_amount,
                                ReferenceNumber = resp.data.reference_number,
                                ExtraEarnByItems = resp.data.extra_earn_by_items,
                                ExtraEarnByCampaign = resp.data.extra_earn_by_campaign,
                                VinIdEarn = resp.data.vin_id_earn,
                                IsEarnSuccess = resp.data.is_earn_success,
                                OverQuota = resp.data.over_quota,
                                BillLines = (resp.data.bill_lines != null && resp.data.bill_lines.Count > 0) ? resp.data.bill_lines.Select(a => new StatusBillPOS
                                {
                                    RecordNo = a.record_no,
                                    Barcode = a.barcode,
                                    Article = a.article,
                                    ArticleName = a.article_name,
                                    UOM = a.uom,
                                    Quantity = a.quantity,
                                    SalePrice = a.sale_price,
                                    Amount = a.amount,
                                    DiscountAmount = a.discount_amount,
                                    LineAmount = a.line_amount,
                                    ExtraQuantityEarn = a.extra_quantity_earn,
                                    ExtraAmountEarn = a.extra_amount_earn,
                                    VCMUnitPrice = a.vcm_unit_price

                                }).ToList() : new List<StatusBillPOS>()
                            };

                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = result,
                                Message = "Thành công",
                                Status = HttpStatusCode.OK
                            });
                        }
                        else
                        {
                            _modelLog.StatusVINID = false;
                            _logVID.WriteLogAPI(_modelLog);
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = resp.meta.message,
                                Status = HttpStatusCode.BadRequest
                            });
                        }

                    }
                    catch (HttpRequestException ex)
                    {
                        _modelLog.RequestXML = subUrl;
                        _modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                        _modelLog.StatusVINID = false;
                        _logVID.WriteLogAPI(_modelLog);

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = "Lỗi hệ thống ScanAndGo, Vui lòng liên hệ IT để được hỗ trợ",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
                catch (Exception ex)
                {
                    _modelLog.RequestXML = subUrl;
                    _modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                    _modelLog.StatusVINID = false;
                    _logVID.WriteLogAPI(_modelLog);

                    return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                    {
                        Data = null,
                        Message = "Lỗi hệ thống ScanAndGo API, Vui lòng liên hệ IT để được hỗ trợ",
                        Status = HttpStatusCode.InternalServerError
                    });
                }
            }
        }

        [HttpPost]
        [Route("vinid/ExtraSales")]
        public HttpResponseMessage ExtraSales(ExtraSalePosRequestModel pos)
        {
            if (pos == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Dữ liệu đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.CardNumber))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "CardNumber đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.PosTerminal))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "POSTerminal đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.ReceiptNumber))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Đơn hàng đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrEmpty(pos.ReferenceNumber))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "ReferenceNumber đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (pos.TotalBillAmount <= 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "BillAmount nhỏ hơn 0",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (pos.BillLines.Count <= 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Đơn hàng này không có sản phẩm",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if ((pos.CardNumber.Substring(0, 4) == "8888" || pos.CardNumber.Substring(0, 5) == "66668") && pos.CardNumber.Length == 16)
            {
                return VINIDExtraSales(pos);
            }
            else
            {
                return CXExtraSales(pos);
            }

            //if (pos.CardNumber.Substring(0, 4) == "6868" || pos.CardNumber.Substring(0, 2) == "84")//CX
            //{
            //    return CXExtraSales(pos);
            //}
            //else
            //{
            //    return VINIDExtraSales(pos);
            //}

        }

        [HttpPost]
        [Route("vinid/ExtraRefund")]
        public HttpResponseMessage ExtraRefund(ExtraRefundPosRequestModel pos)
        {
            if (pos == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Dữ liệu đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.CardNumber))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "CardNumber đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.CustomerName))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "CustomerName đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.PosTerminal))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "POSTerminal đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.ReceiptNumber))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Đơn hàng đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrEmpty(pos.OriginalReceiptNumber))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Không tìm thấy đơn hàng gốc",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.ReferenceNumber))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "ReferenceNumber đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(pos.OriginalReferenceNumber))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "OriginalReferenceNumber đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (pos.TotalBillAmount <= 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "BillAmount nhỏ hơn 0",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if ((pos.CardNumber.Substring(0, 4) == "8888" || pos.CardNumber.Substring(0, 5) == "66668") && pos.CardNumber.Length == 16)
            {
                return VINIDExtraRefund(pos);
            }
            else
            {
                return CXExtraRefund(pos);
            }

            //if (pos.CardNumber.Substring(0, 4) == "6868" || pos.CardNumber.Substring(0, 2) == "84")//CX
            //{
            //    return CXExtraRefund(pos);
            //}
            //else
            //{
            //    return VINIDExtraRefund(pos);
            //}
        }

        [HttpPost]
        [Route("vinid/ScanAndGo/SnGRefund")] // Trả hàng scan and go
        public HttpResponseMessage SnGRefund(POSRefundRequestV2 req)
        {
            try
            {
                //var linkV2 = "https://api-uat.int.vinid.dev/vincart/v1/vcm/order-returns/{0}";
                //var linkRf = "https://api-uat.int.vinid.dev/vincart-vinmart-order/v1/vcm/order-returns/{0}";


                // var linkRf = linkAPIScanAndGo + "/vincart-vinmart-order/v1/vcm/order-returns/{0}";
                var linkRf = linkAPIScanAndGo + "/vincart/v1/vcm/order-returns/{0}";

                // var linkRf = $"{linkAPIScanAndGo}/vincart-vinmart-order/v1/vcm/order-returns/{0}";


                var userVINID = WebConfigurationManager.AppSettings["VINID_API_USER"];
                var passVINID = WebConfigurationManager.AppSettings["VINID_API_PASS"];

                if (req == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Dữ liệu đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(req.CardNumber))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "CardNumber đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(req.StoreNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Cửa hàng đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(req.POSTerminal))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "POSTerminal đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(req.BarcodeOrder))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Đơn hàng barcode đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var model = new VinIDRefundRequestV2
                {
                    return_no = req.ReturnNo,
                    return_date = DateTime.Now.ToString("ddMMyyyy"),
                    return_reason = "order_return",
                    note = "Hoàn do hủy/trả sản phẩm",
                    redeeming_point = req.RedeemingPoint,//TIÊU ĐIỂM
                    items = req.Items.Select(a => new ItemRefundOE
                    {
                        note = a.Note,
                        order_item_code = a.BarcodeItem,
                        order_item_id = a.OrderItemId,
                        return_sale_quantity = a.ReturnSaleQuantity,
                        return_unit_quantity = a.ReturnUnitQuantity

                    }).ToList()
                };
                var modelPos = new POSRefundRequestV2
                {
                    BarcodeOrder = req.BarcodeOrder,
                    OrigOrderNo = req.OrigOrderNo,
                    StoreNo = req.StoreNo,
                    POSTerminal = req.POSTerminal,
                    CardNumber = req.CardNumber,
                    ReturnNo = req.ReturnNo,
                    RedeemingPoint = req.RedeemingPoint,
                    Items = req.Items

                };
                string requestPOS = JsonConvert.SerializeObject(modelPos);

                var json = JsonConvert.SerializeObject(model);
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var client = new RestClient(string.Format(linkRf, req.BarcodeOrder));

                UtilsAPI.Info("SnG", $"Link SnG:{linkRf}");

                UtilsAPI.Info("SnG", $"Model:{json}");

                var request = new RestRequest(Method.Put.ToString());
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("accept", "application/json");
                request.AddParameter("undefined", json, ParameterType.RequestBody);
                request.Authenticator = new HttpBasicAuthenticator(userVINID, passVINID);

                UtilsAPI.Info("SnG", $"User:{userVINID}; Pass:{passVINID}");

                UtilsAPI.Info("SnG", $"Client:{JsonConvert.SerializeObject(client)}");

                var response = client.Execute<VinIDRefundRequestV2>(request);


                UtilsAPI.Info("SnG", $"response:{JsonConvert.SerializeObject(response)}");

                SnGRefundResponse resp = JsonConvert.DeserializeObject<SnGRefundResponse>(response.Content);

                _modelLog = new LogAPIModel { StoreNo = req.StoreNo, POSTerminal = req.POSTerminal, CardNumber = req.CardNumber, InvoiceNo = req.BarcodeOrder, ActionType = "SnG_Refund", RequestXML = json, RequestPOS = requestPOS, ResponseXML = JsonConvert.SerializeObject(resp), DateTime = DateTime.Now };
                _logVID.WriteLogAPI(_modelLog);

                if (resp.meta.code == 200)
                {
                    var OriginalInvoiceNo_BillAmount = _logVID.OrigInvoiceNo(req.OrigOrderNo);//lay tu don hang goc

                    var origAward = OriginalInvoiceNo_BillAmount.Split('|')[3];

                    var result = resp.data == null ? new SnGRefundDataPOS() : new SnGRefundDataPOS
                    {
                        VinIDEarn = ParseHelper.IntOrZero(origAward) - resp.data.vin_id_earn,//Tich
                        VinIDRedeem = (int)req.RedeemingPoint//Tiêu
                    };

                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = result,
                        Message = "Thành công",
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    if (resp.meta.code == 41207101)//
                    {
                        return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                        {
                            Data = null,
                            Message = "Tài khoản không đủ điểm để hoàn, Vui lòng báo KH nạp thêm điểm để có thể trả hàng",
                            Status = HttpStatusCode.Conflict // Chặn  
                        });
                    }

                    if (resp.meta.code == 81910022)//
                    {
                        return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                        {
                            Data = null,
                            Message = "Ko tiêu điểm VINID, vui lòng chọn hình thức TT khác",
                            Status = HttpStatusCode.Conflict
                        });
                    }

                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                    {
                        Data = null,
                        Message = resp.meta.message,
                        Status = HttpStatusCode.Conflict
                    });
                }
            }
            catch (Exception ex)
            {
                UtilsAPI.Info("SnG", $"Exception:{JsonConvert.SerializeObject(ex)}");
                _modelLog = new LogAPIModel { StoreNo = req.StoreNo, POSTerminal = req.POSTerminal, CardNumber = req.CardNumber, InvoiceNo = req.BarcodeOrder, ActionType = "SnG_Refund", RequestPOS = JsonConvert.SerializeObject(ex), RequestXML = JsonConvert.SerializeObject(ex), ResponseXML = JsonConvert.SerializeObject(ex), DateTime = DateTime.Now };
                _logVID.WriteLogAPI(_modelLog);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = "Lỗi hệ thống ScanAndGo API, Vui lòng liên hệ IT để được hỗ trợ",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("vinid/ScanAndGo/SnGTopup")]
        public HttpResponseMessage SnGTopup(POSTopupRequest req)
        {
            try
            {

                //var linkV2 = "https://api-uat.int.vinid.dev/vincart-vinmart-order/v2/vcm/{0}/topup";
                var linkV2 = linkAPIScanAndGo + "/vincart-vinmart-order/v2/vcm/{0}/topup";

                var userVINID = WebConfigurationManager.AppSettings["VINID_API_USER"];
                var passVINID = WebConfigurationManager.AppSettings["VINID_API_PASS"];

                //long timeUni = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                //Int64 timeUnix = timeUni;
                if (req == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Dữ liệu đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(req.CardNumber))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "CardNumber đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(req.StoreNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Cửa hàng đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(req.POSTerminal))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "POSTerminal đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(req.BarcodeOrder))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Đơn hàng barcode đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (req.TopupData.Count() == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Không có dữ liệu data topup",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var model = new TopupVINIDRequest
                {
                    topup_no = req.TopupNo,
                    topup_data = req.TopupData.Select(a => new TopupTypeVINIDRequest
                    {
                        amount = a.Amount,
                        type = a.Type
                    }).ToList()
                };
                var json = JsonConvert.SerializeObject(model);

                string requestPOS = JsonConvert.SerializeObject(req);

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var client = new RestClient(string.Format(linkV2, req.BarcodeOrder));
                var request = new RestRequest(Method.Post.ToString());
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("accept", "application/json");
                request.AddParameter("undefined", json, ParameterType.RequestBody);
                request.Authenticator = new HttpBasicAuthenticator(userVINID, passVINID);
                var response = client.Execute<TopupVINIDRequest>(request);
                SnGTopupResponse resp = JsonConvert.DeserializeObject<SnGTopupResponse>(response.Content);

                _modelLog = new LogAPIModel { StoreNo = req.StoreNo, POSTerminal = req.POSTerminal, CardNumber = req.CardNumber, InvoiceNo = req.BarcodeOrder, ActionType = "SnG_Topup", RequestPOS = requestPOS, RequestXML = json, ResponseXML = JsonConvert.SerializeObject(resp), DateTime = DateTime.Now };
                _logVID.WriteLogAPI(_modelLog);

                if (resp.meta.code == 200)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = null,
                        Message = "Thành công",
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = resp.meta.message,
                        Status = HttpStatusCode.BadRequest
                    });
                }
            }
            catch (Exception ex)
            {
                _modelLog = new LogAPIModel { StoreNo = req.StoreNo, POSTerminal = req.POSTerminal, CardNumber = req.CardNumber, InvoiceNo = req.BarcodeOrder, ActionType = "SnG_Topup", RequestPOS = ex.Message, RequestXML = ex.Message, ResponseXML = ex.Message, DateTime = DateTime.Now };
                _logVID.WriteLogAPI(_modelLog);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = "Lỗi hệ thống ScanAndGo API, Vui lòng liên hệ IT để được hỗ trợ",
                    Status = HttpStatusCode.InternalServerError
                });
            }

        }

        [HttpGet]
        [Route("api/vinid/TransactionEnquiry")]
        public HttpResponseMessage TransactionEnquiry(string MemberCard, string PageNo, string TotalRecordPerPage, string StartDate, string EndDate)
        {
            try
            {
                if (cache[Constants.LoginResponse] != null)
                {
                    loginCache = cache[Constants.LoginResponse] as OperatorLoginRs;
                }
                else
                {
                    Login(configMerchantId, configTerminalId);
                    loginCache = cache[Constants.LoginResponse] as OperatorLoginRs;
                }
                // Login("", "");
                var customerIdentifier = new CustomerIdentifier
                {
                    IDType = "CARD",
                    ID = MemberCard
                };
                var transactionEnquiryRq = new TransactionEnquiryRq
                {
                    RqHeader = new RqHeader
                    {
                        AccessToken = loginCache.RsHeader.AccessToken,
                        Date = DateTime.Now.ToString("yyyyMMdd"),
                        Time = DateTime.Now.ToString("HHmmss"),
                        TimeZone = configTimeZone,
                        MessageType = Constants.MessageType.BALANCE_ENQUIRY,
                        VersionNo = configVersionNo,
                        AppVersion = configAppVersion,
                        MerchantId = configMerchantId,
                        TerminalId = configTerminalId,
                        ChannelId = "POS",
                        LastHostRefNo = "",
                        OTP = ""
                    },
                    CustomerIdentifier = customerIdentifier,
                    PageNo = PageNo,
                    TotalRecordPerPage = TotalRecordPerPage,
                    StartDate = StartDate ?? "",
                    EndDate = EndDate ?? ""
                };
                var xmlRq = UtilsAPI.Serialize(transactionEnquiryRq);
                var callTran = wsVinID.processRequest(xmlRq, "", "", "");
                var transactionRs = UtilsAPI.Deserialize<TransactionEnquiryRs>(callTran);
                if (transactionRs.RsHeader.ResponseCode.Equals("00"))
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = transactionRs,
                        Message = "Thành công",
                        Status = HttpStatusCode.OK
                    });
                }

                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = transactionRs.RsHeader.ErrorMessage,
                    Message = null,
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


        //function
        public OperatorLoginRs Login(string MerchantId, string TerminalId)
        {
            var rs = new OperatorLoginRs();
            try
            {
                var login = new LoginWsVinIDModel
                {
                    Action = "LGN",
                    Mode = "O",
                    Password = configPassword
                };
                var customerIdentifier = new CustomerIdentifier
                {
                    IDType = "OPERATOR",
                    ID = configOperatorId
                };

                var operatorLoginRq = new OperatorLoginRq
                {
                    RqHeader = new RqHeader
                    {
                        Date = DateTime.Now.ToString("yyyyMMdd"),
                        Time = DateTime.Now.ToString("HHmmss"),
                        TimeZone = configTimeZone,
                        MessageType = Constants.MessageType.LOGIN,
                        VersionNo = configVersionNo,
                        AppVersion = configAppVersion,
                        MerchantId = MerchantId,
                        TerminalId = TerminalId,
                        ChannelId = "POS",
                        LastHostRefNo = "",
                        OTP = ""
                    },
                    Login = login,
                    CustomerIdentifier = customerIdentifier
                };

                var xmlRq = UtilsAPI.Serialize(operatorLoginRq);
                var xmlRs = wsVinID.processRequest(xmlRq, "", "", "");
                rs = UtilsAPI.Deserialize<OperatorLoginRs>(xmlRs);
                cache.Insert(Constants.LoginResponse, new OperatorLoginRs(), null, DateTime.Now.AddMonths(2), Cache.NoSlidingExpiration);
            }
            catch 
            {
                throw;
            }
            return new OperatorLoginRs();
        }
        public HttpResponseMessage VINIDGetInfoMember(string numberCard, string posID, string storeNo, bool isLog)
        {
            var posStoreNo = storeNo;
            var posTerminal = posID;
            numberCard = numberCard.Trim();

            var regexItem = new Regex("^[a-zA-Z0-9 ]*$");
            if (!regexItem.IsMatch(numberCard))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã thẻ VINID hoặc QR VINPAY không hợp lệ, vui lòng scan lại thông tin",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (numberCard.Substring(0, 4) != "8888" && numberCard.Substring(0, 5) != "66668" && numberCard.Substring(0, 1) != "P")
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "Mã thẻ VINID hoặc QR VINPAY không hợp lệ, vui lòng scan lại thông tin",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (numberCard.Substring(0, 4) == "8888" || numberCard.Substring(0, 5) == "66668")
            {
                var checkVINID = LoyaltyHelper.IsOffVinID(_memoryCacheService, "GetInfo");
                if (checkVINID.Item1)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = checkVINID.Item2,
                        Status = HttpStatusCode.BadRequest
                    });
                }
            }

            if (string.IsNullOrEmpty(storeNo))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "StoreNo đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(posID))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "PosID đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            //var storeMapping = _logVID.StoreMapping(storeNo, posID);
            var storeMapping = _memoryCacheService.GetStoreMappingVinID(storeNo, posID);
            if (storeMapping != null)
            {
                posID = storeMapping?.OldTerminalID;
                storeNo = storeMapping?.OldStoreID;
            }

            InfoMemberRequest model = new InfoMemberRequest
            {
                code_value = numberCard,
                merchant_id = storeNo,
                terminal_id = posID,
                date = DateTime.Now.ToString("yyyyMMdd"),
                time = DateTime.Now.ToString("HHmmss"),
                time_zone = "GMT+07:00"
            };
            POSFunctionRequest modelPos = new POSFunctionRequest
            {
                numberCard = numberCard,
                storeNo = posStoreNo,
                posID = posTerminal
            };
            string json = JsonConvert.SerializeObject(model);
            string requestPOS = JsonConvert.SerializeObject(modelPos);
            try
            {
                string request_id = Guid.NewGuid().ToString();
                string subUrl = "/internal/checkout/v1/get-loyalty-information";
                _kibana.LogRequest(linkAPI_VINID, posID, subUrl, json);
                var st1 = new Stopwatch();
                st1.Start();
                long milliseconds = 0;               
                var serialize = new JavaScriptSerializer
                {
                    MaxJsonLength = int.MaxValue
                };

                using (var client = new HttpClient())
                {
                    try
                    {
                        
                        client.Timeout = TimeSpan.FromMinutes(1);
                        client.BaseAddress = new Uri(linkAPI_VINID);
                        client.DefaultRequestHeaders.Add("X-Api-Key", API_KEY);//vin_mart_api_key
                        client.DefaultRequestHeaders.Add("X-Api-Secret", API_SECRET);//vin_mart_secret_key
                        client.DefaultRequestHeaders.Add("X-Lang", "vn");
                        client.DefaultRequestHeaders.Add("X-Request-ID", request_id);
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        ServicePointManager.DnsRefreshTimeout = 0;                       
                        var response = client.PostAsync(subUrl, new StringContent(json, Encoding.UTF8, "application/json")).Result;
                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        _kibana.LogResponse(linkAPI_VINID, posID, milliseconds, subUrl, numberCard + " ==> " + JsonConvert.SerializeObject(response.Content.ReadAsStringAsync()));
                        if (response.IsSuccessStatusCode)
                        {
                            var content = response.Content.ReadAsStringAsync();
                            InfoMemberResponse resp = JsonConvert.DeserializeObject<InfoMemberResponse>(content.Result);
                            if (resp.meta.code == 200)
                            {
                                if (isLog == true)
                                {
                                    var statusVINID = true;
                                    _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.code_value, ActionType = "GetInfoMember", RequestPOS = requestPOS, RequestXML = json, ResponseXML = serialize.Serialize(resp), ResponseTotal = milliseconds, StatusVINID = statusVINID, DateTime = DateTime.Now };
                                    _logVID.WriteLogAPI(_modelLog);
                                }

                                var memberPoint = (resp.data.oe_info.member_balance.pool == null || string.IsNullOrEmpty(resp.data.oe_info.member_balance.pool.redeemable_pool_units)) ? 0 : (int)Convert.ToDecimal(resp.data.oe_info.member_balance.pool.redeemable_pool_units);
                                var totalPoint = (resp.data.oe_info.member_balance.pool == null || string.IsNullOrEmpty(resp.data.oe_info.member_balance.pool.total_pool_units)) ? 0 : (int)Convert.ToDecimal(resp.data.oe_info.member_balance.pool.total_pool_units);

                                ///string cardnum = resp.data.oe_info.customer_identifier.id;//6666
                                //var account = resp.data.oe_info.member_accounts;
                                var cardnum = "";
                                var qrCode = "";
                                var cardVirtual = "";

                                var listAccount = resp.data.oe_info.member_accounts.OrderByDescending(d => d.account_no).ToList();
                                foreach (var acc in listAccount)
                                {
                                    if (acc.account_no.Substring(0, 4) == "8888" || acc.account_no.Substring(0, 5) == "66668")
                                    {
                                        //if (acc.AccountStatus == "A" && acc.BlockCode == "0" && (acc.AccountType == "200" || acc.AccountType == "101"))
                                        if (acc.account_status == "A" && acc.block_code == "0")
                                        {
                                            cardnum = acc.account_no;

                                            break;
                                        }
                                    }
                                }

                                if (string.IsNullOrEmpty(cardnum))
                                {
                                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Không tồn tại tài khoản hoặc thẻ đã bị khóa, vui lòng liên hệ DVKH để kiểm tra",
                                        Status = HttpStatusCode.BadRequest
                                    });
                                }

                                if (numberCard.Substring(0, 1) == "P")
                                {
                                    qrCode = numberCard;
                                }

                                if (numberCard.Substring(0, 5) == "66668")
                                {
                                    cardVirtual = numberCard;
                                }

                                var listDetailContact = resp.data.oe_info.member_profile.contact_details;
                                var phoneNumber = "";
                                if (listDetailContact != null && listDetailContact.Count > 0)
                                {
                                    phoneNumber = listDetailContact.FirstOrDefault(d => d.mobile_number != "").mobile_number;
                                }

                                InfoMemberModel result = new InfoMemberModel
                                {
                                    CardNumber = cardnum,
                                    VirtualCard = string.IsNullOrEmpty(cardVirtual) ? cardnum : cardVirtual,
                                    CMND = resp.data.oe_info.member_profile.client_id_1,
                                    MemberName = resp.data.oe_info.member_profile.full_name,
                                    CardLevel = resp.data.oe_info.member_accounts.FirstOrDefault().account_type,
                                    MemberCSN = resp.data.oe_info.member_profile.csn,
                                    PhoneNumber = phoneNumber,// resp.data.oe_info.member_profile.customer_identifier.id,
                                    OtherInfo = "",
                                    QRCode = qrCode,
                                    MemberPoint = memberPoint,
                                    TotalPoint = totalPoint,
                                    ExtraPoint = resp.data.oe_info.has_extra_point,
                                    CurrentRate = 10,
                                    IsOfflineVinID = false,
                                    IsShowMessage = false,
                                    IsRedeem = true,
                                    Status = "Hoạt động",
                                    System = "OE"
                                };
                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Data = result,
                                    Message = "Thành công",
                                    Status = HttpStatusCode.OK
                                });
                            }
                            else
                            {
                                if (isLog == true)
                                {
                                    var statusVINID = false;
                                    _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.code_value, ActionType = "GetInfoMember", RequestPOS = requestPOS, RequestXML = json, ResponseXML = serialize.Serialize(resp), ResponseTotal = milliseconds, StatusVINID = statusVINID, DateTime = DateTime.Now };
                                    _logVID.WriteLogAPI(_modelLog);
                                }

                                if (resp.meta.code == 81910018)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    {
                                        Data = null,
                                        Message = $"Thẻ VINID {numberCard} chưa được đăng ký",
                                        Status = HttpStatusCode.Conflict //Chặn
                                    });
                                }

                                #region validate VinID
                                if (resp.meta.code == 81910007)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    {
                                        Data = null,
                                        Message = $"Thẻ VINID {numberCard} không tồn tại",
                                        Status = HttpStatusCode.Conflict //Chặn
                                    });
                                }
                                if (resp.meta.code == 81910010)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    {
                                        Data = null,
                                        Message = $"Thẻ VINID {numberCard} đã hết hạn. Vui lòng scan lại thẻ VINID",
                                        Status = HttpStatusCode.Conflict //Chặn
                                    });
                                }
                                if (resp.meta.code == 81910011)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    {
                                        Data = null,
                                        Message = $"Tài khoản VINID {numberCard} của khách hàng đã bị khóa",
                                        Status = HttpStatusCode.Conflict //Chặn
                                    });
                                }
                                ///////

                                if (resp.meta.code == 81910008)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Unused, new ResultResponse
                                    {
                                        Data = new InfoMemberModel { Status = "Bị khóa" },
                                        Message = $"Thẻ VINID {numberCard} bị khóa",
                                        Status = HttpStatusCode.Unused //Offline
                                    });
                                }
                                if (resp.meta.code == 81910009)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Unused, new ResultResponse
                                    {
                                        Data = new InfoMemberModel { Status = "Chưa kích hoạt" },
                                        Message = $"Thẻ VINID {numberCard} chưa được kích hoạt",
                                        Status = HttpStatusCode.Unused //Offline
                                    });
                                }
                                if (resp.meta.code == 81910012)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Unused, new ResultResponse
                                    {
                                        Data = new InfoMemberModel { Status = "Chưa kích hoạt" },
                                        Message = $"Thẻ VINID {numberCard} chưa được kích hoạt",
                                        Status = HttpStatusCode.Unused //Offline
                                    });
                                }

                                if (resp.meta.code == 81910005)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                                    {
                                        Data = new InfoMemberModel { Status = "Hoạt động" },
                                        Message = "Hiện tại hệ thống VINID không có phản hồi",
                                        Status = HttpStatusCode.ServiceUnavailable //Offline
                                    });
                                }
                                if (resp.meta.code == 81910001)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                                    {
                                        Data = new InfoMemberModel { Status = "Hoạt động" },
                                        Message = "Hiện tại hệ thống VINID không hoạt động",
                                        Status = HttpStatusCode.ServiceUnavailable //Offline
                                    });
                                }

                                if (resp.meta.code == 81910015)// Invalid Customer ID Error
                                {
                                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                    {
                                        Data = null,
                                        Message = $"Tài khoản VINID { numberCard } của khách hàng không hợp lệ",
                                        Status = HttpStatusCode.BadRequest
                                    });
                                }


                                #endregion
                                var resultMessage = resp.meta.message;
                                var spMessage = resp.meta.message.Split('\t');

                                if (spMessage.Length > 1)
                                    resultMessage = spMessage[1] == "" ? spMessage[0] : spMessage[1];

                                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                                {
                                    Data = new InfoMemberModel { Status = "Hoạt động" },
                                    Message = resultMessage,
                                    Status = HttpStatusCode.ServiceUnavailable //Offline
                                });
                            }
                        }
                        else
                        {
                            if (isLog == true)
                            {
                                _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.code_value, ActionType = "GetInfoMember", RequestPOS = requestPOS, RequestXML = json, ResponseXML = JsonConvert.SerializeObject(response), ResponseTotal = milliseconds, StatusVINID = false, DateTime = DateTime.Now };
                                _logVID.WriteLogAPI(_modelLog);
                            }

                            if (response.StatusCode.ToString() == "522")//TimeOut
                            {
                                InfoMemberModel result = new InfoMemberModel
                                {
                                    CardNumber = numberCard,
                                    CMND = string.Empty,
                                    MemberName = string.Empty,
                                    CardLevel = string.Empty,
                                    MemberCSN = string.Empty,
                                    PhoneNumber = string.Empty,
                                    OtherInfo = "",
                                    //QRCode = numberCard.Substring(0, 4) != "8888" && numberCard.Substring(0, 4) != "6666" && numberCard.Substring(0, 4) != "9999" ? numberCard : "",
                                    QRCode = numberCard,
                                    MemberPoint = 0,
                                    TotalPoint = 0,
                                    CurrentRate = 10,
                                    ExtraPoint = false,
                                    IsRedeem = true,
                                    IsOfflineVinID = true,
                                    IsShowMessage = false,
                                    Status = "",
                                    System = "OE"
                                };
                                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                                {
                                    Data = result,
                                    Message = "Hiện tại hệ thống VINID đang không hoạt động",
                                    Status = HttpStatusCode.ServiceUnavailable //Offline
                                });
                            }

                            return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                            {
                                Data = new InfoMemberModel { Status = "Hoạt động" },
                                Message = "Lỗi hệ thống VINID, Vui lòng liên hệ IT để được hỗ trợ",
                                Status = HttpStatusCode.ServiceUnavailable //Offline
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        InfoMemberModel result = new InfoMemberModel
                        {
                            CardNumber = numberCard,
                            CMND = string.Empty,
                            MemberName = string.Empty,
                            CardLevel = string.Empty,
                            MemberCSN = string.Empty,
                            PhoneNumber = string.Empty,
                            OtherInfo = "",
                            //QRCode = numberCard.Substring(0, 4) != "8888" && numberCard.Substring(0, 4) != "6666" && numberCard.Substring(0, 4) != "9999" ? numberCard : "",
                            QRCode = numberCard,
                            MemberPoint = 0,
                            TotalPoint = 0,
                            CurrentRate = 10,
                            ExtraPoint = false,
                            IsRedeem = true,
                            IsOfflineVinID = true,
                            IsShowMessage = false,
                            Status = "",
                            System = "OE"
                        };

                        if (isLog == true)
                        {
                            _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.code_value, ActionType = "GetInfoMember", RequestPOS = requestPOS, RequestXML = json, ResponseXML = JsonConvert.SerializeObject(ex), StatusVINID = false, DateTime = DateTime.Now };
                            _logVID.WriteLogAPI(_modelLog);
                        }

                        return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                        {
                            Data = result,
                            Message = "Hiện tại hệ thống VINID đang không hoạt động",
                            Status = HttpStatusCode.ServiceUnavailable //Offline
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (isLog == true)
                {
                    _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.code_value, ActionType = "GetInfoMember", RequestPOS = requestPOS, RequestXML = json, ResponseXML = JsonConvert.SerializeObject(ex), StatusVINID = false, DateTime = DateTime.Now };
                    _logVID.WriteLogAPI(_modelLog);
                }

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Data = new InfoMemberModel { Status = "Hoạt động" },
                    Message = "Lỗi hệ thống VINID API, Vui lòng liên hệ IT để được hỗ trợ",
                    Status = HttpStatusCode.ServiceUnavailable //Offline
                });
            }

        }
        public HttpResponseMessage VINIDSales(string QRCode, string CardNumber, string MerchantId, string TerminalId, string InvoiceNo, int SpendPoints, decimal BillAmount, string OrderNo, bool IsOffline, decimal OrderAmount, string VirtualCard, bool CallOffline = false, List<amountToEarn> listAmountEarn = null)
        {

            var posStoreNo = MerchantId;
            var posTerminal = TerminalId;
            var storeMapping = _logVID.StoreMapping(MerchantId, TerminalId);
            if (storeMapping != null)
            {
                TerminalId = storeMapping?.OldTerminalID;
                MerchantId = storeMapping?.OldStoreID;
            }

            //check tích điểm
            var checkEarnVINID = LoyaltyHelper.IsOffVinID(_memoryCacheService, "EarnPoint");
            if (checkEarnVINID.Item1)
            {
                BillAmount = 0;
                OrderAmount = 0;
            }

            //check tiêu điểm
            var checkSpendVINID = LoyaltyHelper.IsOffVinID(_memoryCacheService, "SpendPoints");
            if (checkSpendVINID.Item1)
            {
                SpendPoints = 0;
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Data = null,
                    Message = checkSpendVINID.Item2,
                    Status = HttpStatusCode.ServiceUnavailable // Offline
                });

            }

            //////////////////////
            var channelcode = "VINMART";
            Random rnd = new Random();
            var dateTime = DateTime.Now.ToString("yyMMddHHmmssff");
            var transaction_ref_number = InvoiceNo + "_" + dateTime;
            var description = "Sale Points";
            string plainTextData = "|" + CardNumber + "|" + channelcode + "|" + MerchantId + "|" + TerminalId + "|" + transaction_ref_number + "|" + InvoiceNo + "|" + BillAmount + "|" + SpendPoints;
            string signature = VINIDHelper.VinPaySignature(plainTextData);
            SalesModelRequest model = new SalesModelRequest
            {
                transaction_ref_number = transaction_ref_number,
                description = description,
                code_value = CardNumber,
                date = DateTime.Now.ToString("yyyyMMdd"),
                time = DateTime.Now.ToString("HHmmss"),
                time_zone = "GMT+07:00",
                merchant_id = MerchantId,
                terminal_id = TerminalId,
                channel_code = channelcode,
                invoice_no = InvoiceNo,
                spend_points = SpendPoints.ToString(),
                bill_amount = BillAmount.ToString(),
                signature = signature
            };
            var modelPos = new POSFunctionSaleRequest
            {
                QRCode = QRCode,
                CardNumber = CardNumber,
                MerchantId = posStoreNo,
                TerminalId = posTerminal,
                InvoiceNo = InvoiceNo,
                SpendPoints = SpendPoints,
                BillAmount = BillAmount,//Đơn hàng ScanAndGo có tiêu điểm VINID dưới BLUEPOS: BillAmount=0
                OrderNo = OrderNo,
                IsOffline = IsOffline,
                OrderAmount = OrderAmount,
                VirtualCard = VirtualCard,
                ListAmountEarn = (listAmountEarn != null && listAmountEarn.Count > 0) ? listAmountEarn.Select(a => new POSAmountToEarn { LoyaltyMerchantId = a.loyaltyMerchantId, Amount = a.amount }).ToList() : null

            };
            string requestPOS = JsonConvert.SerializeObject(modelPos);
            try
            {
                var st1 = new Stopwatch();
                st1.Start();
                long milliseconds = 0;

                var serialize = new JavaScriptSerializer
                {
                    MaxJsonLength = int.MaxValue
                };
                string json = serialize.Serialize(model);
                string subUrl = "/internal/checkout/v1/sale-loyalty-points";
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPI_VINID);
                        client.DefaultRequestHeaders.Add("X-Api-Key", API_KEY);
                        client.DefaultRequestHeaders.Add("X-Api-Secret", API_SECRET);
                        client.DefaultRequestHeaders.Add("X-Lang", "vn");
                        client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());

                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                        var response = client.PostAsync(subUrl, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        if (response.IsSuccessStatusCode)
                        {
                            var content = response.Content.ReadAsStringAsync();
                            SalesResponse resp = JsonConvert.DeserializeObject<SalesResponse>(content.Result);

                            _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = InvoiceNo, ActionType = "Sales", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = serialize.Serialize(resp), ResponseTotal = milliseconds, CallOffline = CallOffline, DateTime = DateTime.Now };

                            if (resp.meta.code == 200)
                            {
                                _modelLog.StatusVINID = true;
                                //Task.Run(() => _logVID.WriteLogAPI(_modelLog));

                                var cardNumber = resp.data.customer_identifier.id;
                                var qrCode = QRCode;
                                var invoiceNo = InvoiceNo;
                                var accessToken = resp.data.rs_header.access_token;
                                var channelCode = resp.data.rs_header.channel_id;
                                var gross_transaction_amount = resp.data.transaction.gross_transaction_amount;
                                var nett_amt = resp.data.transaction.nett_amt;
                                var response_code = resp.data.rs_header.response_code;
                                var error_code = resp.data.rs_header.error_code;
                                var error_message = resp.data.rs_header.error_message;

                                var pool_units = resp.data.member_balance.awards.Count > 0 ? resp.data.member_balance.awards.Sum(item => Convert.ToDouble(item.pool_units)) : 0;
                                //var redeem_units = resp.data.member_balance.redeems.Count() == 0 ? 0 : resp.data.member_balance.redeems.Sum(item => Convert.ToDouble(item.redeem_units));
                                var redeem_units = resp.data.redeem_pool_list.Count() == 0 ? 0 : resp.data.redeem_pool_list.Sum(item => Convert.ToDouble(item.pool_units_to_redeem));

                                var listExtraEarnByCampaign = new List<PointEarnCampaint>();
                                if (listAmountEarn != null && listAmountEarn.Count > 0)
                                {

                                    listExtraEarnByCampaign = listAmountEarn.Select(a => new PointEarnCampaint
                                    {
                                        LoyaltyMerchantId = "VCM",
                                        Amount = a.amount,
                                        EarnedPoints = pool_units

                                    }).ToList();
                                }

                                PointModel result = new PointModel
                                {
                                    PointEarn = (int)pool_units,//Tích điểm
                                    PointRedeem = (int)redeem_units,//Tiểu điểm
                                    CurrentRate = 10,
                                    IsOfflineVinID = false,
                                    ExtraEarnByCampaign = listExtraEarnByCampaign
                                };

                                modelTrans = new LogTransactionLoyaltyModel
                                {
                                    CardNumber = CardNumber,
                                    QRCode = qrCode,
                                    VirtualCard = VirtualCard,
                                    InvoiceNo = invoiceNo,
                                    AccessToken = accessToken,
                                    StoreID = posStoreNo,//son truyen
                                    TerminalId = posTerminal,//son truyen
                                    ChannelCode = channelCode,
                                    DateTime = DateTime.Now,
                                    TransactionType = "SALE",
                                    TransactionName = "Bán",
                                    AmountOnOrderPOS = (double)OrderAmount,//Son truyen
                                    BillAmount = (double)BillAmount,
                                    SpendPoints = (double)SpendPoints,
                                    RefundAmount = 0,
                                    Operator = "+",
                                    OrderNo = OrderNo,
                                    OrigOrderNo = OrderNo,
                                    OrigInvoiceNo = invoiceNo,
                                    OrigBillAmount = (double)BillAmount,
                                    Source = "POS",
                                    NettAmtOE = MathHelper.IsNumber(nett_amt),
                                    GrossTransactionAmountOE = MathHelper.IsNumber(gross_transaction_amount),
                                    AwardPointOE = pool_units,
                                    AwardAmountOE = pool_units * 10,
                                    RedeemPointOE = redeem_units,
                                    RedeemAmountOE = redeem_units * 10,
                                    PointPoolIDToRedeem = "P01",
                                    IsOffline = IsOffline,//son truyen
                                    ErrorCode = error_code,
                                    ErrorMessage = error_message,
                                    ResponseCode = response_code
                                };

                                Task.Run(() => _logVID.WriteLogTransaction(modelTrans));

                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Data = result,
                                    Message = "Thành công",
                                    Status = HttpStatusCode.OK
                                });
                            }
                            else
                            {
                                _modelLog.StatusVINID = false;
                                //Task.Run(() => _logVID.WriteLogAPI(_modelLog));

                                #region validate VinID
                                if (resp.meta.code == 81910007)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Thẻ VINID không tồn tại",
                                        Status = HttpStatusCode.Conflict // Chặn  
                                    });
                                }
                                if (resp.meta.code == 81910010)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Thẻ VINID đã hết hạn. Vui lòng scan lại thẻ VINID",
                                        Status = HttpStatusCode.Conflict // Chặn  
                                    });
                                }
                                if (resp.meta.code == 81910011)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Tài khoản VINID của khách hàng đã bị khóa",
                                        Status = HttpStatusCode.Conflict // Chặn  
                                    });
                                }
                                if (resp.meta.code == 81910021)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Vui lòng kiểm tra lại số điểm VINID cần tiêu phải chia hết cho 100",
                                        Status = HttpStatusCode.BadRequest // Chặn  
                                    });
                                }
                                if (resp.meta.code == 4101014)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Mã thẻ không hợp lệ hoặc mã thanh toán đã hết hạn, vui lòng scan lại thẻ VINID hoặc QR VINPAY",
                                        Status = HttpStatusCode.BadRequest // Chặn  
                                    });
                                }
                                ///////

                                if (resp.meta.code == 81910008)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Unused, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Thẻ VINID bị khóa",
                                        Status = HttpStatusCode.Unused // Offline  
                                    });
                                }
                                if (resp.meta.code == 81910009)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Unused, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Thẻ VINID chưa được kích hoạt",
                                        Status = HttpStatusCode.Unused // Offline
                                    });
                                }
                                if (resp.meta.code == 81910012)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Unused, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Thẻ VINID chưa được kích hoạt",
                                        Status = HttpStatusCode.Unused // Offline
                                    });
                                }

                                if (resp.meta.code == 81910005)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Hiện tại hệ thống VINID không có phản hồi",
                                        Status = HttpStatusCode.ServiceUnavailable // Offline
                                    });
                                }

                                if (resp.meta.code == 81910001)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Hiện tại hệ thống VINID không hoạt động",
                                        Status = HttpStatusCode.ServiceUnavailable // Offline
                                    });
                                }

                                if (resp.meta.code == 81910022)//
                                {
                                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    {
                                        Data = null,
                                        Message = "Ko tiêu điểm VINID, vui lòng chọn hình thức TT khác",
                                        Status = HttpStatusCode.Conflict
                                    });
                                }

                                #endregion

                                var resultMessage = resp.meta.message;
                                var spMessage = resp.meta.message.Split('\t');
                                if (spMessage.Length > 1)
                                    resultMessage = spMessage[1] == "" ? spMessage[0] : spMessage[1];
                                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                                {
                                    Data = null,
                                    Message = resultMessage,
                                    Status = HttpStatusCode.ServiceUnavailable // Offline
                                });
                            }
                        }
                        else
                        {
                            _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "Sales", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = response.ToString(), ResponseTotal = milliseconds, StatusVINID = false, DateTime = DateTime.Now };
                            _logVID.WriteLogAPI(_modelLog);

                            if (response.StatusCode.ToString() == "522")//TimeOut
                            {
                                var calAwad = _logVID.CalAwardOffline((double)OrderAmount);
                                int tichDiem = calAwad == null ? 0 : (int)(Math.Round(((double)calAwad.Percent * (double)OrderAmount) / 1000, 0));//Tích điểm
                                var listExtraEarnByCampaign = new List<PointEarnCampaint>();
                                if (listAmountEarn != null && listAmountEarn.Count > 0)
                                {

                                    listExtraEarnByCampaign = listAmountEarn.Select(a => new PointEarnCampaint
                                    {
                                        LoyaltyMerchantId = "VCM",
                                        Amount = a.amount,
                                        EarnedPoints = tichDiem

                                    }).ToList();
                                }
                                PointModel result = new PointModel
                                {
                                    PointEarn = tichDiem,
                                    PointRedeem = 0,//Tiêu điểm
                                    CurrentRate = 10,
                                    IsOfflineVinID = true,
                                    ExtraEarnByCampaign = listExtraEarnByCampaign
                                };
                                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                                {
                                    Data = result,
                                    Message = "Hiện tại hệ thống VINID đang không hoạt động",
                                    Status = HttpStatusCode.ServiceUnavailable // Offline
                                });
                            }

                            return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                            {
                                Data = null,
                                Message = "Lỗi hệ thống VINID, Vui lòng liên hệ IT để được hỗ trợ",
                                Status = HttpStatusCode.ServiceUnavailable // Offline
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        var calAwad = _logVID.CalAwardOffline((double)BillAmount);
                        int tichDiem = calAwad == null ? 0 : (int)(Math.Round(((double)calAwad.Percent * (double)OrderAmount) / 1000, 0));//Tích điểm
                        var listExtraEarnByCampaign = new List<PointEarnCampaint>();
                        if (listAmountEarn != null && listAmountEarn.Count > 0)
                        {

                            listExtraEarnByCampaign = listAmountEarn.Select(a => new PointEarnCampaint
                            {
                                LoyaltyMerchantId = "VCM",
                                Amount = a.amount,
                                EarnedPoints = tichDiem

                            }).ToList();
                        }
                        PointModel result = new PointModel
                        {
                            PointEarn = calAwad == null ? 0 : (int)(Math.Round(((double)calAwad.Percent * (double)BillAmount) / 1000, 0)),//Tích điểm
                            PointRedeem = 0,//Tiêu điểm
                            CurrentRate = 10,
                            IsOfflineVinID = true,
                            ExtraEarnByCampaign = listExtraEarnByCampaign
                        };

                        _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "Sales", PlainText = plainTextData, Signature = signature, RequestXML = json, RequestPOS = requestPOS, ResponseXML = JsonConvert.SerializeObject(ex), StatusVINID = false, CallOffline = CallOffline, DateTime = DateTime.Now };
                        _logVID.WriteLogAPI(_modelLog);

                        return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                        {
                            Data = result,
                            Message = "Hiện tại hệ thống VINID đang không hoạt động",
                            Status = HttpStatusCode.ServiceUnavailable // Offline
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "Sales", PlainText = plainTextData, Signature = signature, RequestPOS = ex.Message, RequestXML = ex.Message, ResponseXML = JsonConvert.SerializeObject(ex), StatusVINID = false, CallOffline = CallOffline, DateTime = DateTime.Now };
                _logVID.WriteLogAPI(_modelLog);

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Data = null,
                    Message = "Lỗi hệ thống VINID API, Vui lòng liên hệ IT để được hỗ trợ",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }
        }
        public HttpResponseMessage VINIDRefund(string QRCode, string CardNumber, string MerchantId, string TerminalId, string InvoiceNo, string OrderNo, string OrigOrderNo, int SpendPoints, decimal RefundAmount, bool IsOffline, decimal OrderAmount, bool IsScanAndGo, List<CXRefundAmount> listAmountEarn = null)
        {

            //MerchantId = "1511";
            //TerminalId = "151101";
            var posStoreNo = MerchantId;
            var posTerminal = TerminalId;

            if (string.IsNullOrEmpty(QRCode))
            {
                QRCode = CardNumber;
            }

            var storeMapping = _logVID.StoreMapping(MerchantId, TerminalId);
            if (storeMapping != null)
            {
                TerminalId = storeMapping?.OldTerminalID;
                MerchantId = storeMapping?.OldStoreID;
            }

            try
            {
                var OriginalInvoiceNo_BillAmount = _logVID.OrigInvoiceNo(OrigOrderNo);//lay tu don hang goc

                var origanlInvoiceNo = OriginalInvoiceNo_BillAmount.Split('|')[0];
                var billAmount = OriginalInvoiceNo_BillAmount.Split('|')[1];
                var origBillAmount = OriginalInvoiceNo_BillAmount.Split('|')[2];
                var origAward = OriginalInvoiceNo_BillAmount.Split('|')[3];
                var source = OriginalInvoiceNo_BillAmount.Split('|')[4];
                var spendPointOrig = OriginalInvoiceNo_BillAmount.Split('|')[5];

                //Random rnd = new Random();
                var dateTime = DateTime.Now.ToString("yyMMddHHmmssff");
                var transaction_ref_number = InvoiceNo + "_" + dateTime;

                var channel_code = "VINMART";
                var description = "Refund Points";

                //OrigOrderNo + "|0|0|0"
                if (OriginalInvoiceNo_BillAmount == OrigOrderNo + "|0|0|0|S|0") // Tích lại đơn hàng gốc
                {
                    var listInvoiceError = _logVID.ListOrderOffline(posStoreNo, OrigOrderNo);
                    if (listInvoiceError != null && listInvoiceError.Count > 0)
                    {
                        var modelRequestPOS = listInvoiceError.FirstOrDefault();
                        if (modelRequestPOS != null)
                        {
                            var offlineSale = VINIDSales("", modelRequestPOS.MemberCardNo, modelRequestPOS.StoreNo, modelRequestPOS.POSTerminalNo, OrigOrderNo, 0, (decimal)modelRequestPOS.AmountToEarn, modelRequestPOS.OrderNo, false, (decimal)modelRequestPOS.OrderAmount, "", true);
                            if (offlineSale.StatusCode == HttpStatusCode.OK)
                            {
                                var OriginalInvoiceNo_BillAmount_offline = _logVID.OrigInvoiceNo(OrigOrderNo);//lay tu don hang goc
                                origanlInvoiceNo = OriginalInvoiceNo_BillAmount_offline.Split('|')[0];
                                billAmount = OriginalInvoiceNo_BillAmount_offline.Split('|')[1];
                                origBillAmount = OriginalInvoiceNo_BillAmount_offline.Split('|')[2];
                                origAward = OriginalInvoiceNo_BillAmount_offline.Split('|')[3];
                                source = OriginalInvoiceNo_BillAmount_offline.Split('|')[4];
                                spendPointOrig = OriginalInvoiceNo_BillAmount_offline.Split('|')[5];

                                try
                                {
                                    _logVID.UpdateOrderOffline(posStoreNo, posTerminal);
                                }
                                catch { }
                            }
                            else
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = null,
                                    Message = $"Hiện tại hệ thống không tìm thấy đơn hàng gốc {OrigOrderNo}, vui lòng liên hệ IT để hỗ trợ",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = $"Hiện tại hệ thống không tìm thấy đơn hàng gốc {OrigOrderNo}, vui lòng liên hệ IT để hỗ trợ",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = $"Hiện tại hệ thống không tìm thấy đơn hàng gốc {OrigOrderNo}, vui lòng liên hệ IT để hỗ trợ",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }

                string plainTextData = "|" + QRCode + "|" + channel_code + "|" + MerchantId + "|" + TerminalId + "|" + transaction_ref_number + "|" + InvoiceNo + "|" + origanlInvoiceNo + "|" + billAmount + "|" + RefundAmount;
                string signature = VINIDHelper.VinPaySignature(plainTextData);

                var spendPoint = int.Parse(string.IsNullOrEmpty(spendPointOrig) ? "0" : spendPointOrig) - SpendPoints;

                RefunModelRequest model = new RefunModelRequest
                {
                    transaction_ref_number = transaction_ref_number,
                    description = description,
                    original_invoice_no = origanlInvoiceNo,
                    code_value = QRCode,
                    //spend_points = SpendPoints.ToString(),
                    spend_points = spendPoint < 0 ? "0" : spendPoint.ToString(),
                    transaction = new transaction
                    {
                        bill_amount = billAmount,
                        refund_amount = RefundAmount.ToString(),//Đơn hàng ScanAndGo có tiêu điểm VINID dưới BLUEPOS: RefundAmount=0
                    },
                    date = DateTime.Now.ToString("yyyyMMdd"),
                    time = DateTime.Now.ToString("HHmmss"),
                    time_zone = "GMT+07:00",
                    merchant_id = MerchantId,
                    terminal_id = TerminalId,
                    channel_code = channel_code,
                    invoice_no = InvoiceNo,
                    signature = signature
                };
                var modelPos = new POSFunctionRefundRequest
                {
                    QRCode = QRCode,
                    CardNumber = CardNumber,
                    MerchantId = posStoreNo,
                    TerminalId = posTerminal,
                    InvoiceNo = InvoiceNo,
                    OrderNo = OrderNo,
                    OrigOrderNo = OrigOrderNo,
                    SpendPoints = SpendPoints,
                    RefundAmount = RefundAmount,
                    IsOffline = IsOffline,
                    OrderAmount = OrderAmount,
                    IsScanAndGo = IsScanAndGo,
                    ListAmountEarn = (listAmountEarn != null && listAmountEarn.Count > 0) ? listAmountEarn.Select(a => new POSAmountToEarn { LoyaltyMerchantId = a.LoyaltyMerchantId, Amount = a.Amount }).ToList() : null

                };

                if (RefundAmount > decimal.Parse(billAmount) && source != "SnG")
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Số tiền trả không được lớn hơn số tiền đơn hàng gốc",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                string requestPOS = JsonConvert.SerializeObject(modelPos);
                try
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    var serialize = new JavaScriptSerializer
                    {
                        MaxJsonLength = int.MaxValue
                    };
                    string json = serialize.Serialize(model);
                    string subUrl = "/internal/checkout/v1/refund-loyalty-points";
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            client.Timeout = TimeSpan.FromMinutes(2);
                            client.BaseAddress = new Uri(linkAPI_VINID);
                            client.DefaultRequestHeaders.Add("X-Api-Key", API_KEY);
                            client.DefaultRequestHeaders.Add("X-Api-Secret", API_SECRET);
                            client.DefaultRequestHeaders.Add("X-Lang", "vn");
                            client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
                            ServicePointManager.Expect100Continue = true;
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                            var response = client.PostAsync(subUrl, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                            milliseconds = st1.ElapsedMilliseconds;
                            st1.Stop();

                            if (response.IsSuccessStatusCode)
                            {
                                var content = response.Content.ReadAsStringAsync();
                                RefundResponse resp = JsonConvert.DeserializeObject<RefundResponse>(content.Result);
                                _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = InvoiceNo, ActionType = "Refund", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = serialize.Serialize(resp), ResponseTotal = milliseconds, DateTime = DateTime.Now };

                                if (resp.meta.code == 200)
                                {
                                    _modelLog.StatusVINID = true;
                                    //Task.Run(() => _logVID.WriteLogAPI(_modelLog));

                                    //luu transaction                               
                                    var qrCode = QRCode;
                                    var invoiceNo = InvoiceNo;
                                    var accessToken = resp.data.rs_header.access_token;
                                    var channelCode = resp.data.rs_header.channel_id;
                                    var gross_transaction_amount = resp.data.transaction.gross_transaction_amount;
                                    var nett_amt = resp.data.transaction.nett_amt;
                                    var refund_amount = resp.data.transaction.refund_amount;
                                    var response_code = resp.data.rs_header.response_code;
                                    var error_code = resp.data.rs_header.error_code;
                                    var error_message = resp.data.rs_header.error_message;
                                    var pool_units = (resp.data.member_balance != null && resp.data.member_balance.awards.Count > 0) ? resp.data.member_balance.awards.Sum(item => Convert.ToDouble(item.pool_units)) : 0;
                                    var redeem_units = SpendPoints;//Son Truyen
                                    var award = Math.Abs(double.Parse(origAward) - pool_units);

                                    var listExtraEarnByCampaign = new List<PointEarnCampaint>();
                                    if (listAmountEarn != null && listAmountEarn.Count > 0)
                                    {

                                        listExtraEarnByCampaign = listAmountEarn.Select(a => new PointEarnCampaint
                                        {
                                            LoyaltyMerchantId = "VCM",
                                            Amount = a.Amount,
                                            EarnedPoints = award

                                        }).ToList();
                                    }

                                    PointModel result = new PointModel
                                    {
                                        PointEarn = (int)award, //Tích
                                        PointRedeem = redeem_units, //Tiêu
                                        CurrentRate = 10,
                                        IsOfflineVinID = false,
                                        ExtraEarnByCampaign = listExtraEarnByCampaign
                                    };

                                    modelTrans = new LogTransactionLoyaltyModel
                                    {
                                        CardNumber = CardNumber,
                                        QRCode = qrCode,
                                        InvoiceNo = invoiceNo,
                                        AccessToken = accessToken,
                                        StoreID = posStoreNo,//son truyen
                                        TerminalId = posTerminal,//son truyen
                                        ChannelCode = channelCode,
                                        DateTime = DateTime.Now,
                                        TransactionType = "REF",
                                        TransactionName = "Trả",
                                        AmountOnOrderPOS = (double)OrderAmount,//Son truyen
                                        BillAmount = MathHelper.IsNumber(billAmount) - (double)RefundAmount,
                                        SpendPoints = MathHelper.IsNumber(spendPointOrig) - SpendPoints,
                                        RefundAmount = (double)RefundAmount,
                                        Operator = "-",
                                        OrderNo = OrderNo,
                                        OrigOrderNo = OrigOrderNo,
                                        OrigInvoiceNo = origanlInvoiceNo,
                                        OrigBillAmount = MathHelper.IsNumber(origBillAmount),
                                        Source = "POS",
                                        NettAmtOE = MathHelper.IsNumber(nett_amt),
                                        GrossTransactionAmountOE = MathHelper.IsNumber(gross_transaction_amount),
                                        AwardPointOE = pool_units,
                                        AwardAmountOE = pool_units * 10,
                                        RedeemPointOE = redeem_units,
                                        RedeemAmountOE = redeem_units * 10,
                                        PointPoolIDToRedeem = "P01",
                                        IsOffline = IsOffline,//son truyen
                                        ErrorCode = error_code,
                                        ErrorMessage = error_message,
                                        ResponseCode = response_code
                                    };
                                    //if (IsScanAndGo == false)
                                    //{
                                    //    Task.Run(() => logVID.WriteLogTransaction(modelTrans));
                                    //}

                                    Task.Run(() => _logVID.WriteLogTransaction(modelTrans));

                                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                    {
                                        Data = result,
                                        Message = "Thành công",
                                        Status = HttpStatusCode.OK
                                    });
                                }
                                else
                                {
                                    _modelLog.StatusVINID = false;
                                    //Task.Run(() => _logVID.WriteLogAPI(_modelLog));
                                    var notifyConfig = _memoryCacheService.GetNotifyConfig(_memoryCacheService.GetConnectStringCentralMD());

                                    if(resp.meta.code > 0)
                                    {
                                        var resultRsp = VINIDHelper.GetMessConfig(notifyConfig, resp.meta.code.ToString(), resp.data);
                                        return Request.CreateResponse(resultRsp.Status, new ResultResponse
                                        {
                                            Data = resultRsp.Data,
                                            Message = resultRsp.Message,
                                            Status = resultRsp.Status
                                        });
                                        
                                    }

                                    #region validate VinID
                                    //if (resp.meta.code == 81910007)//
                                    //{
                                    //    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    //    {
                                    //        Data = null,
                                    //        Message = "Thẻ VINID không tồn tại",
                                    //        Status = HttpStatusCode.Conflict //Chặn
                                    //    });
                                    //}
                                    //if (resp.meta.code == 81910010)//
                                    //{
                                    //    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    //    {
                                    //        Data = null,
                                    //        Message = "Thẻ VINID đã hết hạn. Vui lòng scan lại thẻ VINID",
                                    //        Status = HttpStatusCode.Conflict //Chặn
                                    //    });
                                    //}

                                    //if (resp.meta.code == 81910011)//
                                    //{
                                    //    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    //    {
                                    //        Data = null,
                                    //        Message = "Tài khoản VINID của khách hàng đã bị khóa",
                                    //        Status = HttpStatusCode.Conflict //Chặn
                                    //    });
                                    //}

                                    //if (resp.meta.code == 41207101)//
                                    //{
                                    //    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    //    {
                                    //        Data = null,
                                    //        Message = "Tài khoản không đủ điểm để hoàn, Vui lòng báo KH nạp thêm điểm để có thể trả hàng",
                                    //        Status = HttpStatusCode.Conflict // Chặn  
                                    //    });
                                    //}

                                    //if (resp.meta.code == 81910022)//
                                    //{
                                    //    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    //    {
                                    //        Data = null,
                                    //        Message = "Ko tiêu điểm VINID, vui lòng chọn hình thức TT khác",
                                    //        Status = HttpStatusCode.Conflict
                                    //    });
                                    //}

                                    /////////

                                    //if (resp.meta.code == 81910008)//
                                    //{
                                    //    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    //    {
                                    //        Data = null,
                                    //        Message = "Thẻ VINID bị khóa",
                                    //        Status = HttpStatusCode.Conflict //Offline
                                    //    });
                                    //}

                                    //if (resp.meta.code == 81910009)//
                                    //{
                                    //    return Request.CreateResponse(HttpStatusCode.Unused, new ResultResponse
                                    //    {
                                    //        Data = null,
                                    //        Message = "Thẻ VINID chưa được kích hoạt",
                                    //        Status = HttpStatusCode.Unused //Offline
                                    //    });
                                    //}
                                    //if (resp.meta.code == 81910012)//
                                    //{
                                    //    return Request.CreateResponse(HttpStatusCode.Unused, new ResultResponse
                                    //    {
                                    //        Data = null,
                                    //        Message = "Thẻ VINID chưa được kích hoạt",
                                    //        Status = HttpStatusCode.Unused //Offline
                                    //    });
                                    //}

                                    //if (resp.meta.code == 81910005)//
                                    //{
                                    //    return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                                    //    {
                                    //        Data = null,
                                    //        Message = "Hiện tại hệ thống VINID không có phản hồi",
                                    //        Status = HttpStatusCode.ServiceUnavailable //Offline
                                    //    });
                                    //}

                                    //if (resp.meta.code == 81910001)//
                                    //{
                                    //    return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                                    //    {
                                    //        Data = null,
                                    //        Message = "Hiện tại hệ thống VINID không hoạt động",
                                    //        Status = HttpStatusCode.ServiceUnavailable //Offline
                                    //    });

                                    //}

                                    #endregion

                                    var resultMessage = resp.meta.message;
                                    var spMessage = resp.meta.message.Split('\t');
                                    if (spMessage.Length > 1)
                                        resultMessage = spMessage[1] == "" ? spMessage[0] : spMessage[1];

                                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                    {
                                        Data = null,
                                        Message = resultMessage,
                                        Status = HttpStatusCode.Conflict //Offline
                                    });
                                }
                            }
                            else
                            {
                                _modelLog = new LogAPIModel { StoreNo = posStoreNo, POSTerminal = posTerminal, CardNumber = model.code_value, InvoiceNo = model.invoice_no, ActionType = "Refund", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = JsonConvert.SerializeObject(response), ResponseTotal = milliseconds, StatusVINID = false, DateTime = DateTime.Now };
                                //Task.Run(() => _logVID.WriteLogAPI(_modelLog));

                                if (response.StatusCode.ToString() == "522")//TimeOut
                                {

                                    var calAwad = _logVID.CalAwardOffline((double)RefundAmount);
                                    int tichDiem = calAwad == null ? 0 : (int)(Math.Round(((double)calAwad.Percent * (double)RefundAmount) / 10, 0));//Tích điểm
                                    var listExtraEarnByCampaign = new List<PointEarnCampaint>();
                                    if (listAmountEarn != null && listAmountEarn.Count > 0)
                                    {

                                        listExtraEarnByCampaign = listAmountEarn.Select(a => new PointEarnCampaint
                                        {
                                            LoyaltyMerchantId = "VCM",
                                            Amount = a.Amount,
                                            EarnedPoints = tichDiem

                                        }).ToList();
                                    }

                                    PointModel result = new PointModel
                                    {
                                        PointEarn = tichDiem,
                                        PointRedeem = SpendPoints,//Tiêu điểm
                                        CurrentRate = 10,
                                        IsOfflineVinID = true,
                                        ExtraEarnByCampaign = listExtraEarnByCampaign

                                    };
                                    return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                                    {
                                        Data = result,
                                        Message = "Hiện tại hệ thống VINID đang không hoạt động",
                                        Status = HttpStatusCode.ServiceUnavailable
                                    });
                                }

                                return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                {
                                    Data = null,
                                    Message = "Lỗi hệ thống VINID, Vui lòng liên hệ IT để được hỗ trợ",
                                    Status = HttpStatusCode.Conflict //Offline
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            var calAwad = _logVID.CalAwardOffline((double)RefundAmount);
                            int tichDiem = calAwad == null ? 0 : (int)(Math.Round(((double)calAwad.Percent * (double)RefundAmount) / 10, 0));//Tích điểm
                            var listExtraEarnByCampaign = new List<PointEarnCampaint>();
                            if (listAmountEarn != null && listAmountEarn.Count > 0)
                            {

                                listExtraEarnByCampaign = listAmountEarn.Select(a => new PointEarnCampaint
                                {
                                    LoyaltyMerchantId = "VCM",
                                    Amount = a.Amount,
                                    EarnedPoints = tichDiem

                                }).ToList();
                            }

                            PointModel result = new PointModel
                            {
                                PointEarn = tichDiem,
                                PointRedeem = SpendPoints,//Tiêu điểm
                                CurrentRate = 10,
                                IsOfflineVinID = true,
                                ExtraEarnByCampaign = listExtraEarnByCampaign
                            };

                            _modelLog = new LogAPIModel
                            {
                                StoreNo = posStoreNo,
                                POSTerminal = posTerminal,
                                CardNumber = model.code_value,
                                InvoiceNo = model.invoice_no,
                                ActionType = "Refund",
                                PlainText = plainTextData,
                                Signature = signature,
                                RequestPOS = requestPOS,
                                RequestXML = json,
                                ResponseXML = JsonConvert.SerializeObject(ex),
                                DateTime = DateTime.Now,
                                StatusVINID = false
                            };
                            //Task.Run(() => _logVID.WriteLogAPI(_modelLog));

                            return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                            {
                                Data = result,
                                Message = "Hiện tại hệ thống VINID đang không hoạt động",
                                Status = HttpStatusCode.ServiceUnavailable
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _modelLog = new LogAPIModel
                    {
                        StoreNo = posStoreNo,
                        POSTerminal = posTerminal,
                        CardNumber = model.code_value,
                        InvoiceNo = model.invoice_no,
                        ActionType = "Refund",
                        RequestPOS = ex.Message,
                        RequestXML = JsonConvert.SerializeObject(ex),
                        ResponseXML = JsonConvert.SerializeObject(ex),
                        StatusVINID = false,
                        DateTime = DateTime.Now
                    };
                    //Task.Run(() => _logVID.WriteLogAPI(_modelLog));


                    //var calAwad = logVID.CalAwardOffline((double)OrderAmount);
                    //PointModel result = new PointModel
                    //{
                    //    PointEarn = calAwad == null ? 0 : (int)(Math.Round(((double)calAwad.Percent * (double)OrderAmount), 0)),//Tích điểm
                    //    PointRedeem = 0,//Tiêu điểm
                    //    IsOfflineVinID = true
                    //};

                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                    {
                        Data = null,
                        Message = "Lỗi hệ thống VINID API, Vui lòng liên hệ IT để được hỗ trợ",
                        Status = HttpStatusCode.Conflict //Offline
                    });
                    //return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
                }

            }
            catch (Exception ex)
            {
                UtilsAPI.Info("Refund", JsonConvert.SerializeObject(ex));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = "Hiện tại hệ thống VINID đang không hoạt động",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
        public HttpResponseMessage VINIDExtraSales(ExtraSalePosRequestModel pos)
        {

            var TerminalId = pos.PosTerminal;
            var MerchantId = pos.StoreCode;
            var storeMapping = _logVID.StoreMapping(pos.StoreCode, pos.PosTerminal);
            if (storeMapping != null)
            {
                TerminalId = storeMapping?.OldVinPayTerminalID;
                MerchantId = storeMapping?.OldVinPayStoreID;
            }

            var PosCode = pos.PosTerminal.Substring(4, pos.PosTerminal.Length - 4);

            var orderDate = pos.OrderDate;

            long timeUni = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Int64 timeUnix = timeUni;
            Int64 timeUnixOrderDate = timeUni;

            if (orderDate != null)
            {
                try
                {
                    DateTimeOffset offset = new DateTimeOffset(orderDate);
                    long unixOrderDate = offset.ToUnixTimeSeconds();
                    timeUnixOrderDate = unixOrderDate;
                }
                catch
                {

                }
            }

            Int64 totalBill = (Int64)pos.TotalBillAmount;
            string plainTextData = pos.VinidCSN + "|" + pos.OrderNo + "|" + pos.StoreCode + "|" + PosCode + "|" + MerchantId + "|" + TerminalId + "|" + totalBill + "|" + timeUnix + "|" + pos.ReferenceNumber;
            var signature = VINIDHelper.VinPaySignature(plainTextData);

            var model = new ExtraSaleModel
            {
                receipt_number = pos.OrderNo, //=>invoiceNo : lenght=8
                transaction_type = "0",
                store_code = pos.StoreCode,
                pos_code = PosCode,
                merchant_id = MerchantId,
                terminal_id = TerminalId,
                cashier_code = pos.CashierCode,//bỏ
                transaction_time = timeUnix,
                business_time = timeUnixOrderDate,//bỏ
                customer_name = pos.CustomerName,//bỏ
                vinid_card_number = pos.VinidCardNumber,//bỏ
                vinid_csn = pos.VinidCSN,//bỏ
                total_bill_amount = pos.TotalBillAmount,
                reference_number = pos.ReferenceNumber,
                bill_lines = pos.BillLines.Select(x => new BillLine
                {

                    amount = x.Amount,
                    article = x.Article,
                    article_name = x.ArticleName,//bỏ
                    barcode = x.Barcode,//bỏ
                    discount_amount = x.DiscountAmount,
                    line_amount = x.LineAmount,
                    quantity = x.Quantity,
                    record_no = (float)x.RecordNo,
                    sale_price = x.SalePrice,
                    uom = x.UOM
                }).ToList(),
                signature = signature
            };

            var modelPos = new ExtraSalePosRequestModel
            {
                CardNumber = pos.CardNumber,
                ReceiptNumber = pos.ReceiptNumber,
                OrderNo = pos.OrderNo,
                OrderDate = orderDate,
                StoreCode = pos.StoreCode,
                PosTerminal = pos.PosTerminal,
                CashierCode = pos.CashierCode,
                CustomerName = pos.CustomerName,
                VinidCardNumber = pos.VinidCardNumber,
                VinidCSN = pos.VinidCSN,
                TotalBillAmount = pos.TotalBillAmount,
                ReferenceNumber = pos.ReferenceNumber,
                BillLines = pos.BillLines
            };
            string requestPOS = JsonConvert.SerializeObject(modelPos);
            try
            {
                var st1 = new Stopwatch();
                st1.Start();
                long milliseconds = 0;

                var serialize = new JavaScriptSerializer
                {
                    MaxJsonLength = int.MaxValue
                };
                string json = serialize.Serialize(model);
                string subUrl = "/loyalty-extra-point/v1/sale";
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPIScanAndGo);

                        client.DefaultRequestHeaders.Add("X-Api-Key", API_KEY);
                        client.DefaultRequestHeaders.Add("X-Api-Secret", API_SECRET);
                        client.DefaultRequestHeaders.Add("X-Lang", "vn");
                        client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        var response = client.PostAsync(subUrl, new StringContent(json, Encoding.UTF8, "application/json")).Result;


                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        var content = response.Content.ReadAsStringAsync();
                        ExtralSaleResponse resp = JsonConvert.DeserializeObject<ExtralSaleResponse>(content.Result);

                        _modelLog = new LogAPIModel { StoreNo = pos.StoreCode, POSTerminal = pos.PosTerminal, CardNumber = pos.CardNumber, InvoiceNo = pos.OrderNo, ActionType = "ExtraSale", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = serialize.Serialize(resp), ResponseTotal = milliseconds, DateTime = DateTime.Now };


                        if (resp.meta.code == 200)
                        {
                            _modelLog.StatusVINID = true;
                            //Task.Run(() => _logVID.WriteLogAPI(_modelLog));
                            var extralHeader = new ExtraHeaderModel
                            {
                                StoreNo = pos.StoreCode,
                                POSCode = pos.PosTerminal,
                                ReceiptNumber = resp.data.receipt_number,//bỏ
                                OrderNo = pos.OrderNo,
                                MerchantId = MerchantId,
                                TerminalId = TerminalId,
                                TransactionTime = resp.data.transaction_time,
                                TransactionType = resp.data.transaction_type,//bỏ
                                CashierCode = resp.data.cashier_code,//bỏ
                                BusinessTime = resp.data.business_time,//bỏ
                                CustomerName = resp.data.customer_name,//bỏ
                                VinidCardNumber = resp.data.vinid_card_number,//bỏ
                                VinidCSN = resp.data.vinid_csn,//bỏ
                                TotalBillAmount = resp.data.total_bill_amount,
                                ReferenceNumber = resp.data.reference_number,
                                OverQuota = resp.data.over_quota,
                                ExtraEarnByItems = resp.data.extra_earn_by_items,
                                ExtraEarnByCampaign = resp.data.extra_earn_by_campaign,
                                EmployeeCode = resp.data.employee_code,
                                CompanyCode = resp.data.company_code,
                                ActionAPI = "ExtraSale",
                                CreatedDate = DateTime.Now,
                                CreatedUser = pos.CashierCode
                            };
                            var extraItem = resp.data.bill_lines.Select(a => new ExtratItemBillModel
                            {
                                ReceiptNumber = resp.data.receipt_number,
                                RecordNo = a.record_no,
                                Barcode = a.barcode,//bỏ
                                Article = a.article,
                                ArticleName = a.article_name,//bỏ
                                UOM = a.uom,
                                Quantity = a.quantity,
                                SalePrice = a.sale_price,
                                Amount = a.amount,
                                DiscountAmount = a.discount_amount,
                                LineAmount = a.line_amount,
                                ExtraQuantityEarn = a.extra_quantity_earn,//Số sản phẩm được tích theo mã sản phẩm 
                                ExtraAmountEarn = a.extra_amount_earn,//Số điểm được tích theo mã sản phẩm                               

                            }).ToList();
                            Task.Run(() => _logVID.InsertExtral(extralHeader, extraItem));

                            var result = new ExtraSalePOSResponse
                            {
                                OverQuota = resp.data.over_quota,//Quota còn lại theo chiến dịch của khách hàng
                                ExtraEarnByItems = resp.data.extra_earn_by_items,//Số điểm được tích trên giao dịch
                                ExtraEarnByCampaign = resp.data.extra_earn_by_campaign,//Số điểm được tích theo campaign (1%)
                                CompanyCode = resp.data.company_code,
                                EmployCode = resp.data.employee_code,
                                ListItem = resp.data.bill_lines.Select(a => new ExtraSaleItemPOSResponse
                                {
                                    RecordNo = a.record_no,
                                    Barcode = a.barcode,
                                    Article = a.article,
                                    ArticleName = a.article_name,
                                    UOM = a.uom,
                                    Quantity = a.quantity,
                                    SalePrice = a.sale_price,
                                    Amount = a.amount,
                                    DiscountAmount = a.discount_amount,
                                    LineAmount = a.line_amount,
                                    ExtraQuantityEarn = a.extra_quantity_earn,//Số sản phẩm được tích theo mã sản phẩm 
                                    ExtraAmountEarn = a.extra_amount_earn     //Số điểm được tích theo mã sản phẩm 

                                }).ToList()
                            };

                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = result,
                                Message = "Thành công",
                                Status = HttpStatusCode.OK
                            });
                        }
                        else
                        {
                            //modelLog = new LogAPIModel { StoreNo = pos.StoreCode, POSTerminal = pos.PosTerminal, CardNumber = pos.CardNumber, InvoiceNo = pos.ReceiptNumber, ActionType = "ExtraSale", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = response.ToString(), DateTime = DateTime.Now };
                            //logVID.WriteLogAPI(modelLog);

                            _modelLog.StatusVINID = false;
                            Task.Run(() => _logVID.WriteLogAPI(_modelLog));

                            var resultMessage = resp.meta.message;
                            var spMessage = resp.meta.message.Split('\t');
                            if (spMessage.Length > 1)
                                resultMessage = spMessage[1];

                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = resultMessage,
                                Status = HttpStatusCode.BadRequest
                            });
                        }

                    }
                    catch (Exception ex)
                    {
                        _modelLog = new LogAPIModel { StoreNo = pos.StoreCode, POSTerminal = pos.PosTerminal, CardNumber = pos.CardNumber, InvoiceNo = pos.OrderNo, ActionType = "ExtraSale", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = JsonConvert.SerializeObject(ex), StatusVINID = false, ResponseTotal = milliseconds, DateTime = DateTime.Now };
                        Task.Run(() => _logVID.WriteLogAPI(_modelLog));

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = "Lỗi hệ thống VINID, Vui lòng liên hệ IT để được hỗ trợ",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }

            catch (Exception ex)
            {
                _modelLog = new LogAPIModel { StoreNo = pos.StoreCode, POSTerminal = pos.PosTerminal, CardNumber = pos.CardNumber, InvoiceNo = pos.OrderNo, ActionType = "ExtraSale", PlainText = plainTextData, Signature = signature, RequestPOS = ex.Message, RequestXML = ex.Message, ResponseXML = JsonConvert.SerializeObject(ex), StatusVINID = false, DateTime = DateTime.Now };
                Task.Run(() => _logVID.WriteLogAPI(_modelLog));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = "Lỗi hệ thống VINID API, Vui lòng liên hệ IT để được hỗ trợ",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
        public HttpResponseMessage VINIDExtraRefund(ExtraRefundPosRequestModel pos)
        {

            var TerminalId = pos.PosTerminal;
            var MerchantId = pos.StoreCode;
            var storeMapping = _logVID.StoreMapping(pos.StoreCode, pos.PosTerminal);
            if (storeMapping != null)
            {
                TerminalId = storeMapping?.OldVinPayTerminalID;
                MerchantId = storeMapping?.OldVinPayStoreID;
            }

            var PosCode = pos.PosTerminal.Substring(4, pos.PosTerminal.Length - 4);
            long timeUni = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Int64 timeUnix = timeUni;
            Int64 totalBill = (Int64)pos.TotalBillAmount;
            string plainTextData = pos.OrderNo + "|" + pos.StoreCode + "|" + PosCode + "|" + MerchantId + "|" + TerminalId + "|" + totalBill + "|" + timeUnix + "|" + pos.ReferenceNumber;
            var signature = VINIDHelper.VinPaySignature(plainTextData);
            ExtraRefundModel model = new ExtraRefundModel
            {
                receipt_number = pos.OrderNo,
                original_receipt_number = pos.OriginalReceiptNumber,
                transaction_type = "1",
                store_code = pos.StoreCode,
                pos_code = PosCode,
                merchant_id = MerchantId,
                terminal_id = TerminalId,
                cashier_code = pos.CashierCode,
                transaction_time = timeUnix,
                business_time = timeUnix,
                total_bill_amount = pos.TotalBillAmount,
                reference_number = pos.ReferenceNumber,
                earn_by_default = pos.EarnByDefault,
                original_reference_number = pos.OriginalReferenceNumber,
                refund_amount = pos.RefundAmount,
                refund_point_by_default = pos.RefundPointDefault,
                bill_lines = pos.BillLines.Select(x => new BillLineRefund
                {
                    amount = x.Amount,
                    article = x.Article,
                    article_name = x.ArticleName,
                    barcode = x.Barcode,
                    discount_amount = x.DiscountAmount,
                    line_amount = x.LineAmount,
                    quantity = x.Quantity,
                    record_no = (float)x.RecordNo,
                    refund_quantity = x.RefundQuantity,
                    sale_price = x.SalePrice,
                    uom = x.UOM
                }).ToList(),
                original_pos_number = PosCode,
                signature = signature
            };

            var modelPos = new ExtraRefundPosRequestModel
            {
                CardNumber = pos.CardNumber,
                CustomerName = pos.CustomerName,
                ReceiptNumber = pos.ReceiptNumber,
                OrderNo = pos.OrderNo,
                StoreCode = pos.StoreCode,
                PosTerminal = pos.PosTerminal,
                CashierCode = pos.CashierCode,
                TotalBillAmount = pos.TotalBillAmount,
                OriginalReceiptNumber = pos.OriginalReceiptNumber,
                OriginalPosNumber = pos.OriginalPosNumber,
                OriginalReferenceNumber = pos.OriginalReferenceNumber,
                ReferenceNumber = pos.ReferenceNumber,
                EarnByDefault = pos.EarnByDefault,
                RefundAmount = pos.RefundAmount,
                RefundPointDefault = pos.RefundPointDefault,
                BillLines = pos.BillLines
            };
            string requestPOS = JsonConvert.SerializeObject(modelPos);

            try
            {
                var st1 = new Stopwatch();
                st1.Start();
                long milliseconds = 0;

                var serialize = new JavaScriptSerializer
                {
                    MaxJsonLength = int.MaxValue
                };
                string json = serialize.Serialize(model);
                string subUrl = "/loyalty-extra-point/v1/refund";
                using (var client = new HttpClient())
                {
                    try
                    {

                        //linkAPI = "https://api-uat.int.vinid.dev";
                        //linkAPI = "https://api-uat.vinid.net";
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPIScanAndGo);
                        client.DefaultRequestHeaders.Add("X-Api-Key", API_KEY);
                        client.DefaultRequestHeaders.Add("X-Api-Secret", API_SECRET);
                        client.DefaultRequestHeaders.Add("X-Lang", "vn");
                        client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        var response = client.PostAsync(subUrl, new StringContent(json, Encoding.UTF8, "application/json")).Result;
                        var content = response.Content.ReadAsStringAsync();
                        ExtraRefundResponse resp = JsonConvert.DeserializeObject<ExtraRefundResponse>(content.Result);


                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        _modelLog = new LogAPIModel { StoreNo = pos.StoreCode, POSTerminal = pos.PosTerminal, CardNumber = pos.CardNumber, InvoiceNo = pos.OrderNo, ActionType = "ExtraRefund", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = serialize.Serialize(resp), ResponseTotal = milliseconds, DateTime = DateTime.Now };


                        if (resp.meta.code == 200)
                        {
                            _modelLog.StatusVINID = true;
                            Task.Run(() => _logVID.WriteLogAPI(_modelLog));

                            var extraItem = new List<ExtratItemBillModel>();
                            var result = new ExtraRefundPOSResponse
                            {
                                RefundAmount = resp.data.refund_amount,
                                RefundPointDefault = resp.data.refund_point_by_default,
                                ExtraRefundByItems = resp.data.extra_refund_by_items,
                                ExtraRefundByCampaign = resp.data.extra_refund_by_campaign,
                                CompanyCode = resp.data.company_code,
                                EmployCode = resp.data.employee_code
                            };

                            if (resp.data.bill_lines != null && resp.data.bill_lines.Count > 0)
                            {
                                result.ListItem = resp.data.bill_lines.Select(a => new ExtraRefundtemPOSResponse
                                {
                                    RecordNo = a.record_no,
                                    Barcode = a.barcode,
                                    Article = a.article,
                                    Quantity = a.quantity,
                                    SalePrice = a.sale_price,
                                    Amount = a.amount,
                                    ArticleName = a.article_name,
                                    UOM = a.uom,
                                    DiscountAmount = a.discount_amount,
                                    LineAmount = a.line_amount,
                                    ExtraAmountEarn = a.extra_amount_refund,
                                    ExtraQuantityEarn = a.extra_quantity_refund

                                }).ToList();

                                extraItem = resp.data.bill_lines.Select(a => new ExtratItemBillModel
                                {
                                    ReceiptNumber = resp.data.receipt_number,
                                    RecordNo = a.record_no,
                                    Barcode = a.barcode,
                                    Article = a.article,
                                    ArticleName = a.article_name,
                                    UOM = a.uom,
                                    Quantity = a.quantity,
                                    SalePrice = a.sale_price,
                                    Amount = a.amount,
                                    DiscountAmount = a.discount_amount,
                                    LineAmount = a.line_amount,
                                    ExtraQuantityRefund = a.extra_quantity_refund,
                                    ExtraAmountRefund = a.extra_amount_refund

                                }).ToList();
                            }

                            var extralHeader = new ExtraHeaderModel
                            {
                                StoreNo = pos.StoreCode,
                                POSCode = pos.PosTerminal,
                                ReceiptNumber = resp.data.receipt_number,
                                OrderNo = pos.OrderNo,
                                MerchantId = MerchantId,
                                TerminalId = TerminalId,
                                TransactionTime = resp.data.transaction_time,
                                TransactionType = resp.data.transaction_type,
                                CashierCode = resp.data.cashier_code,
                                BusinessTime = resp.data.business_time,
                                CustomerName = pos.CustomerName,
                                VinidCardNumber = pos.CardNumber,
                                VinidCSN = resp.data.vinid_csn,
                                TotalBillAmount = resp.data.total_bill_amount,
                                ReferenceNumber = resp.data.reference_number,
                                ExtraRefundByItems = resp.data.extra_refund_by_items,
                                ExtraRefundByCampaign = resp.data.extra_refund_by_campaign,
                                OriginalReceiptNumber = resp.data.original_receipt_number,
                                OriginalPosNumber = resp.data.original_pos_number,
                                OriginalReferenceNumber = resp.data.original_reference_number,
                                RefundAmount = resp.data.refund_amount,
                                RefundPointDefault = resp.data.refund_point_by_default,
                                EarnByDefault = resp.data.earn_by_default,
                                EmployeeCode = resp.data.employee_code,
                                CompanyCode = resp.data.company_code,
                                ActionAPI = "ExtraRefund",
                                CreatedDate = DateTime.Now,
                                CreatedUser = pos.CashierCode
                            };

                            Task.Run(() => _logVID.InsertExtral(extralHeader, extraItem)); //insert to table ExtraHeader và ExtratItemBill

                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = result,
                                Message = "Thành công",
                                Status = HttpStatusCode.OK
                            });
                        }
                        else
                        {
                            //modelLog = new LogAPIModel { StoreNo = pos.StoreCode, POSTerminal = PosCode, CardNumber = "", InvoiceNo = pos.InvoiceNo, ActionType = "ExtraRefund", PlainText = plainTextData, Signature = signature, RequestXML = json, ResponseXML = response.ToString(), DateTime = DateTime.Now };
                            //logVID.WriteLogAPI(modelLog);
                            _modelLog.StatusVINID = false;
                            Task.Run(() => _logVID.WriteLogAPI(_modelLog));

                            if (resp.meta.code == 41207101)//
                            {
                                return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                {
                                    Data = null,
                                    Message = "Tài khoản không đủ điểm để hoàn, Vui lòng báo KH nạp thêm điểm để có thể trả hàng",
                                    Status = HttpStatusCode.Conflict // Chặn  
                                });
                            }
                            if (resp.meta.code == 81910022)//
                            {
                                return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                {
                                    Data = null,
                                    Message = "Ko tiêu điểm VINID, vui lòng chọn hình thức TT khác",
                                    Status = HttpStatusCode.Conflict
                                });
                            }

                            var resultMessage = resp.meta.message;
                            var spMessage = resp.meta.message.Split('\t');
                            if (spMessage.Length > 1)
                                resultMessage = spMessage[1];

                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = resultMessage,
                                Status = HttpStatusCode.BadRequest
                            });
                        }

                    }
                    catch (Exception ex)
                    {
                        _modelLog = new LogAPIModel { StoreNo = pos.StoreCode, POSTerminal = pos.PosTerminal, CardNumber = pos.CardNumber, InvoiceNo = pos.OrderNo, ActionType = "ExtraRefund", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = json, ResponseXML = JsonConvert.SerializeObject(ex), ResponseTotal = milliseconds, StatusVINID = false, DateTime = DateTime.Now };
                        Task.Run(() => _logVID.WriteLogAPI(_modelLog));
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = "Lỗi hệ thống VINID, Vui lòng liên hệ IT để được hỗ trợ",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _modelLog = new LogAPIModel { StoreNo = pos.StoreCode, POSTerminal = pos.PosTerminal, CardNumber = pos.CardNumber, InvoiceNo = pos.OrderNo, ActionType = "ExtraRefund", PlainText = plainTextData, Signature = signature, RequestPOS = requestPOS, RequestXML = ex.Message, ResponseXML = JsonConvert.SerializeObject(ex), StatusVINID = false, DateTime = DateTime.Now };
                Task.Run(() => _logVID.WriteLogAPI(_modelLog));
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = "Lỗi hệ thống VINID API, Vui lòng liên hệ IT để được hỗ trợ",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
        HttpResponseMessage CXGetInfoMember(string codeValue, string storeNo, string posID, string clubCode)
        {
            clubCode = string.IsNullOrEmpty(clubCode) ? clubCodeWin : clubCode;
            string endPoint = $"{UrlCrownX.CXUrlInfoV2}";
            string param = $"?codeValue={codeValue}&clubCode={clubCode}";
            var qrCode = "";
            long responseTime = 0;

            if (codeValue.Substring(0, 1) == "C")//QRCode
            {
                qrCode = codeValue;
            }

            try
            {
                //var regexItem = new Regex("^[a-zA-Z0-9 ]*$");
                if (!RegexHelper.IsCharacters(codeValue))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Mã thẻ/QRCode/số điện thoại không hợp lệ, vui lòng scan lại thông tin",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                _modelLogCX = new CX_LogAPIModel
                {
                    CardNumber = codeValue,
                    StoreNo = storeNo,
                    POSTerminal = posID,
                    ActionType = "CXGetInfoMember",
                    RequestPOS = $"{CXUrl}{endPoint}",
                    RequestXML = $"{CXUrl}{endPoint}",
                    Source = "CX",
                    DateTime = DateTime.Now
                };

                using (var client = new HttpClient())
                {
                    try
                    {
                        HttpStatusCode statusCode = HttpStatusCode.OK;
                        string response = _winLifeService.CallApiCrowX(posID, endPoint, MethodApiEnum.GET.ToString(), param, ref statusCode, ref responseTime);
                        var resultData = JsonConvert.DeserializeObject<ResponseCrownX>(response);
                        
                         //Save to Logs db
                        _modelLogCX.ResponseXML = JsonConvert.SerializeObject(resultData);
                        _modelLogCX.ResponseTotal = responseTime;
                        Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                        if (statusCode == HttpStatusCode.OK)
                        {
                            var dataResult = JsonConvert.SerializeObject(resultData.data);
                            var modelCustomer = JsonConvert.DeserializeObject<CustomerEnquiryModel>(dataResult);
                            
                            var isShowMsg = !string.IsNullOrEmpty(modelCustomer.cardStatus) && ((modelCustomer.cardStatus == "BLOCK" || string.IsNullOrEmpty(modelCustomer.cardStatus) || modelCustomer.cardStatus == "INACTIVE"));
                            var showMsg = "Thẻ đang bị Khóa/Chưa kích hoạt. Vui lòng thông báo với KH để làm thủ tục kích hoạt lại thẻ";

                            var firstMemberBalance = modelCustomer.memberBalance?.FirstOrDefault(d => d.subPoolId == clubCode);
                            var memberPoint = 0;
                            var totalPoint = 0;
                            var currentRate = 0;
                            if (firstMemberBalance != null)
                            {
                                memberPoint = Convert.ToInt32(firstMemberBalance.availableBalance ?? 0);
                                totalPoint = Convert.ToInt32(firstMemberBalance.totalBalance ?? 0);
                                currentRate = (int)firstMemberBalance.currencyRate;
                            }

                            InfoMemberModel result = new InfoMemberModel
                            {
                                CardNumber = !string.IsNullOrEmpty(modelCustomer.cardNo) ? modelCustomer.cardNo : modelCustomer.phoneNo,
                                VirtualCard = "",
                                CMND = "",
                                MemberName = modelCustomer.fullName,
                                Title = modelCustomer.title,
                                CardLevel = modelCustomer.customerTierDesc,
                                MemberCSN = modelCustomer.csn,
                                PhoneNumber = modelCustomer.phoneNo,
                                OtherInfo = "",
                                QRCode = qrCode,
                                Dob = StringHelper.FormatDate_yyyyMMdd(modelCustomer.dob?? modelCustomer.dateOfBirth.ToString()),
                                DateOfBirth = StringHelper.FormatDate_yyyyMMdd(modelCustomer.dateOfBirth.ToString()),
                                BirthdayGiftInd = modelCustomer.birthdayGiftInd,
                                MemberPoint = memberPoint,
                                TotalPoint = totalPoint,
                                ExtraPoint = modelCustomer.isBlue,
                                CurrentRate = currentRate,
                                IsOfflineVinID = false,
                                IsShowMessage = isShowMsg,
                                IsRedeem = (bool)modelCustomer.isRedeem,
                                Status = modelCustomer.cardStatus == "ACTIVE" ? "Hoạt động" : modelCustomer.cardStatus == "BLOCK" ? "Khóa thẻ" : modelCustomer.cardStatus == "INACTIVE" ? "InActive" : string.IsNullOrEmpty(modelCustomer.cardStatus) ? "" : modelCustomer.cardStatus,
                                //Status = modelCustomer.cardStatus == "ACTIVE" ? "Hoạt động" : modelCustomer.cardStatus == "BLOCK" ? "Khóa thẻ" : string.IsNullOrEmpty(modelCustomer.cardStatus) ? "Chưa hoạt động" : modelCustomer.cardStatus == "INACTIVE" ? "Không hoạt động" : modelCustomer.cardStatus,
                                System = "CX",
                                ClubCode = clubCode,
                                AvailablePromotion = _loyaltyService.GetWinCodePromotion(storeNo,posID, codeValue, null)
                            };
                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = result,
                                Message = isShowMsg == true ? showMsg : "Thành công",
                                Status = HttpStatusCode.OK
                            });
                        }
                        else if (statusCode == HttpStatusCode.InternalServerError)//OFFLINE
                        {
                            InfoMemberModel result = new InfoMemberModel
                            {
                                CardNumber = codeValue,
                                CMND = string.Empty,
                                MemberName = string.Empty,
                                CardLevel = string.Empty,
                                MemberCSN = string.Empty,
                                PhoneNumber = string.Empty,
                                OtherInfo = "",
                                QRCode = qrCode,
                                MemberPoint = 0,
                                TotalPoint = 0,
                                CurrentRate = 10,
                                ExtraPoint = false,
                                IsRedeem = false,
                                IsOfflineVinID = true,
                                IsShowMessage = false,
                                Status = "",
                                System = "CX"
                            };

                            return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                            {
                                Data = result,
                                Message = $"Lỗi hệ thống API CX",
                                Status = HttpStatusCode.InternalServerError
                            });
                        }
                        else
                        {
                            if (resultData.code == "E003")
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, new ResultResponse
                                {
                                    Data = null,
                                    Message = $"{resultData.message}",
                                    Status = HttpStatusCode.NotFound//404 chua dang ky
                                });
                            }

                            return Request.CreateResponse(statusCode, new ResultResponse
                            {
                                Data = null,
                                Message = $"{resultData.message}",
                                Status = statusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                        Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

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
                _modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));
                InfoMemberModel result = new InfoMemberModel
                {
                    CardNumber = codeValue,
                    CMND = string.Empty,
                    MemberName = string.Empty,
                    CardLevel = string.Empty,
                    MemberCSN = string.Empty,
                    PhoneNumber = string.Empty,
                    OtherInfo = "",
                    QRCode = qrCode,
                    MemberPoint = 0,
                    TotalPoint = 0,
                    CurrentRate = 10,
                    ExtraPoint = false,
                    IsRedeem = false,
                    IsOfflineVinID = true,
                    IsShowMessage = false,
                    Status = "",
                    System = "CX"
                };
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Data = result,
                    Message = $"Lỗi hệ thống API Blue {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }
        }
        HttpResponseMessage CXSales(string card, string storeNo, string posTerminal, string invoiceNo, int spendPoints, double billAmount, string orderNo, bool isOffline, double orderAmount, string source = "CX")
        {

            //cardNumber: cardNo or phoneNo

            var endPoint = $"{UrlCrownX.CXUrlSale}";
            long timeUni = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var phoneNumber = "";
            var cardNo = "";

            if (card.Substring(0, 4) == "6868")
            {
                cardNo = card;
            }
            else//PhoneNo
            {
                phoneNumber = card;
            }

            var modelPos = new CXSalesPOSRequest
            {
                CardNumber = card,
                StoreNo = storeNo,
                PosTerminal = posTerminal,
                InvoiceNo = invoiceNo,
                SpendPoints = spendPoints,
                BillAmount = billAmount,
                OrderNo = orderNo,
                IsOffline = isOffline,
                OrderAmount = orderAmount,
                Source = source
            };

            string requestPOS = JsonConvert.SerializeObject(modelPos);

            var listMerchant = new List<amountToEarn>
            {
                new amountToEarn
                {
                    loyaltyMerchantId = "VCM",
                    amount = billAmount
                }
            };

            var modelV2 = new PLSalePOSV2RequestModel
            {
                clubCode = clubCodeWin,
                cardNumber = phoneNumber,
                posCode = posTerminal,
                storeCode = storeNo,
                spendPoints = spendPoints,
                orderNo = orderNo,
                orderAmount = billAmount,
                amountToEarn = listMerchant

            };

            string jsonCX = JsonConvert.SerializeObject(modelV2);

            _modelLogCX = new CX_LogAPIModel
            {

                CardNumber = card,
                StoreNo = storeNo,
                POSTerminal = posTerminal,
                InvoiceNo = invoiceNo,
                ActionType = "CXSalesV2",
                RequestPOS = $"{requestPOS}",
                RequestXML = $"{jsonCX}",
                Source = source,
                DateTime = DateTime.Now
            };

            try
            {
                return CXSalesV2(modelV2);
            }
            catch (Exception ex)
            {
                var error = JsonConvert.SerializeObject(ex);
                _modelLogCX.ResponseXML = error;
                Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API CrownX: {error}",
                    Status = HttpStatusCode.InternalServerError
                });
            }


        }
        HttpResponseMessage CXRefund(string card, string storeNo, string posTerminal, string invoiceNo, string orderNo, string origOrderNo, int spendPoints, decimal refundAmount, bool isOffline, decimal orderAmount)
        {

            var endPoint = $"{UrlCrownX.CXUrlRefund}";
            long timeUni = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var modelPos = new CXRefundPOSRequest
            {
                CardNumber = card,
                StoreNo = storeNo,
                PosTerminal = posTerminal,
                InvoiceNo = invoiceNo,
                OrderNo = orderNo,
                OrigOrderNo = origOrderNo,
                SpendPoints = spendPoints,
                RefundAmount = refundAmount,
                IsOffline = isOffline,
                OrderAmount = orderAmount
            };

            string requestPOS = JsonConvert.SerializeObject(modelPos);
            var clubCodeSetup = clubCodeWin;

            var listRefundMerchant = new List<CXRefundAmount>
            {
                new CXRefundAmount
                {
                    LoyaltyMerchantId = "VCM",
                    Amount = (double)refundAmount

                }
            };

            var model = new CXRefundPOSV2Request
            {
                clubCode = clubCodeSetup,
                cardNumber = card,
                storeCode = storeNo,
                posCode = posTerminal,
                orderNo = invoiceNo,
                spendPoints = spendPoints,
                origOrderNo = origOrderNo,
                refundAmount = listRefundMerchant
            };
            string jsonCX = JsonConvert.SerializeObject(model);

            _modelLogCX = new CX_LogAPIModel
            {
                CardNumber = card,
                StoreNo = storeNo,
                POSTerminal = posTerminal,
                InvoiceNo = invoiceNo,
                ActionType = "CXRefundV2",
                RequestPOS = $"{requestPOS}",
                RequestXML = $"{jsonCX}",
                Source = "CX",
                DateTime = DateTime.Now
            };

            try
            {

                return CXRefundV2(model);
            }
            catch (Exception ex)
            {
                _modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {

                    Message = $"Lỗi hệ thống {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
        HttpResponseMessage CXExtraSales(ExtraSalePosRequestModel modelPOS)
        {

            var endPoint = $"{UrlCrownX.CXUrlBlueSale}";
            long timeUni = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string requestPOS = JsonConvert.SerializeObject(modelPOS);

            var phoneNumber = "";
            var cardNo = "";
            if (modelPOS.CardNumber.Substring(0, 4) == "6868")
            {
                cardNo = modelPOS.CardNumber;
            }
            else//PhoneNo
            {
                phoneNumber = modelPOS.CardNumber;
            }

            var model = new CXSalesBlueModel
            {
                cardNo = cardNo,
                phone = phoneNumber,
                transactionTime = timeUni,
                transactionType = "0",
                storeCode = modelPOS.StoreCode,
                posCode = modelPOS.PosTerminal,
                invoiceNo = modelPOS.OrderNo,
                totalBillAmount = (double)modelPOS.TotalBillAmount,
                billLines = modelPOS.BillLines.Select(x => new CXSalesBlueItemModel
                {
                    amount = x.Amount,
                    article = x.Article,
                    barcode = x.Barcode,
                    discountAmount = x.DiscountAmount,
                    lineAmount = x.LineAmount,
                    quantity = x.Quantity,
                    recordNo = (int)x.RecordNo,
                    salePrice = x.SalePrice,
                    uom = x.UOM
                }).ToList(),
            };
            string jsonCX = JsonConvert.SerializeObject(model);

            _modelLogCX = new CX_LogAPIModel
            {
                CardNumber = modelPOS.CardNumber,
                StoreNo = modelPOS.StoreCode,
                POSTerminal = modelPOS.PosTerminal,
                InvoiceNo = modelPOS.OrderNo,
                ActionType = "CXExtraSales",
                RequestPOS = $"{requestPOS}",
                RequestXML = $"{jsonCX}",
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
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                    var response = client.PostAsync(endPoint, new StringContent(jsonCX, Encoding.UTF8, "application/json")).Result;
                    var readData = response.Content.ReadAsStringAsync();
                    var resultData = JsonConvert.DeserializeObject<ResponseCrownX>(readData.Result);


                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    _modelLogCX.ResponseXML = JsonConvert.SerializeObject(resultData);
                    _modelLogCX.ResponseTotal = milliseconds;
                    Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var dataResult = JsonConvert.SerializeObject(resultData.data);
                        var modelSale = JsonConvert.DeserializeObject<CXSalesBlueResponse>(dataResult);

                        var tichdiemCampaign = modelSale.currencyRate == 0 ? 0 : Math.Abs((int)Math.Round(((double)(modelSale.extraEarnByCampaign) / (double)modelSale.currencyRate), 0));
                        var tichdiemItem = modelSale.currencyRate == 0 ? 0 : Math.Abs((int)Math.Round(((double)(modelSale.extraEarnByItems) / (double)modelSale.currencyRate), 0));
                        var result = new ExtraSalePOSResponse
                        {
                            OverQuota = modelSale.overQuota,
                            EmployCode = modelSale.employeeCode,
                            CompanyCode = modelSale.companyCode,
                            ExtraEarnByItems = tichdiemItem,// modelSale.extraEarnByItems,//Số điểm được tích trên giao dịch
                            ExtraEarnByCampaign = tichdiemCampaign,// modelSale.extraEarnByCampaign,//Số điểm được tích theo campaign (1%)                          
                            ListItem = modelSale.billLines.Select(a => new ExtraSaleItemPOSResponse
                            {
                                RecordNo = a.recordNo,
                                Article = a.article,
                                Barcode = a.barcode,
                                UOM = a.uom,
                                Quantity = a.quantity,
                                SalePrice = a.salePrice,
                                Amount = a.amount,
                                DiscountAmount = a.discountAmount,
                                LineAmount = a.lineAmount,
                                ExtraQuantityEarn = a.extraQuantityEarn,//Số sản phẩm được tích theo mã sản phẩm 
                                ExtraAmountEarn = modelSale.currencyRate == 0 ? 0 : (double)(a.extraAmountEarn) / (double)modelSale.currencyRate,// a.extraAmountEarn     //Số điểm được tích theo mã sản phẩm 

                            }).ToList()
                        };
                        var extralHeader = new CXExtralHeaderModel
                        {
                            StoreNo = modelPOS.StoreCode,
                            POSCode = modelPOS.PosTerminal,
                            OrderNo = modelPOS.OrderNo,
                            ReferenceNumber = modelPOS.ReferenceNumber,
                            TransactionTime = modelSale.transactionTime,
                            TransactionType = modelSale.transactionType,
                            CustomerName = modelPOS.CustomerName,
                            OverQuota = modelSale.overQuota,
                            EmployeeCode = modelSale.employeeCode,
                            CompanyCode = modelSale.companyCode,
                            TotalBillAmount = modelSale.totalBillAmount,
                            ExtraEarnByItems = tichdiemItem,// modelSale.extraEarnByItems,
                            ExtraEarnByCampaign = tichdiemCampaign,// modelSale.remainingCampaignQuota,
                            Rate = modelSale.currencyRate,
                            ActionAPI = "CXExtraSale",
                            CreatedDate = DateTime.Now
                        };
                        var extraItem = modelSale.billLines.Select(a => new CXExtralItemBillModel
                        {
                            ReceiptNumber = modelPOS.OrderNo,
                            RecordNo = a.recordNo,
                            Article = a.article,
                            Barcode = a.barcode,
                            UOM = a.uom,
                            Quantity = a.quantity,
                            SalePrice = a.salePrice,
                            Amount = a.amount,
                            DiscountAmount = a.discountAmount,
                            LineAmount = a.lineAmount,
                            ExtraQuantityEarn = a.extraQuantityEarn,
                            ExtraAmountEarn = modelSale.currencyRate == 0 ? 0 : (double)(a.extraAmountEarn) / (double)modelSale.currencyRate, //a.extraAmountEarn,

                        }).ToList();
                        Task.Run(() => _logVID.CXInsertExtral(extralHeader, extraItem));

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = result,
                            Message = "Thành công",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(response.StatusCode, new ResultResponse
                        {
                            Data = null,
                            Message = resultData.message,
                            Status = response.StatusCode
                        });
                    }
                }

                catch (Exception ex)
                {
                    _modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                    Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                    return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                    {
                        Data = null,
                        Message = $"Lỗi hệ thống API CrownX {ex.Message}",
                        Status = HttpStatusCode.InternalServerError
                    });
                }
            }
        }
        HttpResponseMessage CXExtraRefund(ExtraRefundPosRequestModel modelPOS)
        {
            var endPoint = $"{UrlCrownX.CXUrlBlueRefund}";
            long timeUni = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string requestPOS = JsonConvert.SerializeObject(modelPOS);

            var phoneNumber = "";
            var cardNo = "";

            if (modelPOS.CardNumber.Substring(0, 4) == "6868")
            {
                cardNo = modelPOS.CardNumber;
            }
            else//PhoneNo
            {
                phoneNumber = modelPOS.CardNumber;
            }

            var model = new CXRefundBlueModel
            {
                cardNo = cardNo,
                phone = phoneNumber,
                transactionTime = timeUni,
                transactionType = "1",
                storeCode = modelPOS.StoreCode,
                posCode = modelPOS.PosTerminal,
                originalInvoiceNo = modelPOS.OriginalPosNumber,
                invoiceNo = modelPOS.OrderNo,
                totalBillAmount = (double)modelPOS.RefundAmount,
                billLines = modelPOS.BillLines.Select(x => new CXRefundBlueItemModel
                {
                    amount = x.Amount,
                    article = x.Article,
                    barcode = x.Barcode,
                    discountAmount = x.DiscountAmount,
                    lineAmount = x.LineAmount,
                    quantity = x.RefundQuantity,
                    recordNo = (int)x.RecordNo,
                    salePrice = x.SalePrice,
                    uom = x.UOM,
                    originalRecordNo = (int)x.RecordNo

                }).ToList(),
            };
            string jsonCX = JsonConvert.SerializeObject(model);

            _modelLogCX = new CX_LogAPIModel
            {
                CardNumber = modelPOS.CardNumber,
                StoreNo = modelPOS.StoreCode,
                POSTerminal = modelPOS.PosTerminal,
                InvoiceNo = modelPOS.OrderNo,
                ActionType = "CXExtraRefund",
                RequestPOS = $"{requestPOS}",
                RequestXML = $"{jsonCX}",
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
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                    var response = client.PostAsync(endPoint, new StringContent(jsonCX, Encoding.UTF8, "application/json")).Result;
                    var readData = response.Content.ReadAsStringAsync();
                    var resultData = JsonConvert.DeserializeObject<ResponseCrownX>(readData.Result);
                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    _modelLogCX.ResponseXML = JsonConvert.SerializeObject(resultData);
                    _modelLogCX.ResponseTotal = milliseconds;
                    Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var dataResult = JsonConvert.SerializeObject(resultData.data);
                        var refundBlue = JsonConvert.DeserializeObject<CXRefundBlueResponse>(dataResult);

                        var tichdiemCampaign = refundBlue.currencyRate == 0 ? 0 : Math.Abs((int)Math.Round(((double)(refundBlue.extraEarnByCampaign) / (double)refundBlue.currencyRate), 0));
                        var tichdiemItem = refundBlue.currencyRate == 0 ? 0 : Math.Abs((int)Math.Round(((double)(refundBlue.extraEarnByItems) / (double)refundBlue.currencyRate), 0));

                        //var extraItem = new List<ExtratItemBillModel>();
                        var result = new ExtraRefundPOSResponse
                        {
                            CompanyCode = refundBlue.companyCode,
                            EmployCode = refundBlue.employeeCode,
                            RefundAmount = modelPOS.RefundAmount,
                            ExtraRefundByItems = tichdiemItem,// refundBlue.extraEarnByItems,
                            ExtraRefundByCampaign = tichdiemCampaign// refundBlue.extraEarnByCampaign
                        };

                        if (refundBlue.billLines != null && refundBlue.billLines.Count > 0)
                        {
                            result.ListItem = refundBlue.billLines.Select(a => new ExtraRefundtemPOSResponse
                            {
                                RecordNo = a.recordNo,
                                Article = a.article,
                                Barcode = a.barcode,
                                Quantity = a.quantity,
                                SalePrice = a.salePrice,
                                Amount = a.amount,
                                UOM = a.uom,
                                DiscountAmount = a.discountAmount,
                                LineAmount = a.lineAmount,
                                ExtraAmountEarn = refundBlue.currencyRate == 0 ? 0 : (float)(a.extraAmountEarn) / (float)refundBlue.currencyRate, //a.extraAmountEarn,                                                                                                                                                  
                                ExtraQuantityEarn = a.extraQuantityEarn

                            }).ToList();

                            //extraItem = refundBlue.billLines.Select(a => new ExtratItemBillModel
                            //{
                            //    RecordNo = a.recordNo,
                            //    Article = a.article,
                            //    Barcode = a.barcode,
                            //    UOM = a.uom,
                            //    Quantity = a.quantity,
                            //    SalePrice = a.salePrice,
                            //    Amount = a.amount,
                            //    DiscountAmount = a.discountAmount,
                            //    LineAmount = a.lineAmount,
                            //    ExtraQuantityRefund = a.extraQuantityEarn,
                            //    ExtraAmountRefund = refundBlue.currencyRate == 0 ? 0 : (float)(a.extraAmountEarn) / (float)refundBlue.currencyRate// a.extraAmountEarn

                            //}).ToList();
                        }

                        var extralHeader = new CXExtralHeaderModel
                        {
                            StoreNo = modelPOS.StoreCode,
                            POSCode = modelPOS.PosTerminal,
                            OrderNo = modelPOS.OrderNo,
                            ReferenceNumber = modelPOS.ReferenceNumber,
                            TransactionTime = refundBlue.transactionTime,
                            TransactionType = refundBlue.transactionType,
                            CustomerName = modelPOS.CustomerName,
                            CompanyCode = refundBlue.companyCode,
                            EmployeeCode = refundBlue.employeeCode,
                            TotalBillAmount = modelPOS.TotalBillAmount,
                            RefundAmount = modelPOS.RefundAmount,
                            ExtraRefundByItems = tichdiemItem,// refundBlue.extraEarnByItems,
                            ExtraRefundByCampaign = tichdiemCampaign,// refundBlue.extraEarnByCampaign,                             
                            OriginalReceiptNumber = modelPOS.OriginalReceiptNumber,
                            OriginalReferenceNumber = modelPOS.OriginalReferenceNumber,
                            OriginalPosNumber = modelPOS.OriginalPosNumber,
                            Rate = refundBlue.currencyRate,
                            ActionAPI = "CXExtraRefund",
                            CreatedDate = DateTime.Now
                        };

                        var extraBillItem = refundBlue.billLines.Select(a => new CXExtralItemBillModel
                        {
                            ReceiptNumber = modelPOS.OrderNo,
                            RecordNo = a.recordNo,
                            Article = a.article,
                            Barcode = a.barcode,
                            UOM = a.uom,
                            Quantity = modelPOS.BillLines.FirstOrDefault(d => d.RecordNo == a.recordNo) == null ? 0 : modelPOS.BillLines.FirstOrDefault(d => d.RecordNo == a.recordNo).Quantity,
                            SalePrice = a.salePrice,
                            Amount = a.amount,
                            RefundQuantity = a.quantity,
                            DiscountAmount = a.discountAmount,
                            LineAmount = a.lineAmount,
                            ExtraQuantityRefund = a.extraQuantityEarn,
                            ExtraAmountRefund = refundBlue.currencyRate == 0 ? 0 : (double)(a.extraAmountEarn) / (double)refundBlue.currencyRate// a.extraAmountEarn,

                        }).ToList();

                        Task.Run(() => _logVID.CXInsertExtral(extralHeader, extraBillItem));

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = result,
                            Message = "Thành công",
                            Status = HttpStatusCode.OK
                        });

                    }
                    else
                    {
                        return Request.CreateResponse(response.StatusCode, new ResultResponse
                        {
                            Data = null,
                            Message = resultData.message,
                            Status = response.StatusCode
                        });
                    }
                }
                catch (Exception ex)
                {
                    _modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                    Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                    return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                    {
                        Data = null,
                        Message = $"Lỗi hệ thống API CrownX {ex.Message}",
                        Status = HttpStatusCode.InternalServerError
                    });
                }
            }
        }   
        HttpResponseMessage CXSalesV2(PLSalePOSV2RequestModel modelPOS)
        {

            var endPoint = $"{UrlCrownX.CXUrlSale}";
            long timeUni = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (string.IsNullOrEmpty(modelPOS.storeCode))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "storeNo đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrEmpty(modelPOS.posCode))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "posTerminal đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (string.IsNullOrEmpty(modelPOS.orderNo))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "orderNo đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            var phoneNumber = "";
            var cardNo = "";

            //1.Số phone: 10 số và bắt đầu bởi số 0(==> để đảm bảo thì cần validate ở đầu end - user là khi nhập sẽ nhập sô 0 đầu tiên ạ)
            //2.Barcode: 17 số
            //3.Các trường hợp còn lại là số thẻ
            if ((modelPOS.cardNumber.Length == 10 && modelPOS.cardNumber.Substring(0, 1) == "0") || modelPOS.cardNumber.Length == 17)
            {
                phoneNumber = modelPOS.cardNumber;
            }
            else
            {
                cardNo = modelPOS.cardNumber;
            }
            string requestPOS = JsonConvert.SerializeObject(modelPOS);
            string merchantID = "VCM";

            var model = new PLSaleCXV2RequestModel
            {
                phoneNo = phoneNumber,
                cardNo = cardNo,
                invoiceNo = modelPOS.orderNo,
                posCode = modelPOS.posCode,
                storeCode = modelPOS.storeCode,
                spendPoints = modelPOS.spendPoints,
                totalBillAmount = modelPOS.orderAmount,
                transactionTime = timeUni,
                merchantId = merchantID,
                clubCode = clubCodeWin,
                //channelId = "POS_PLH",                
                // referenceInvoice = modelPOS.referenceInvoice, //Số hoá đơn tại POS để tham chiếu khi gửi 2 GD tích/tiêu điểm trên 1 GD bán hàng
                amountToEarn = modelPOS.amountToEarn

            };
            string jsonCX = JsonConvert.SerializeObject(model);

            _modelLogCX = new CX_LogAPIModel
            {
                CardNumber = modelPOS.cardNumber,
                StoreNo = modelPOS.storeCode,
                POSTerminal = modelPOS.posCode,
                InvoiceNo = modelPOS.orderNo,
                ActionType = "CXSalesV2",
                RequestPOS = $"{requestPOS}",
                RequestXML = $"{jsonCX}",
                Source = "CX",
                DateTime = DateTime.Now
            };

            //Chưa tích/tiêu điểm CX
            if(clubCodeWin.ToUpper() == "WIN")
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = new PointModel
                    {
                        PointEarn = 0,//Tích điểm
                        PointRedeem = 0,//(int)modelSale.spendPoints,//Tiêu điểm
                        CurrentRate = 0,
                        IsOfflineVinID = false,
                        EmpCode = "",
                        MasanerPackageInd = false,
                        StaffPercentage = 0,
                        NormCustPercentage = 0,
                        ExtraEarnByCampaign = null
                    },
                    Message = "Thành công",
                    Status = HttpStatusCode.OK
                });
            }

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
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                    var response = client.PostAsync(endPoint, new StringContent(jsonCX, Encoding.UTF8, "application/json")).Result;
                    var readData = response.Content.ReadAsStringAsync();
                    var responseCX = JsonConvert.SerializeObject(readData.Result);

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    _modelLogCX.ResponseTotal = milliseconds;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var resultData = JsonConvert.DeserializeObject<ResponseCrownX>(readData.Result);
                        _modelLogCX.ResponseXML = JsonConvert.SerializeObject(resultData);

                        Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                        var dataResult = JsonConvert.SerializeObject(resultData.data);
                        var modelSale = JsonConvert.DeserializeObject<CXSalesV2ModelResponse>(dataResult);

                        var listExtraEarnByCampaign = new List<PointEarnCampaint>();
                        if (modelSale.extraEarnByCampaign != null && modelSale.extraEarnByCampaign.Count > 0)
                        {
                            listExtraEarnByCampaign = modelSale.extraEarnByCampaign.Select(a => new PointEarnCampaint
                            {
                                LoyaltyMerchantId = a.loyaltyMerchantId,
                                Amount = a.amount,
                                EarnedPoints = a.earnedPoints
                            }).ToList();
                        }

                        int tichdiem = (listExtraEarnByCampaign == null || listExtraEarnByCampaign.Count == 0) ? 0 : (int)listExtraEarnByCampaign.Sum(d => d.EarnedPoints);

                        PointModel result = new PointModel
                        {
                            PointEarn = tichdiem,//Tích điểm
                            PointRedeem = modelPOS.spendPoints,//(int)modelSale.spendPoints,//Tiêu điểm
                            CurrentRate = (int)modelSale.currencyRate,
                            IsOfflineVinID = false,
                            EmpCode = string.IsNullOrEmpty(modelSale.empCode) ? "" : modelSale.empCode,
                            MasanerPackageInd = modelSale.masanerPackageInd,
                            StaffPercentage = modelSale.staffPercentage == null ? 0 : Convert.ToInt32(modelSale.staffPercentage),
                            NormCustPercentage = modelSale.normCustPercentage == null ? 0 : Convert.ToInt32(modelSale.normCustPercentage),
                            ExtraEarnByCampaign = listExtraEarnByCampaign
                        };

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = result,
                            Message = "Thành công",
                            Status = HttpStatusCode.OK
                        });
                    }

                    else
                    {
                        var resultData = JsonConvert.DeserializeObject<ResponseCrownX>(readData.Result);

                        _modelLogCX.ResponseXML = JsonConvert.SerializeObject(resultData);
                        Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                        var result = new ResultResponse
                        {
                            Data = null,
                            Message = resultData.message,
                            Status = HttpStatusCode.BadRequest
                        };
                        return Request.CreateResponse(HttpStatusCode.BadRequest, result);
                    }
                }

                catch (Exception ex)
                {
                    _modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                    Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                    return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                    {
                        Data = null,
                        Message = $"Lỗi hệ thống API CrownX {ex.Message}",
                        Status = HttpStatusCode.InternalServerError
                    });
                }
            }
        }
        HttpResponseMessage CXRefundV2(CXRefundPOSV2Request modelPOS)
        {
            var endPoint = $"{UrlCrownX.CXUrlRefund}";
            long timeUni = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var clubCodeSetup = clubCodeWin;
            var phoneNumber = "";
            var cardNo = "";
            var clubCode = clubCodeSetup;// string.IsNullOrEmpty(modelPOS.clubCode) ? "WIN" : modelPOS.clubCode;
            var merchantID = "VCM";

            if (string.IsNullOrEmpty(modelPOS.storeCode))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "storeNo đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrEmpty(modelPOS.posCode))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "posTerminal đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (modelPOS.refundAmount == null || modelPOS.refundAmount.Count == 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = "refundAmount đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }

            //1.Số phone: 10 số và bắt đầu bởi số 0(==> để đảm bảo thì cần validate ở đầu end - user là khi nhập sẽ nhập sô 0 đầu tiên ạ)
            //2.Barcode: 17 số
            //3.Các trường hợp còn lại là số thẻ
            if (modelPOS.cardNumber.Length == 10 || modelPOS.cardNumber.Length == 17)
            {
                phoneNumber = modelPOS.cardNumber;
            }
            else
            {
                cardNo = modelPOS.cardNumber;
            }

            string requestPOS = JsonConvert.SerializeObject(modelPOS);

            var model = new CXRefundV2ModelRequest
            {
                cardNo = cardNo,
                phoneNo = phoneNumber,
                invoiceNo = modelPOS.orderNo,
                originalInvoiceNo = modelPOS.origOrderNo,
                posCode = modelPOS.posCode,
                storeCode = modelPOS.storeCode,
                refundPoint = modelPOS.spendPoints,
                transactionTime = timeUni,
                merchantId = merchantID,
                clubCode = clubCode,
                refundAmount = modelPOS.refundAmount

            };
            string jsonCX = JsonConvert.SerializeObject(model);

            _modelLogCX = new CX_LogAPIModel
            {
                CardNumber = modelPOS.cardNumber,
                StoreNo = modelPOS.storeCode,
                POSTerminal = modelPOS.posCode,
                InvoiceNo = modelPOS.orderNo,
                ActionType = "CXRefundV2",
                RequestPOS = $"{requestPOS}",
                RequestXML = $"{jsonCX}",
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
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                    var response = client.PostAsync(endPoint, new StringContent(jsonCX, Encoding.UTF8, "application/json")).Result;
                    var readData = response.Content.ReadAsStringAsync();

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    _modelLogCX.ResponseTotal = milliseconds;
                    // logVID.WriteLogAPI_CX(modelLogCX);
                    var resultData = JsonConvert.DeserializeObject<ResponseCrownX>(readData.Result);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {

                        _modelLogCX.ResponseXML = JsonConvert.SerializeObject(resultData);
                        Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                        var dataResult = JsonConvert.SerializeObject(resultData.data);
                        var modelRefund = JsonConvert.DeserializeObject<CXRefundV2ModelResponse>(dataResult);
                        // var tichdiem = modelRefund.currencyRate == 0 ? 0 : Math.Abs((int)Math.Round(((double)(modelRefund.extraEarnByCampaign) / (double)modelRefund.currencyRate), 0));

                        var listextraEarnByCampaign = new List<PointEarnCampaint>();
                        if (modelRefund.extraEarnByCampaign != null && modelRefund.extraEarnByCampaign.Count > 0)
                        {
                            listextraEarnByCampaign = modelRefund.extraEarnByCampaign.Select(a => new PointEarnCampaint
                            {
                                LoyaltyMerchantId = a.loyaltyMerchantId,
                                Amount = a.amount,
                                EarnedPoints = Math.Abs(a.earnedPoints)
                            }).ToList();
                        }


                        //PointModel result = new PointModel
                        //{
                        //    //PointEarn = tichdiem,//(int)modelRefund.extraEarnByCampaign,//Tích điểm
                        //    //PointRedeem = spendPoints,//(int)modelRefund.refundPoint,//Tiêu điểm
                        //    CurrentRate = (int)modelRefund.currencyRate,
                        //    ExtraEarnByCampaign = listextraEarnByCampaign
                        //};

                        //int tichdiem = listextraEarnByCampaign.FirstOrDefault(d => d.LoyaltyMerchantId == "VCM") == null ? 0 : (int)listextraEarnByCampaign.FirstOrDefault(d => d.LoyaltyMerchantId == "VCM").EarnedPoints;
                        int tichdiem = (listextraEarnByCampaign == null || listextraEarnByCampaign.Count == 0) ? 0 : (int)listextraEarnByCampaign.Sum(d => d.EarnedPoints);

                        PointModel result = new PointModel
                        {
                            PointEarn = tichdiem,//Tích điểm
                            PointRedeem = modelPOS.spendPoints,//(int)modelSale.spendPoints,//Tiêu điểm
                            CurrentRate = (int)modelRefund.currencyRate,
                            IsOfflineVinID = false,
                            EmpCode = string.IsNullOrEmpty(modelRefund.empCode) ? "" : modelRefund.empCode,
                            MasanerPackageInd = modelRefund.masanerPackageInd,
                            StaffPercentage = modelRefund.staffPercentage == null ? 0 : Convert.ToInt32(modelRefund.staffPercentage),
                            NormCustPercentage = modelRefund.normCustPercentage == null ? 0 : Convert.ToInt32(modelRefund.normCustPercentage),
                            ExtraEarnByCampaign = listextraEarnByCampaign
                        };

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = result,
                            Message = "Thành công",
                            Status = HttpStatusCode.OK
                        });

                    }
                    else
                    {
                        _modelLogCX.ResponseXML = JsonConvert.SerializeObject(readData.Result);
                        Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Message = "" + resultData.message,
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }

                catch (Exception ex)
                {
                    _modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                    Task.Run(() => _logVID.WriteLogAPI_CX(_modelLogCX));

                    return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                    {
                        Message = $"Lỗi hệ thống API CrownX {ex.Message}",
                        Status = HttpStatusCode.InternalServerError
                    });
                }
            }
        }
      
    }
}
