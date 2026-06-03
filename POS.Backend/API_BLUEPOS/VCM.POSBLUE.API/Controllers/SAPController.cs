using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Capillary.Transaction;
using TCX.API.Common.Dtos.Capillary.Vouchers;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore;
using TCX.WebApiCore.AppServices.Coupon;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;
using VCM.POSBLUE.API.Helpers;
using VCM.POSBLUE.API.SAPReturnVoucher;
using VCM.POSBLUE.API.SAPVoucher;
using VCM.POSBLUE.API.Services;
using VCM.POSBLUE.Business.Common;
using VCM.POSBLUE.Business.SAP;
using VCM.POSBLUE.Model.Common;
using VCM.POSBLUE.Model.Dtos.DRW;
using VCM.POSBLUE.Model.Dtos.ROP;
using VCM.POSBLUE.Model.SAP;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api/sap")]
    public class SAPController : BaseController
    {
        readonly SI_VoucherSerialNo_OUTService _siVoucherOut;
        readonly VoucherSerialNoService _returnVoucher;
        private LogSAPModel ModelSAPLog { get; set; }
        private SAPBLO SapBLO { get; set; }
        private readonly CommonBLO _commonBLO;

        readonly string PLG_ChecVoucher = WebConfigurationManager.AppSettings["PLG_ChecVoucher"];
        readonly string PLG_CheckCoupon = WebConfigurationManager.AppSettings["PLG_CheckCoupon"];
        readonly string PLG_RedeemVoucher = WebConfigurationManager.AppSettings["PLG_RedeemVoucher"];
        readonly string PLG_RedeemCoupon = WebConfigurationManager.AppSettings["PLG_RedeemCoupon"];

        readonly string PLGUser = WebConfigurationManager.AppSettings["PLGUser"];
        readonly string PLGPassword = WebConfigurationManager.AppSettings["PLGPassword"];
        readonly string linkAPIPLH = WebConfigurationManager.AppSettings["linkAPIPLH"];
        private readonly ROPVoucherService _rOPVoucherService;
        private readonly UsingVoucherPartnerService _usingVoucherPartnerService;
        private readonly List<string> _listActionStatus = new List<string> { "SOLD", "RDM", "EXP" };
        private readonly KibanaService _kibanaService;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly LoyaltyCapillaryService _loyaltyCapillaryService;
        private readonly LoyaltyService _loyaltyService;
        private readonly LoyaltyRepository _loyaltyRepository;
        private readonly RedisManager _redisCacheService;
        private readonly IssueVoucherService _issueVoucherService;
        private readonly RedisManager _redisClusterManager;
        private readonly CouponCapillaryService _couponsCapillary;
        private readonly WinXService _winXService;
        private readonly int _maxLengthVoucherCapillary = 15;
        public SAPController()
        {
            _siVoucherOut = new SI_VoucherSerialNo_OUTService();
            _returnVoucher = new VoucherSerialNoService();
            var sapUser = WebConfigurationManager.AppSettings["SAPUser"];
            var sapPassword = WebConfigurationManager.AppSettings["SAPPassword"];
            _siVoucherOut.Credentials = new NetworkCredential(sapUser, sapPassword);
            _returnVoucher.Credentials = new NetworkCredential(sapUser, sapPassword);
            ModelSAPLog = new LogSAPModel();
            SapBLO = new SAPBLO();
            _commonBLO = new CommonBLO();
            _rOPVoucherService = new ROPVoucherService();
            _kibanaService = new KibanaService();
            _memoryCacheService = new MemoryCacheService();
            _usingVoucherPartnerService = new UsingVoucherPartnerService();
            _loyaltyCapillaryService = new LoyaltyCapillaryService();
            _redisCacheService = new RedisManager();
            _issueVoucherService = new IssueVoucherService();
            _redisClusterManager = new RedisManager();
            _couponsCapillary = new CouponCapillaryService();
            _loyaltyService = new LoyaltyService();
            _winXService = new WinXService();
            _loyaltyRepository = new LoyaltyRepository();
        }


        //E: ArticleType : ZCPN, ZVCN ___ MỚI
        //companyCode=PLG,WPH,ALL,isCardVoucher: voucher or prepaid,isVoucher:voucher or coupon cho PLH
        [HttpGet]
        [Route("CheckVoucherBySerials")]
        public HttpResponseMessage CheckVoucherBySerials([Required] string voucherNumber, [Required] string posTerminal = "", bool IsLog = true)
        {
            string siteNo = posTerminal.Substring(0, 4);
            try
            {
                var checkROP = GetROPConfigWebApi();
                if (checkROP != null)
                {
                    string[] lsVoucher = new string[] { voucherNumber };
                    var requestROP = new CheckVouchersROP
                    {
                        VoucherNumbers = lsVoucher,
                        Site = siteNo,
                        PosTerminal = posTerminal
                    };

                    var resultROP = _rOPVoucherService.CheckVoucherBySerials(requestROP);
                    if (resultROP.Item1)
                    {
                        if (resultROP.Item3 != null && resultROP.Item3.ActicleType == ArticleTypeEnum.ZPOS.ToString())
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = resultROP.Item3,
                                Message = voucherNumber + " không phải voucher, vui lòng kiểm tra lại",
                                Status = HttpStatusCode.BadRequest
                            });
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = resultROP.Item3,
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = resultROP.Item3,
                            Message = resultROP.Item2 ?? "Có lỗi API từ ROP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Chưa cấu hình WebApi, vui lòng liên hệ IT",
                        Status = HttpStatusCode.BadRequest
                    });
                }
            }
            catch (Exception ex)
            {
                if (IsLog)
                {
                    var logRqPOS = voucherNumber;
                    var logRqSAP = JsonConvert.SerializeObject(ex);
                    ModelSAPLog = new LogSAPModel
                    {
                        StoreNo = siteNo,
                        POSTerminal = posTerminal,
                        VoucherNumber = voucherNumber,
                        ActionType = "CheckVoucherBySerials",
                        RequestPOS = logRqPOS,
                        RequestSAP = logRqSAP,
                        ResponseSAP = JsonConvert.SerializeObject(ex),
                        CreatedDate = DateTime.Now
                    };
                    SapBLO.WriteLogSAP(ModelSAPLog);
                }

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống :{JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet]
        [Route("CheckVoucher")]
        public async Task<HttpResponseMessage> CheckVoucher([Required] string voucherNumber, 
                                                string siteNo = "", 
                                                string posTerminal = "", 
                                                string companyCode = "", 
                                                string isEmployee = "", 
                                                string partner = "", 
                                                string isCardVoucher = "", 
                                                int quantity = 1, 
                                                bool isVoucher = true, 
                                                string phoneNumber = "",
                                                bool IsLog = true)
        {
            try
            {
                //check dynamic voucher winx
                var isDynamicVoucher = ValidateHelper.IsDynamicVoucher(voucherNumber);
                if (isDynamicVoucher.Item1 && !string.IsNullOrEmpty(isDynamicVoucher.Item2) && isDynamicVoucher.Item2 == "WINX")
                {
                    var dataWinX = await _winXService.ResolveDynamicVouchers(_memoryCacheService, posTerminal, voucherNumber, phoneNumber);
                    if (string.IsNullOrEmpty(dataWinX.Item1))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = dataWinX.Item2,
                            Status = HttpStatusCode.BadRequest,
                            MessageTechnical = dataWinX.Item3
                        });
                    }
                    else
                    {
                        voucherNumber = dataWinX.Item1;
                    }
                }

                //check voucher on CAP
                var checkIsCapillary = ValidateHelper.IsVoucherCapillary(voucherNumber, partner);
                if (checkIsCapillary.Item1)
                {
                    string voucherCapillary = voucherNumber;
                    if (string.IsNullOrEmpty(phoneNumber))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = $"Vui lòng nhập số điện thoại hội viên WIN",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                                       
                    var dataCoupon = _couponsCapillary.ValidateCoupon(_loyaltyRepository, _redisClusterManager, _memoryCacheService, siteNo, posTerminal, phoneNumber, voucherCapillary, null, false);
                    if (dataCoupon.Item1)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = dataCoupon.Item3,
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = dataCoupon.Item3,
                            Message = dataCoupon.Item2 ?? "Lỗi kết nối Capillary",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }

                //voucher PLH
                if (!string.IsNullOrEmpty(companyCode))
                {
                    if (companyCode == "PLG")
                    {
                        var mappingStore = _commonBLO.MappingStoreWinlife(siteNo);
                        if (mappingStore == null)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = $"Không tìm thấy cửa hàng PHÚC LONG, Vui lòng liên hệ IT để setup",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                        if (isVoucher)
                        {

                            var isEmp = isEmployee == "X";

                            var listSerial = voucherNumber.Split(new char[] { ';', ',', '\n', '\r', ' ' }).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                            var listSerialModel = listSerial.Select(a => new PLH_SeriNoRequest
                            {
                                seriNo = a,
                                isEmployee = isEmp

                            }).ToList();

                            var model = new PLH_CheckAPIVoucherRequest
                            {
                                isWeb = false,
                                storeNo = siteNo,
                                posID = posTerminal,
                                partner = partner,//PLH,GotIt,URBOX,GiftBox
                                isVoucher = isCardVoucher == "X",//Voucher/PrepaidCard
                                listSeriNo = listSerialModel
                            };

                            var endPoint = $"{PLG_ChecVoucher}";
                            var jsonRequestAPI = JsonConvert.SerializeObject(model);

                            using (var client = new HttpClient())
                            {
                                try
                                {

                                    client.Timeout = TimeSpan.FromMinutes(1);
                                    client.BaseAddress = new Uri(linkAPIPLH);

                                    var byteArray = Encoding.ASCII.GetBytes($"{PLGUser}:{PLGPassword}");
                                    var authenBasic = Convert.ToBase64String(byteArray);

                                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                                    var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;

                                    var readData = response.Content.ReadAsStringAsync();
                                    var convertObject = JsonConvert.DeserializeObject<ResultResponse>(readData.Result);

                                    if (IsLog)
                                    {
                                        var logRqPOS = $"{linkAPIPLH}{endPoint}";
                                        var logRqSAP = jsonRequestAPI;
                                        ModelSAPLog = new LogSAPModel
                                        {
                                            StoreNo = siteNo,
                                            POSTerminal = posTerminal,
                                            VoucherNumber = voucherNumber,
                                            ActionType = "CheckVoucher",
                                            RequestPOS = logRqPOS,
                                            RequestSAP = logRqSAP,
                                            ResponseSAP = JsonConvert.SerializeObject(convertObject),
                                            CreatedDate = DateTime.Now
                                        };
                                        SapBLO.WriteLogSAP(ModelSAPLog);
                                    }

                                    if (response.StatusCode == HttpStatusCode.OK)
                                    {

                                        var dataJsonObject = JsonConvert.SerializeObject(convertObject.Data);
                                        var dataList = JsonConvert.DeserializeObject<List<PLH_InforVoucherResponse>>(dataJsonObject);
                                        var data = dataList.FirstOrDefault();

                                        var resultPLH = new VoucherStatusResponse();

                                        resultPLH = new VoucherStatusResponse
                                        {
                                            Status = data.DescVoucher,
                                            Return = data.StatusVoucher,
                                            ActicleNo = "",
                                            ActicleType = data.TypeVoucher,
                                            Value = data.Value.ToString().Trim().Replace(".", ""),
                                            VoucherNumber = voucherNumber,
                                            Voucher_Currency = "VNĐ",
                                            Validity_From_Date = "",
                                            Expiry_Date = "",
                                            CompanyCode = companyCode
                                        };

                                        if (data.StatusVoucher == "A")
                                        {
                                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                            {
                                                Data = resultPLH,
                                                Message = $"OK",
                                                Status = HttpStatusCode.OK
                                            });
                                        }
                                        else
                                        {
                                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                            {
                                                Data = resultPLH,
                                                Message = $"{data.DescVoucher}",
                                                Status = HttpStatusCode.BadRequest
                                            });
                                        }
                                    }
                                    else
                                    {
                                        return Request.CreateResponse(response.StatusCode, new ResultResponse
                                        {
                                            Data = null,
                                            Message = $"Lỗi {convertObject.Message}",
                                            Status = response.StatusCode
                                        });
                                    }

                                }
                                catch (Exception ex)
                                {
                                    if (IsLog)
                                    {
                                        var logRqPOS = $"{linkAPIPLH}{endPoint}";
                                        var logRqSAP = jsonRequestAPI;
                                        ModelSAPLog = new LogSAPModel
                                        {
                                            StoreNo = siteNo,
                                            POSTerminal = posTerminal,
                                            VoucherNumber = voucherNumber,
                                            ActionType = "CheckVoucher",
                                            RequestPOS = logRqPOS,
                                            RequestSAP = logRqSAP,
                                            ResponseSAP = JsonConvert.SerializeObject(ex),
                                            CreatedDate = DateTime.Now
                                        };
                                        SapBLO.WriteLogSAP(ModelSAPLog);
                                    }

                                    return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                                    {
                                        Data = null,
                                        Message = $"Lỗi hệ thống API VOUCHER BLUE POS {JsonConvert.SerializeObject(ex)}",
                                        Status = HttpStatusCode.InternalServerError
                                    });
                                }
                            }
                        }
                        else//Coupon
                        {
                            var isEmp = isEmployee == "X";

                            var listSerial = voucherNumber.Split(new char[] { ';', ',', '\n', '\r', ' ' }).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                            var listSerialModel = listSerial.Select(a => new SeriNoCouponRequest
                            {
                                seriNo = a,
                                quantity = quantity,
                                isEmployee = isEmp

                            }).ToList();

                            var model = new PLH_CheckCouponModel
                            {
                                isWeb = false,
                                storeNo = siteNo,
                                posID = posTerminal,
                                userPOS = "WCM",
                                listSeriNo = listSerialModel
                            };

                            var endPoint = $"{PLG_CheckCoupon}";
                            var jsonRequestAPI = JsonConvert.SerializeObject(model);
                            using (var client = new HttpClient())
                            {
                                try
                                {

                                    client.Timeout = TimeSpan.FromMinutes(2);
                                    client.BaseAddress = new Uri(linkAPIPLH);

                                    var byteArray = Encoding.ASCII.GetBytes($"{PLGUser}:{PLGPassword}");
                                    var authenBasic = Convert.ToBase64String(byteArray);

                                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                                    var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;

                                    var readData = response.Content.ReadAsStringAsync();

                                    var convertObject = JsonConvert.DeserializeObject<ResultResponse>(readData.Result);


                                    if (IsLog)
                                    {
                                        var logRqPOS = $"{linkAPIPLH}{endPoint}";
                                        var logRqSAP = jsonRequestAPI;
                                        ModelSAPLog = new LogSAPModel
                                        {
                                            StoreNo = siteNo,
                                            POSTerminal = posTerminal,
                                            VoucherNumber = voucherNumber,
                                            ActionType = "CheckVoucher",
                                            RequestPOS = logRqPOS,
                                            RequestSAP = logRqSAP,
                                            ResponseSAP = JsonConvert.SerializeObject(convertObject),
                                            CreatedDate = DateTime.Now
                                        };
                                        SapBLO.WriteLogSAP(ModelSAPLog);
                                    }


                                    var resultPLH = new VoucherStatusResponse();

                                    if (response.StatusCode == HttpStatusCode.OK)
                                    {
                                        var dataJsonObject = JsonConvert.SerializeObject(convertObject.Data);
                                        var dataList = JsonConvert.DeserializeObject<List<PLH_CheckCouponResponse>>(dataJsonObject);
                                        var data = dataList.FirstOrDefault();

                                        resultPLH = new VoucherStatusResponse
                                        {
                                            Status = data.DescCoupon,
                                            Return = data.StatusCoupon,
                                            ActicleNo = data.ItemNo,
                                            ActicleType = "CPN",
                                            Value = "" + data.Quantity,
                                            VoucherNumber = voucherNumber,
                                            Voucher_Currency = "VNĐ",
                                            Validity_From_Date = "",
                                            Expiry_Date = "",
                                            CompanyCode = companyCode,
                                            IsEmployee = data.IsEmployee
                                        };

                                        if (data.StatusCoupon == "A")
                                        {
                                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                            {
                                                Data = resultPLH,
                                                Message = $"OK",
                                                Status = HttpStatusCode.OK
                                            });
                                        }
                                        else
                                        {
                                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                            {
                                                Data = resultPLH,
                                                Message = $"{data.DescCoupon}",
                                                Status = HttpStatusCode.BadRequest
                                            });
                                        }
                                    }
                                    else
                                    {
                                        return Request.CreateResponse(response.StatusCode, new ResultResponse
                                        {
                                            Data = null,
                                            Message = $"Lỗi {convertObject.Message}",
                                            Status = response.StatusCode
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (IsLog)
                                    {
                                        var logRqPOS = $"{linkAPIPLH}{endPoint}";
                                        var logRqSAP = jsonRequestAPI;
                                        ModelSAPLog = new LogSAPModel
                                        {
                                            StoreNo = siteNo,
                                            POSTerminal = posTerminal,
                                            VoucherNumber = voucherNumber,
                                            ActionType = "CheckVoucher",
                                            RequestPOS = logRqPOS,
                                            RequestSAP = logRqSAP,
                                            ResponseSAP = JsonConvert.SerializeObject(ex),
                                            CreatedDate = DateTime.Now
                                        };
                                        SapBLO.WriteLogSAP(ModelSAPLog);
                                    }

                                    return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                                    {
                                        Data = null,
                                        Message = $"Lỗi hệ thống API BLUE POS {JsonConvert.SerializeObject(ex)}",
                                        Status = HttpStatusCode.InternalServerError
                                    });
                                }
                            }
                        }
                    }
                }

                //Olala
                if (StringHelper.ValidatePhoneNumber(voucherNumber))
                {
                    var resultCheck = _usingVoucherPartnerService.CheckVoucherByPhoneNumber(voucherNumber);
                    SaveLogToDB("CheckVoucher", posTerminal, voucherNumber, posTerminal, voucherNumber, JsonConvert.SerializeObject(resultCheck.Item3));
                    if (resultCheck.Item1)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = resultCheck.Item3,
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = resultCheck.Item3,
                            Message = resultCheck.Item2 ?? string.Format("Số điện thoại {0} không có mã giảm giá", voucherNumber),
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
                var checkROP = GetROPConfigWebApi();
                var prifixVoucherROP = _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX) != null ? _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX).Value : "";
                if (checkROP != null && IsCheckRequestROP(voucherNumber, prifixVoucherROP))
                {
                    string[] lsVoucher = new string[] { voucherNumber };
                    var requestROP = new CheckVouchersROP
                    {
                        VoucherNumbers = lsVoucher,
                        Site = siteNo,
                        PosTerminal = posTerminal
                    };

                    var resultROP = _rOPVoucherService.CheckVouchers(requestROP);
                    if (resultROP.Item1)
                    {
                        if (resultROP.Item3 != null && resultROP.Item3.ActicleType == ArticleTypeEnum.ZPOS.ToString())
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = resultROP.Item3,
                                Message = voucherNumber + " không phải voucher/coupon, vui lòng kiểm tra lại",
                                Status = HttpStatusCode.BadRequest
                            });
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = resultROP.Item3,
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = resultROP.Item3,
                            Message = resultROP.Item2 ?? "Có lỗi API từ ROP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
                else
                {
                    if (voucherNumber.Trim().Length > 20)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = $"Mã Voucher/Coupon {voucherNumber} không được lớn hơn 20 ký tự",
                            Status = HttpStatusCode.BadRequest
                        });
                    }

                    //CALL TO SAP
                    List<DT_NECPOS_VoucherSerialNo_InputInput> objRequestList = new List<DT_NECPOS_VoucherSerialNo_InputInput>();
                    var objectRequest = new DT_NECPOS_VoucherSerialNo_InputInput
                    {
                        Voucher_Type = "V",
                        Article_No = "",
                        SerialNo = voucherNumber,
                        Voucher_Value = "",
                        Voucher_Currency = "VND",
                        Validity_From_Date = "",
                        Expiry_Date = "",
                        Processing_Type = "C",//A create,C check, U update
                        Status = "C",
                        Bonus_Buy = "",
                        Site = ""
                    };
                    objRequestList.Add(objectRequest);
                    var objResponseList = _siVoucherOut.SI_VoucherSerialNo_OUT(objRequestList.ToArray()).ToList();
                    if (IsLog)
                    {
                        var logRqPOS = voucherNumber;
                        var logRqSAP = JsonConvert.SerializeObject(objRequestList);
                        ModelSAPLog = new LogSAPModel
                        {
                            StoreNo = siteNo,
                            POSTerminal = posTerminal,
                            VoucherNumber = voucherNumber,
                            ActionType = "CheckVoucher",
                            RequestPOS = logRqPOS,
                            RequestSAP = logRqSAP,
                            ResponseSAP = JsonConvert.SerializeObject(objResponseList),
                            CreatedDate = DateTime.Now
                        };
                        SapBLO.WriteLogSAP(ModelSAPLog);
                    }

                    if (objResponseList != null && objResponseList.Count() > 0)
                    {
                        var result = objResponseList[0];

                        if (result.Return.Trim() == "0")
                        {
                            if (result.Status != null && result.Status.Trim().ToUpper() == "RDM")
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = objResponseList == null ? null : new VoucherStatusResponse { Status = result.Status, Return = result.Return.Trim(), ActicleNo = result.Article_No.Trim(), ActicleType = result.Article_Type, Value = result.Voucher_Value.Trim(), VoucherNumber = voucherNumber, Voucher_Currency = result.Voucher_Currency, Validity_From_Date = result.Validity_From_Date, Expiry_Date = result.Expiry_Date, CompanyCode = result.CompanyCode },
                                    Message = "Mã Voucher/Coupon đã được sử dụng",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                            if (result.Status != null && result.Status.Trim().ToUpper() == "EXP")
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = objResponseList == null ? null : new VoucherStatusResponse { Status = result.Status, Return = result.Return.Trim(), ActicleNo = result.Article_No.Trim(), ActicleType = result.Article_Type, Value = result.Voucher_Value.Trim(), VoucherNumber = voucherNumber, Voucher_Currency = result.Voucher_Currency, Validity_From_Date = result.Validity_From_Date, Expiry_Date = result.Expiry_Date, CompanyCode = result.CompanyCode },
                                    Message = "Mã Voucher/Coupon đã bị hủy",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                            if (result.Status != null && result.Status.Trim().ToUpper() == "AVL")
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = objResponseList == null ? null : new VoucherStatusResponse { Status = result.Status, Return = result.Return.Trim(), ActicleNo = result.Article_No.Trim(), ActicleType = result.Article_Type, Value = result.Voucher_Value.Trim(), VoucherNumber = voucherNumber, Voucher_Currency = result.Voucher_Currency, Validity_From_Date = result.Validity_From_Date, Expiry_Date = result.Expiry_Date, CompanyCode = result.CompanyCode },
                                    Message = $"Mã Voucher/Coupon này ({voucherNumber}) chưa được kích hoạt ({result.Status})",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                            try
                            {
                                var checkDateExp = result.Expiry_Date.Replace(".", "/");//31/12/2020
                                DateTime DateExp = DateTime.ParseExact(checkDateExp, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                var dateNow = DateTime.Now.Date;
                                if (DateExp < dateNow)
                                {
                                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                    {
                                        Data = objResponseList == null ? null : new VoucherStatusResponse { Status = result.Status, Return = result.Return.Trim(), Value = result.Voucher_Value.Trim(), VoucherNumber = voucherNumber, Voucher_Currency = result.Voucher_Currency, Validity_From_Date = result.Validity_From_Date, Expiry_Date = result.Expiry_Date, CompanyCode = result.CompanyCode },
                                        Message = "Mã Voucher/Coupon đã hết hạn sử dụng",
                                        Status = HttpStatusCode.BadRequest
                                    });
                                }
                            }
                            catch (Exception ex)
                            {

                                if (IsLog)
                                {
                                    var logRqPOS = voucherNumber;
                                    var logRqSAP = JsonConvert.SerializeObject(objRequestList);
                                    ModelSAPLog = new LogSAPModel
                                    {
                                        StoreNo = siteNo,
                                        POSTerminal = posTerminal,
                                        VoucherNumber = voucherNumber,
                                        ActionType = "CheckVoucher",
                                        RequestPOS = logRqPOS,
                                        RequestSAP = logRqSAP,
                                        ResponseSAP = JsonConvert.SerializeObject(ex),
                                        CreatedDate = DateTime.Now
                                    };
                                    SapBLO.WriteLogSAP(ModelSAPLog);
                                }
                            }
                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = objResponseList == null ? null : new VoucherStatusResponse
                                {

                                    Status = result.Status,
                                    Return = result.Return.Trim(),
                                    ActicleNo = result.Article_No.Trim(),
                                    ActicleType = result.Article_Type,
                                    Value = result.Voucher_Value.Trim().Replace(".", ""),
                                    VoucherNumber = voucherNumber,
                                    Voucher_Currency = result.Voucher_Currency,
                                    Validity_From_Date = result.Validity_From_Date,
                                    Expiry_Date = result.Expiry_Date,
                                    CompanyCode = result.CompanyCode,
                                    Partner = "SAP"
                                },
                                Message = "Success",
                                Status = HttpStatusCode.OK
                            });
                        }
                        else
                        {
                            if (result.Return.Trim() == "E4")
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = objResponseList == null ? null : new VoucherStatusResponse { Status = result.Status, Return = result.Return.Trim(), ActicleNo = result.Article_No.Trim(), ActicleType = result.Article_Type, Value = result.Voucher_Value.Trim(), VoucherNumber = voucherNumber, Voucher_Currency = result.Voucher_Currency, Validity_From_Date = result.Validity_From_Date, Expiry_Date = result.Expiry_Date, CompanyCode = result.CompanyCode },
                                    Message = "Mã Voucher/Coupon không tồn tại",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }

                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = objResponseList == null ? null : new VoucherStatusResponse { Status = result.Status, Return = result.Return.Trim(), Value = result.Voucher_Value.Trim(), VoucherNumber = voucherNumber, Voucher_Currency = result.Voucher_Currency, Validity_From_Date = result.Validity_From_Date, Expiry_Date = result.Expiry_Date, CompanyCode = result.CompanyCode },
                                Message = result.Status,
                                Status = HttpStatusCode.BadRequest
                            });

                        }

                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = $"Mã Voucher/Coupon này {voucherNumber} không đúng",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (IsLog)
                {
                    var logRqPOS = voucherNumber;
                    var logRqSAP = JsonConvert.SerializeObject(ex);
                    ModelSAPLog = new LogSAPModel
                    {
                        StoreNo = siteNo,
                        POSTerminal = posTerminal,
                        VoucherNumber = voucherNumber,
                        ActionType = "CheckVoucher",
                        RequestPOS = NetworkHelper.GetHostName(),
                        RequestSAP = logRqSAP,
                        ResponseSAP = JsonConvert.SerializeObject(ex),
                        CreatedDate = DateTime.Now
                    };
                    SapBLO.WriteLogSAP(ModelSAPLog);
                }
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống :{JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("CreateNewVoucher")]
        public HttpResponseMessage CreateNewVoucher(List<CreateVoucherModel> model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = !string.IsNullOrEmpty(ModelState.Values.First().Errors[0].ErrorMessage.ToString())? ModelState.Values.First().Errors[0].ErrorMessage.ToString() : "Dữ liệu request không hợp lệ",
                    Status = HttpStatusCode.BadRequest
                });
            }
            try
            {
                if (model == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Dữ liệu đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => string.IsNullOrEmpty(x.VoucherNumber)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Voucher/Coupon Number đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => string.IsNullOrEmpty(x.From_Date)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "From Date đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => string.IsNullOrEmpty(x.Expiry_Date)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Expiry_Date đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => string.IsNullOrEmpty(x.SiteCode)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Sitecode đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => string.IsNullOrEmpty(x.Article_No)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Mã article đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => string.IsNullOrEmpty(x.BonusBuy)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "BonusBuy đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => x.Value <= 0))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Value không hợp lệ",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => x.VoucherNumber.Length > 20))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã Voucher/Coupon không được lớn hơn 20 ký tự",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                //Check voucher ROP or SAP
                var prifixVoucherROP = _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX) != null ? _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX).Value : "";
                if (!IsValidateVoucherROP(model.Select(x => x.VoucherNumber).ToArray(), prifixVoucherROP))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Danh sách voucher/coupon không đồng nhất",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                //PROCESSING
                var checkROP = GetROPConfigWebApi();
                if (checkROP != null && IsUpdateCreateRequestROP(model.Select(x=>x.VoucherNumber).ToArray(), prifixVoucherROP))
                {

                    string voucherType = ArticleTypeEnum.ZVCN.ToString();
                    if(StringHelper.Right(StringHelper.Left(model.FirstOrDefault().VoucherNumber,7), 1) == "3")
                    {
                        voucherType = ArticleTypeEnum.ZCPN.ToString();
                    }

                    var resultROP = _rOPVoucherService.CreateVoucher(model, voucherType, (int)VoucherROPStatusEnum.Actived);
                    if (resultROP.Item1)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = resultROP.Item3,
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = resultROP.Item2 ?? "Có lỗi API từ ROP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
                else
                {
                    List<DT_NECPOS_VoucherSerialNo_InputInput> objRequestList = new List<DT_NECPOS_VoucherSerialNo_InputInput>();
                    for (int i = 0; i < model.Count; i++)
                    {
                        var serialNo = model[i].VoucherNumber;

                        if (!string.IsNullOrEmpty(serialNo))
                        {
                            var objectRequest = new DT_NECPOS_VoucherSerialNo_InputInput
                            {
                                Voucher_Type = "V",
                                Article_No = model[i].Article_No,
                                SerialNo = model[i].VoucherNumber,
                                Voucher_Value = model[i].Value.ToString(),
                                Voucher_Currency = "VND",
                                Validity_From_Date = model[i].From_Date,
                                Expiry_Date = model[i].Expiry_Date,
                                Processing_Type = "A",//A create,C check, U update
                                Status = "A",
                                Bonus_Buy = model[i].BonusBuy,
                                Site = model[i].SiteCode
                            };
                            objRequestList.Add(objectRequest);
                        }
                    }

                    var objResponseList = _siVoucherOut.SI_VoucherSerialNo_OUT(objRequestList.ToArray()).ToList();
                    
                    _kibanaService.LogResponse("CreateNewVoucher",model.FirstOrDefault().POSTerminal,0,"",JsonConvert.SerializeObject(objResponseList));
                    var logRqPOS = JsonConvert.SerializeObject(model);
                    var logRqSAP = JsonConvert.SerializeObject(objRequestList);
                    ModelSAPLog = new LogSAPModel
                    {
                        StoreNo = model.FirstOrDefault()?.SiteCode,
                        POSTerminal = model.FirstOrDefault()?.POSTerminal,
                        VoucherNumber = model.FirstOrDefault()?.VoucherNumber,
                        ActionType = "CreateNewVoucher",
                        RequestPOS = logRqPOS,
                        RequestSAP = logRqSAP,
                        ResponseSAP = JsonConvert.SerializeObject(objResponseList),
                        CreatedDate = DateTime.Now
                    };
                    SapBLO.WriteLogSAP(ModelSAPLog);

                    if (objResponseList != null && objResponseList.Count() > 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = objResponseList?.Select(x => new VoucherStatusResponse { Status = x.Status, Return = x.Return.Trim() }).ToList(),
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });

                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = "Không có phản hồi từ SAP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
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
        }

        [HttpPost]
        [Route("UpdateVoucher")]
        public HttpResponseMessage UpdateVoucherAsync(List<VoucherUpdateRequest> model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = ModelState.Values.First().Errors[0].ErrorMessage.ToString(),
                    Status = HttpStatusCode.BadRequest
                });
            }
            try
            {
                var listCompanyCode = model.GroupBy(d => d.CompanyCode, (key, g) => new { companyCode = key, ListData = g.ToList() }).ToList();
                if (listCompanyCode.Count > 1)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Vui lòng thanh toán voucher/coupon trên cùng 1 companyCode",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                //Check voucher ROP or SAP
                var prifixVoucherROP = _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX) != null ? _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX).Value : "";
                if (!IsValidateVoucherROP(model.Select(x => x.VoucherNumber).ToArray(), prifixVoucherROP))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Danh sách voucher/coupon không đồng nhất",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                //PROCESSING TO ROP
                var checkROP = GetROPConfigWebApi();
                if (checkROP != null && IsUpdateCreateRequestROP(model.Select(x => x.VoucherNumber).ToArray(), prifixVoucherROP))
                {
                    List<InsertVoucherROP> insertVoucherROPs = new List<InsertVoucherROP>();
                    for (int i = 0; i < model.Count; i++)
                    {
                        var serialNo = model[i].VoucherNumber;
                    }
                    var lstSeriNo = new List<VoucherUpdateSerial>();
                    foreach(var item in model)
                    {
                        lstSeriNo.Add(new VoucherUpdateSerial()
                        {
                            Partner ="",
                            CompanyCode = item.CompanyCode,
                            isVoucher = false,
                            articleNo = item.ArticleNo,
                            articleType = item.ArticleType,
                            value = 0,
                            status = item.Status,
                            voucherNumber = item.VoucherNumber
                        });
                    }

                    var request = new VoucherUpdateModel()
                    {
                        SiteCode = model.FirstOrDefault().SiteCode,
                        POSTerminal = model.FirstOrDefault().POSTerminal,
                        IsVoucher = false,
                        OrderNo="",
                        TotalBill = 0,
                        ListSeriNo = lstSeriNo
                    };

                    var resultROP = _rOPVoucherService.UpdateVoucherStatus(request, _redisClusterManager);
                    if (resultROP.Item1)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = resultROP.Item3 ?? null,
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = resultROP.Item3??null,
                            Message = resultROP.Item2 ?? "Có lỗi API từ ROP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
                else
                {
                    if (model == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = "Dữ liệu đang bị trống",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    if (model.Any(x => string.IsNullOrEmpty(x.Status)))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = "Trạng thái không được trống.",
                            Message = "Thất bại",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    if (model.Any(x => !_listActionStatus.Contains(x.Status)))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = "Trạng thái không hợp lệ.",
                            Message = "Thất bại",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    if (model.Any(x => string.IsNullOrEmpty(x.VoucherNumber)))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = "Voucher/Coupon không được trống.",
                            Message = "Thất bại",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    if (model.Any(x => string.IsNullOrEmpty(x.ArticleNo)))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = "ArticleNo không được trống.",
                            Message = "Thất bại",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    if (model.Any(x => string.IsNullOrEmpty(x.ArticleType)))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = "ActicleType không được trống.",
                            Message = "Thất bại",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    if (model.Any(x => x.VoucherNumber.Length > 20))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = $"Mã Voucher/Coupon không được lớn hơn 20 ký tự",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    #region CHAY BLUEPOS COMMENT SAP
                    if (model.FirstOrDefault().ArticleType == "ZVCN" || model.FirstOrDefault().ArticleType == "ZCPN")
                    {
                        List<DT_NECPOS_VoucherSerialNo_InputInput> objRequestList = new List<DT_NECPOS_VoucherSerialNo_InputInput>();
                        for (int i = 0; i < model.Count; i++)
                        {
                            var serialNo = model[i].VoucherNumber;

                            if (!string.IsNullOrEmpty(serialNo))
                            {
                                var objectRequest = new DT_NECPOS_VoucherSerialNo_InputInput
                                {
                                    Voucher_Type = "V",
                                    Article_No = model[i].ArticleNo,
                                    SerialNo = model[i].VoucherNumber,
                                    Voucher_Value = "",
                                    Voucher_Currency = "",
                                    Validity_From_Date = "",
                                    Expiry_Date = "",
                                    Processing_Type = "U",//A create,C check, U update
                                                          //Status = "RDM",//EXP
                                    Status = model[i].Status,//"RDM","EXP"
                                    Bonus_Buy = "",
                                    Site = ""
                                };
                                objRequestList.Add(objectRequest);
                            }
                        }
                        var objResponseList = _siVoucherOut.SI_VoucherSerialNo_OUT(objRequestList.ToArray()).ToList();

                        var logRqPOS = JsonConvert.SerializeObject(model);
                        var logRqSAP = JsonConvert.SerializeObject(objRequestList);

                        ModelSAPLog = new LogSAPModel
                        {
                            StoreNo = model.FirstOrDefault()?.SiteCode,
                            POSTerminal = model.FirstOrDefault()?.POSTerminal,
                            VoucherNumber = model.FirstOrDefault()?.VoucherNumber,
                            ActionType = "UpdateVoucher",
                            RequestPOS = logRqPOS,
                            RequestSAP = logRqSAP,
                            ResponseSAP = JsonConvert.SerializeObject(objResponseList),
                            CreatedDate = DateTime.Now
                        };
                        SapBLO.WriteLogSAP(ModelSAPLog);

                        if (objResponseList != null && objResponseList.Count() > 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = objResponseList?.Select(x => new VoucherStatusResponse
                                {
                                    Status = x.Status,
                                    Return = x.Return.Trim(),
                                    Value = x.Voucher_Value.Trim(),
                                    Voucher_Currency = x.Voucher_Currency,
                                    Expiry_Date = x.Expiry_Date
                                }).ToList(),
                                Message = "Success",
                                Status = HttpStatusCode.OK
                            });
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = "Không có phản hồi từ SAP",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                    }
                    else //Danh cho VOUCHER OLD
                    {
                        List<VoucherSerialNo_InputInput> objRequestList = new List<VoucherSerialNo_InputInput>();

                        for (int i = 0; i < model.Count; i++)
                        {
                            var serialNo = model[i].VoucherNumber;

                            if (!string.IsNullOrEmpty(serialNo))
                            {
                                var objectRequest = new VoucherSerialNo_InputInput
                                {
                                    Voucher_Type = "V",
                                    Article_No = model[i].ArticleNo,
                                    SerialNo = model[i].VoucherNumber,
                                    Voucher_Value = "",
                                    Voucher_Currency = "",
                                    Validity_From_Date = "",
                                    Expiry_Date = "",
                                    Processing_Type = "U",//A create,C check, U update
                                                          //Status = "RDM",
                                    Status = model[i].Status,//RDM,EXP
                                    Site = ""
                                };
                                objRequestList.Add(objectRequest);
                            }
                        }
                        var objResponseList = _returnVoucher.VoucherSerialNo(objRequestList.ToArray()).ToList();

                        var logRqPOS = JsonConvert.SerializeObject(model);
                        var logRqSAP = JsonConvert.SerializeObject(objRequestList);
                        ModelSAPLog = new LogSAPModel
                        {
                            StoreNo = model.FirstOrDefault()?.SiteCode,
                            POSTerminal = model.FirstOrDefault()?.POSTerminal,
                            VoucherNumber = model.FirstOrDefault()?.VoucherNumber,
                            ActionType = "UpdateVoucher",
                            RequestPOS = logRqPOS,
                            RequestSAP = logRqSAP,
                            ResponseSAP = JsonConvert.SerializeObject(objResponseList),
                            CreatedDate = DateTime.Now
                        };
                        SapBLO.WriteLogSAP(ModelSAPLog);

                        if (objResponseList != null && objResponseList.Count() > 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = objResponseList?.Select(x => new VoucherStatusResponse
                                {
                                    Status = x.Status,
                                    Return = x.Return.Trim(),
                                    Value = x.Voucher_Value,
                                    Voucher_Currency = x.Voucher_Currency,
                                    Expiry_Date = x.Expiry_Date
                                }).ToList(),
                                Message = "Success",
                                Status = HttpStatusCode.OK
                            });
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = "Không có phản hồi từ SAP",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống:{JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("winlife/redeemCpnVch")]
        public async Task<HttpResponseMessage> RedeemCpnVch(VoucherUpdateModel model)
        {
            bool isRedeem = false;
            var resultVoucherRedeem = new List<VoucherStatusResponse>();
            var resultVoucherRedeemError = new List<string>();
            int qtyVoucher = model.ListSeriNo != null ? model.ListSeriNo.Count : 0;
            int qtyPayment = 0;
            List<string> lstVoucherUsed = new List<string>();
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                string voucherMapping = model.ListSeriNo.FirstOrDefault().voucherNumber;
                if (StringHelper.ValidatePhoneNumber(voucherMapping))
                {
                    if (model.ListSeriNo.FirstOrDefault().articleNo == "11111111")
                    {
                        var staffDiscountInfo = new UpdateStatusSfaffDiscountDto()
                        {
                            PosNo = model.POSTerminal,
                            OrderNo = model.OrderNo,
                            StaffPhone = model.ListSeriNo.FirstOrDefault().voucherNumber,
                            Id = 0
                        };

                        var rsp = _usingVoucherPartnerService.UpdateStatusSfaffPhone_DRW(staffDiscountInfo);

                        return Request.CreateResponse(rsp.Status, new ResultResponse
                        {
                            Data = rsp.Data,
                            Message = rsp.Message,
                            Status = rsp.Status
                        });
                    }

                    var resultCheck = _usingVoucherPartnerService.CheckVoucherByPhoneNumber(voucherMapping);
                    if (resultCheck.Item1)
                    {
                        model.ListSeriNo.FirstOrDefault().voucherNumber = resultCheck.Item3.VoucherNumber;
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = resultCheck.Item3,
                            Message = resultCheck.Item2 ?? string.Format("Số điện thoại {0} không có mã giảm giá", voucherMapping),
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }

                //Check voucher ROP or SAP
               var prifixVoucherROP = _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX) != null ? _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX).Value : "";
                //TO ROP or SAP
                var checkROP = GetROPConfigWebApi();
                var listVoucherROP = model.ListSeriNo.Where(x => x.voucherNumber.Length > _maxLengthVoucherCapillary).ToList();
                if (listVoucherROP != null && listVoucherROP.Count > 0 && IsUpdateCreateRequestROP(listVoucherROP.Select(x => x.voucherNumber).ToArray(), prifixVoucherROP))
                {
                    //retry redeem voucher
                    if (model.IsRetry)
                    {
                        var dataVoucherRetry = _rOPVoucherService.ReTryUpdateVoucherStatus(model.OrderNo, _redisCacheService);
                        if (dataVoucherRetry != null)
                        {
                            var arrayA = model.ListSeriNo.Select(x => x.voucherNumber).ToArray();
                            var arrayB = dataVoucherRetry.Select(x => x.VoucherNumber).ToArray();
                            if (arrayA.OrderBy(x => x).SequenceEqual(arrayB.OrderBy(x => x)))
                            {
                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Data = dataVoucherRetry,
                                    Message = string.Format("Bill {0} retry sử dụng voucher thành công", model.OrderNo),
                                    Status = HttpStatusCode.OK
                                });
                            }
                            else
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = null,
                                    Message = string.Format("Số lượng voucher retry không khớp với request của bill {0}", model.OrderNo),
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }
                    }
                    var resultROP = _rOPVoucherService.UpdateVoucherStatus(model, _redisCacheService);
                    if (resultROP.Item1)
                    {
                        //UPDATE STATUS VOUCHER O'lala on ROP
                        if (StringHelper.ValidatePhoneNumber(voucherMapping))
                        {
                            await _usingVoucherPartnerService.UpdateStatusVoucherPAR(model);
                        }
                        isRedeem = true;
                        resultVoucherRedeem.AddRange(resultROP.Item3);
                        lstVoucherUsed.AddRange(resultROP.Item3.Select(x => x.VoucherNumber).ToList());
                        qtyPayment += resultVoucherRedeem.Count();
                    }
                    else
                    {
                        isRedeem = false;
                        if(resultROP.Item3 != null)
                        {
                            foreach (var item in resultROP.Item3)
                            {
                                resultVoucherRedeemError.Add(item.VoucherNumber);
                            }
                        }
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = resultROP.Item3 ?? null,
                            Message = resultROP.Item2 ?? "Có lỗi API từ ROP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }

                //========================
                //redeem voucher Capillary
                var listVoucherCAP = model.ListSeriNo.Where(x => x.voucherNumber.Length <= _maxLengthVoucherCapillary && ValidateHelper.IsVoucherCapillary(x.voucherNumber, x.Partner).Item1 == true).ToList();
                if (listVoucherCAP != null && listVoucherCAP.Count > 0)
                {
                    if (string.IsNullOrEmpty(model.PhoneNumber))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = $"Vui lòng nhập số điện thoại hội viên WIN",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    UpdateStatusVoucherPartnerRequest redeemVoucherCap = new UpdateStatusVoucherPartnerRequest
                    {
                        Partner = "CAP",
                        PosNo = model.POSTerminal,
                        StoreNo = model.SiteCode,
                        PhoneNumber = model.PhoneNumber,
                        Items = null,
                        OrderNo = model.OrderNo,
                    };
                    List<LstVoucherPartner> serial = new List<LstVoucherPartner>();
                    foreach (var item in listVoucherCAP)
                    {
                        if (item.voucherNumber.Length >= 7 && item.voucherNumber.Length <= 15 && ValidateHelper.IsVoucherCapillary(item.voucherNumber, item.Partner).Item1)
                        {
                            serial.Add(new LstVoucherPartner()
                            {
                                Code = item.voucherNumber,
                                Amount = (decimal)item.value,
                                Description = ""
                            });
                        }
                    }
                    redeemVoucherCap.SerialNo = serial;
                    var resultRedeem = _loyaltyService.RedeemCouponCapillary(_redisClusterManager, _memoryCacheService, redeemVoucherCap, false);
                    if (resultRedeem != null && resultRedeem.Status == HttpStatusCode.OK)
                    {
                        foreach (var v in redeemVoucherCap.SerialNo)
                        {
                            lstVoucherUsed.Add(v.Code);
                            resultVoucherRedeem.Add(new VoucherStatusResponse()
                            {
                                Return = "0",
                                Status = VoucherStatusEnum.RDM.ToString(),
                                VoucherNumber = v.Code
                            });
                        }
                        isRedeem = true;
                        qtyPayment += redeemVoucherCap.SerialNo.Count;
                    }
                    else
                    {
                        isRedeem = false;
                        foreach (var item in redeemVoucherCap.SerialNo)
                        {
                            resultVoucherRedeemError.Add(item.Code);
                        }
                        return Request.CreateResponse(resultRedeem?.Status ?? HttpStatusCode.BadRequest, new ResultResponse
                        {
                            MessageTechnical = $"Voucher Capillary lỗi thanh toán {JsonConvert.SerializeObject(resultVoucherRedeemError)}",
                            Data = null,
                            Message = resultRedeem?.Message ?? "Có lỗi trong quá trình redeem voucher",
                            Status = resultRedeem?.Status ?? HttpStatusCode.BadRequest
                        });
                    }
                }
                
                //SAP
                if (qtyPayment < qtyVoucher)
                {
                    var listCompanyCode = model.ListSeriNo.Where(x=>!lstVoucherUsed.Contains(x.voucherNumber)).GroupBy(d => d.CompanyCode, (key, g) => new { companyCode = key, ListData = g.ToList() }).ToList();
                    if (listCompanyCode.Count > 0)
                    {
                        var resultFinal = new List<VoucherStatusResponse>();
                        if (listCompanyCode.Any(d => d.companyCode != "PLG"))
                        {
                            if (listCompanyCode.Where(d => d.companyCode != "PLG").Any(x => x.ListData.Any(a => string.IsNullOrEmpty(a.articleNo))))
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = null,
                                    Message = "ArticleNo không được trống",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }

                            if (listCompanyCode.Where(d => d.companyCode != "PLG").Any(x => x.ListData.Any(a => string.IsNullOrEmpty(a.articleType))))
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = null,
                                    Message = "ActicleType không được trống.",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                            if (listCompanyCode.Where(d => d.companyCode != "PLG").Any(x => x.ListData.Any(a => a.voucherNumber.Length > 20)))
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = null,
                                    Message = $"Mã Voucher/Coupon không được lớn hơn 20 ký tự",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }

                        var mappingStore = new StoreNoMappingModel();
                        if (listCompanyCode.Any(d => d.companyCode == "PLG"))
                        {
                            mappingStore = _commonBLO.MappingStoreWinlife(model.SiteCode);
                            if (mappingStore == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = null,
                                    Message = $"Không tìm thấy cửa hàng PHÚC LONG, Vui lòng liên hệ IT để setup",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }

                        foreach (var itemCompany in listCompanyCode)
                        {
                            var companyCode = itemCompany.companyCode;
                            var listData = itemCompany.ListData.Where(x=> !lstVoucherUsed.Contains(x.voucherNumber)).ToList();
                            var resultData = new List<VoucherStatusResponse>();
                            if (companyCode == "PLG")
                            {
                                if (model.IsVoucher)//Voucher
                                {
                                    var endPoint = $"{PLG_RedeemVoucher}";
                                    try
                                    {
                                        var listPartner = itemCompany.ListData.GroupBy(d => d.Partner, (key, g) => new { partnerCode = key, ListData = g.ToList() }).ToList();
                                        if (listPartner.Count > 0)
                                        {
                                            foreach (var itemPartner in listPartner)
                                            {
                                                using (var client = new HttpClient())
                                                {
                                                    var modelPLH = new PLH_RedeemVoucherModel
                                                    {
                                                        partner = itemPartner.partnerCode,
                                                        orderNo = model.OrderNo,
                                                        storeNo = mappingStore.StoreNoPLH,
                                                        posID = model.POSTerminal,
                                                        totalBill = model.TotalBill,
                                                        staffCode = "WCM",
                                                        listSeriNo = itemPartner.ListData.Select(a => new PLH_RedeemSeriNo
                                                        {
                                                            seriNo = a.voucherNumber,
                                                            isVoucher = a.isVoucher,
                                                            value = a.value

                                                        }).ToList()
                                                    };

                                                    var jsonRequestAPI = JsonConvert.SerializeObject(modelPLH);
                                                    client.Timeout = TimeSpan.FromMinutes(1);
                                                    client.BaseAddress = new Uri(linkAPIPLH);

                                                    var byteArray = Encoding.ASCII.GetBytes($"{PLGUser}:{PLGPassword}");
                                                    var authenBasic = Convert.ToBase64String(byteArray);

                                                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                                                    var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                                                    var readData = response.Content.ReadAsStringAsync();
                                                    var convertObject = StringHelper.StringToObject<ResultResponse>(readData.Result);
                                                    FileHelper.WriteLogs($"RedeemVoucher.PhucLong: {convertObject}");
                                                    if (convertObject.Status == HttpStatusCode.OK)
                                                    {
                                                        var dataJsonObject = JsonConvert.SerializeObject(convertObject.Data);
                                                        var dataList = StringHelper.StringToObject<List<PLH_RedeemResponse>>(dataJsonObject);
                                                        if(dataList == null)
                                                        {
                                                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                                            {
                                                                MessageTechnical = $"Lỗi thanh toán voucher Phúc Long {JsonConvert.SerializeObject(modelPLH.listSeriNo)}",
                                                                Data = null,
                                                                Message = "Lỗi thanh toán voucher Phúc Long",
                                                                Status = HttpStatusCode.BadRequest
                                                            });
                                                        }
                                                        resultData = dataList.Select(a => new VoucherStatusResponse
                                                        {
                                                            Status = a.Message,
                                                            VoucherNumber = a.SeriNo,
                                                            ActicleNo = "",
                                                            ActicleType = "",
                                                            Value = a.Amount.ToString().Trim().Replace(".", ""),
                                                            Return = a.StatusVoucher == true ? "0" : "E",
                                                            CompanyCode = companyCode,
                                                            Partner = itemPartner.partnerCode

                                                        }).ToList();
                                                    }
                                                    else
                                                    {
                                                        resultData = new List<VoucherStatusResponse>{new VoucherStatusResponse
                                                        {
                                                            Status = convertObject.Message,
                                                            VoucherNumber =string.Join(",", itemPartner.ListData.Select(a=>a.voucherNumber)),
                                                            ActicleNo = "",
                                                            ActicleType = "",
                                                            Value = "0",
                                                            Return = "E",
                                                            CompanyCode = companyCode,
                                                            Partner=itemPartner.partnerCode

                                                        }};
                                                        isRedeem = false;
                                                    }
                                                    resultFinal.AddRange(resultData);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            isRedeem = false;
                                            resultData = new List<VoucherStatusResponse>{new VoucherStatusResponse
                                                    {
                                                        Status = $"Không tìm thấy partner",
                                                        VoucherNumber =string.Join(",", listData.Select(a=>a.voucherNumber)),
                                                        ActicleNo = "",
                                                        ActicleType = "",
                                                        Value = "0",
                                                        Return = "E",
                                                        CompanyCode = companyCode

                                                    }};
                                            resultFinal.AddRange(resultData);
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        resultData = new List<VoucherStatusResponse>{new VoucherStatusResponse
                                        {
                                            Status = ex.Message,
                                            VoucherNumber =string.Join(",", listData.Select(a=>a.voucherNumber)),
                                            ActicleNo = "",
                                            ActicleType = "",
                                            Value = "0",
                                            Return = "E",
                                            CompanyCode = companyCode

                                        }};
                                        resultFinal.AddRange(resultData);
                                        isRedeem = false;
                                    }
                                }
                                else//Coupon
                                {
                                    var modelPLH = new PLH_RedeemCouponModel
                                    {

                                        orderNo = model.OrderNo,
                                        storeNo = mappingStore.StoreNoPLH,
                                        posID = model.POSTerminal,
                                        staffCode = "WCM",
                                        listSeriNo = listData.Select(a => new PLH_RedeemCouponResquest
                                        {
                                            seriNo = a.voucherNumber,
                                            quantityRedeem = (int)a.value


                                        }).ToList()
                                    };

                                    var endPoint = $"{PLG_RedeemCoupon}";
                                    using (var client = new HttpClient())
                                    {
                                        //var resultPLH = new List<VoucherStatusResponse>();
                                        try
                                        {
                                            var jsonRequestAPI = JsonConvert.SerializeObject(modelPLH);
                                            client.Timeout = TimeSpan.FromMinutes(2);
                                            client.BaseAddress = new Uri(linkAPIPLH);

                                            var byteArray = Encoding.ASCII.GetBytes($"{PLGUser}:{PLGPassword}");
                                            var authenBasic = Convert.ToBase64String(byteArray);

                                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                                            var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                                            var readData = response.Content.ReadAsStringAsync();
                                            var convertObject = StringHelper.StringToObject<ResultResponse>(readData.Result);
                                            
                                            if (convertObject.Status == HttpStatusCode.OK)
                                            {
                                                var dataJsonObject = JsonConvert.SerializeObject(convertObject.Data);
                                                var dataList = StringHelper.StringToObject<List<PLH_RedeemCouponResponse>>(dataJsonObject);
                                                if (dataList == null)
                                                {
                                                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                                    {
                                                        MessageTechnical = $"Lỗi thanh toán coupon Phúc Long {JsonConvert.SerializeObject(modelPLH.listSeriNo)}",
                                                        Data = null,
                                                        Message = "Lỗi thanh toán coupon Phúc Long",
                                                        Status = HttpStatusCode.BadRequest
                                                    });
                                                }
                                                resultData = dataList.Select(a => new VoucherStatusResponse
                                                {
                                                    Status = a.Message,
                                                    VoucherNumber = a.SeriNo,
                                                    ActicleNo = "",
                                                    ActicleType = "",
                                                    Value = "" + a.QuantityRemain,
                                                    Return = a.StatusCode == true ? "0" : "E",
                                                    CompanyCode = companyCode

                                                }).ToList();
                                            }
                                            else
                                            {
                                                resultData = new List<VoucherStatusResponse>{new VoucherStatusResponse
                                                {
                                                    Status = convertObject.Message,
                                                    VoucherNumber =string.Join(",", listData.Select(a=>a.voucherNumber)),
                                                    ActicleNo = "",
                                                    ActicleType = "",
                                                    Value = "0",
                                                    Return = "E",
                                                    CompanyCode = companyCode

                                                }};
                                            }

                                        }
                                        catch (Exception ex)
                                        {
                                            resultData = new List<VoucherStatusResponse>{new VoucherStatusResponse
                                        {
                                            Status = ex.Message,
                                            VoucherNumber =string.Join(",", listData.Select(a=>a.voucherNumber)),
                                            ActicleNo = "",
                                            ActicleType = "",
                                            Value = "0",
                                            Return = "E",
                                            CompanyCode = companyCode

                                        }};
                                        }
                                        resultFinal.AddRange(resultData);
                                    }
                                }

                            }
                            else//SAP VCM
                            {
                                // var listRequest = model.ListSeriNo;
                                listData = itemCompany.ListData.Where(x => ValidateHelper.IsVoucherSAP(x.voucherNumber) == true).ToList();
                                if (listData.FirstOrDefault().articleType == "ZVCN" || listData.FirstOrDefault().articleType == "ZCPN")
                                {
                                    List<DT_NECPOS_VoucherSerialNo_InputInput> objRequestList = new List<DT_NECPOS_VoucherSerialNo_InputInput>();
                                    for (int i = 0; i < listData.Count; i++)
                                    {
                                        var serialNo = listData[i].voucherNumber;
                                        if (!string.IsNullOrEmpty(serialNo) && ValidateHelper.IsVoucherSAP(serialNo))
                                        {
                                            var objectRequest = new DT_NECPOS_VoucherSerialNo_InputInput
                                            {
                                                Voucher_Type = "V",
                                                Article_No = listData[i].articleNo?.Trim(),
                                                SerialNo = listData[i].voucherNumber,
                                                Voucher_Value = "",
                                                Voucher_Currency = "",
                                                Validity_From_Date = "",
                                                Expiry_Date = "",
                                                Processing_Type = "U",//A create,C check, U update
                                                                      //Status = "RDM",//EXP
                                                Status = listData[i].status,//"RDM","EXP"
                                                Bonus_Buy = "",
                                                Site = ""
                                            };
                                            objRequestList.Add(objectRequest);
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    var objResponseList = _siVoucherOut.SI_VoucherSerialNo_OUT(objRequestList.ToArray()).ToList();
                                    var logRqPOS = JsonConvert.SerializeObject(listData);
                                    var logRqSAP = JsonConvert.SerializeObject(objRequestList);
                                    _kibanaService.LoggingToLogSAP("RedeemCpnVch", model.POSTerminal, listData.FirstOrDefault()?.voucherNumber, logRqPOS, logRqSAP, JsonConvert.SerializeObject(objResponseList));

                                    if (objResponseList != null && objResponseList.Count() > 0)
                                    {

                                        resultData = objResponseList.Select(x => new VoucherStatusResponse
                                        {
                                            Status = x.Status,
                                            ActicleNo = x.Article_No?.Trim(),
                                            ActicleType = x.Article_Type,
                                            Return = x.Return.Trim(),
                                            Value = x.Voucher_Value.Trim(),
                                            Voucher_Currency = x.Voucher_Currency,
                                            Expiry_Date = x.Expiry_Date,
                                            CompanyCode = x.CompanyCode

                                        }).ToList();
                                    }
                                    else
                                    {
                                        resultData = new List<VoucherStatusResponse>{new VoucherStatusResponse
                                        {
                                            Status =$"Không có phản hồi từ SAP",
                                            VoucherNumber =string.Join(",", listData.Select(a=>a.voucherNumber)),
                                            ActicleNo = "",
                                            ActicleType = "",
                                            Value = "0",
                                            Return = "E",
                                            CompanyCode = companyCode

                                        }};

                                    }
                                    resultFinal.AddRange(resultData);
                                }
                                else //Danh cho VOUCHER OLD
                                {
                                    List<VoucherSerialNo_InputInput> objRequestList = new List<VoucherSerialNo_InputInput>();

                                    for (int i = 0; i < listData.Count; i++)
                                    {
                                        var serialNo = listData[i].voucherNumber;

                                        if (!string.IsNullOrEmpty(serialNo))
                                        {
                                            var objectRequest = new VoucherSerialNo_InputInput
                                            {
                                                Voucher_Type = "V",
                                                Article_No = listData[i].articleNo?.Trim(),
                                                SerialNo = listData[i].voucherNumber,
                                                Voucher_Value = "",
                                                Voucher_Currency = "",
                                                Validity_From_Date = "",
                                                Expiry_Date = "",
                                                Processing_Type = "U",//A create,C check, U update
                                                                      //Status = "RDM",
                                                Status = listData[i].status,//RDM,EXP
                                                Site = ""
                                            };
                                            objRequestList.Add(objectRequest);
                                        }
                                    }
                                    var objResponseList = _returnVoucher.VoucherSerialNo(objRequestList.ToArray()).ToList();

                                    var logRqPOS = JsonConvert.SerializeObject(listData);
                                    var logRqSAP = JsonConvert.SerializeObject(objRequestList);

                                    _kibanaService.LoggingToLogSAP("RedeemCpnVch", model.POSTerminal, listData.FirstOrDefault()?.voucherNumber, logRqPOS, logRqSAP, JsonConvert.SerializeObject(objResponseList));

                                    if (objResponseList != null && objResponseList.Count() > 0)
                                    {
                                        resultData = objResponseList.Select(x => new VoucherStatusResponse
                                        {
                                            Status = x.Status,
                                            ActicleNo = x.Article_No?.Trim(),
                                            ActicleType = "VC_CU",
                                            Return = x.Return.Trim(),
                                            Value = x.Voucher_Value.Trim(),
                                            Voucher_Currency = x.Voucher_Currency,
                                            Expiry_Date = x.Expiry_Date,
                                            CompanyCode = companyCode

                                        }).ToList();
                                    }
                                    else
                                    {
                                        resultData = new List<VoucherStatusResponse>{new VoucherStatusResponse
                                        {
                                            Status =$"Không có phản hồi từ SAP",
                                            VoucherNumber =string.Join(",", listData.Select(a=>a.voucherNumber)),
                                            ActicleNo = "",
                                            ActicleType = "",
                                            Value = "0",
                                            Return = "E",
                                            CompanyCode = companyCode

                                        }};
                                    }
                                    resultFinal.AddRange(resultData);
                                }
                            }
                        }

                        //UPDATE STATUS VOUCHER O'lala on SAP
                        if (resultFinal.FirstOrDefault().Return == "0")
                        {
                            await _usingVoucherPartnerService.UpdateStatusVoucherPAR(model);
                        }

                        _kibanaService.LoggingToLogSAP("api/sap/winlife/redeemCpnVch", model.POSTerminal, model.ListSeriNo.FirstOrDefault()?.voucherNumber, JsonConvert.SerializeObject(model), JsonConvert.SerializeObject(model), JsonConvert.SerializeObject(resultFinal));
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = resultFinal,
                            Message = $"OK",
                            Status = HttpStatusCode.OK
                        });
                    }

                    var voucherError = model.ListSeriNo.Where(x => lstVoucherUsed.Contains(x.voucherNumber)).Select(a => a.voucherNumber).ToList();
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        MessageTechnical = $"Danh sách voucher có CompanyCode không hợp lệ {JsonConvert.SerializeObject(voucherError)}",
                        Data = null,
                        Message = $"BadRequest",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                if (isRedeem)
                {
                    _kibanaService.LogResponse("api/sap/winlife/redeemCpnVch", model.POSTerminal, 0, "", model.OrderNo + "_" +JsonConvert.SerializeObject(resultVoucherRedeem));
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = resultVoucherRedeem,
                        Message = string.Format("Bill {0} sử dụng voucher thành công", model.OrderNo),
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Sử dụng voucher {resultVoucherRedeemError.First()} thất bại",
                        Status = HttpStatusCode.BadRequest,
                        MessageTechnical = $"DS voucher lỗi {JsonConvert.SerializeObject(resultVoucherRedeemError)}"
                    });
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LoggingToLogSAP("RedeemCpnVch", model.POSTerminal, model.ListSeriNo.FirstOrDefault()?.voucherNumber, JsonConvert.SerializeObject(model), JsonConvert.SerializeObject(model), JsonConvert.SerializeObject(ex));
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống:{ex.Message}",
                    Status = HttpStatusCode.BadRequest
                });
            }
        }

        [HttpGet]
        [Route("CheckReturnVoucher")]
        public HttpResponseMessage CheckReturnVoucher(string voucherNumber, string siteNo = "", string posTerminal = "", bool IsLog = true)
        {
            try
            {
                if (string.IsNullOrEmpty(voucherNumber))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Mã BNMH không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (voucherNumber.Length > 20)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã BNMH không được lớn hơn 20 ký tự",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                //var prefix_ROP = StringHelper.ConverStringToArray(_prifixVoucherROP, ';');
                var checkROP = GetROPConfigWebApi();
                var prifixVoucherROP = _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX) != null ? _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX).Value : "";
                if (checkROP != null && IsCheckRequestROP(voucherNumber, prifixVoucherROP))
                {
                    string[] lsVoucher = new string[] { voucherNumber };
                    var requestROP = new CheckVouchersROP
                    {
                        VoucherNumbers = lsVoucher,
                        Site = siteNo,
                        PosTerminal = posTerminal
                    };

                    var resultROP = _rOPVoucherService.CheckVouchers(requestROP);
                    if (resultROP.Item1)
                    {
                        if(resultROP.Item3 != null && resultROP.Item3.ActicleType != ArticleTypeEnum.ZPOS.ToString())
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = resultROP.Item3,
                                Message = voucherNumber + " Biên nhận mua hàng không tồn tại",
                                Status = HttpStatusCode.BadRequest
                            });
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = resultROP.Item3,
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = resultROP.Item3,
                            Message = resultROP.Item2 ?? "Có lỗi API từ ROP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
                else
                {
                    List<VoucherSerialNo_InputInput> objRequestList = new List<VoucherSerialNo_InputInput>();
                    var objectRequest = new VoucherSerialNo_InputInput
                    {
                        Voucher_Type = "R",
                        Article_No = "",
                        SerialNo = voucherNumber,
                        Voucher_Value = "",
                        Voucher_Currency = "VND",
                        Validity_From_Date = "",
                        Expiry_Date = "",
                        Processing_Type = "C",//A create,C check, U update
                        Status = "C",
                        Site = ""
                    };
                    objRequestList.Add(objectRequest);
                    var objResponseList = _returnVoucher.VoucherSerialNo(objRequestList.ToArray()).ToList();
                    if (IsLog)
                    {
                        var logRqPOS = voucherNumber;
                        var logRqSAP = JsonConvert.SerializeObject(objRequestList);
                        ModelSAPLog = new LogSAPModel
                        {
                            StoreNo = siteNo,
                            POSTerminal = posTerminal,
                            VoucherNumber = voucherNumber,
                            ActionType = "CheckReturnVoucher",
                            RequestPOS = logRqPOS,
                            RequestSAP = logRqSAP,
                            ResponseSAP = JsonConvert.SerializeObject(objResponseList),
                            CreatedDate = DateTime.Now
                        };
                        SapBLO.WriteLogSAP(ModelSAPLog);
                    }
                    if (objResponseList != null && objResponseList.Count() > 0)
                    {
                        var result = objResponseList[0];
                        if (result.Return != null && result.Return.Trim() == "4")
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = "Mã BNMH không đúng",
                                Status = HttpStatusCode.BadRequest
                            });
                        }

                        if (result.Return != null && result.Return.Trim() != "0")
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = result.Status,
                                Status = HttpStatusCode.BadRequest
                            });
                        }

                        if (result.Status != null && result.Status.Trim() == "RDM")
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = objResponseList == null ? null : new VoucherStatusResponse { Status = result.Status, Return = result.Return.Trim(), Value = result.Voucher_Value.Trim(), VoucherNumber = voucherNumber, Voucher_Currency = result.Voucher_Currency, Validity_From_Date = result.Validity_From_Date, Expiry_Date = result.Expiry_Date },
                                Message = "Mã BNMH đã được sử dụng",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                        if (result.Status != null && result.Status.Trim() == "EXP")
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = objResponseList == null ? null : new VoucherStatusResponse { Status = result.Status, Return = result.Return.Trim(), Value = result.Voucher_Value.Trim(), VoucherNumber = voucherNumber, Voucher_Currency = result.Voucher_Currency, Validity_From_Date = result.Validity_From_Date, Expiry_Date = result.Expiry_Date },
                                Message = "Mã BNMH đã được hủy",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                        try
                        {
                            var checkDateExp = result.Expiry_Date.Replace(".", "/");//31/12/2020
                            DateTime DateExp = DateTime.ParseExact(checkDateExp, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            var dateNow = DateTime.Now.Date;
                            if (DateExp < dateNow)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Data = objResponseList == null ? null : new VoucherStatusResponse
                                    {
                                        Status = result.Status,
                                        Return = result.Return.Trim(),
                                        Value = result.Voucher_Value.Trim(),
                                        VoucherNumber = voucherNumber,
                                        Voucher_Currency = result.Voucher_Currency,
                                        Validity_From_Date = result.Validity_From_Date,
                                        Expiry_Date = result.Expiry_Date
                                    },
                                    Message = "Mã BNMH đã hết hạn sử dụng",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }
                        catch { }
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = objResponseList == null ? null : new VoucherStatusResponse
                            {
                                Status = result.Status,
                                Return = result.Return.Trim(),
                                Value = result.Voucher_Value.Trim().Replace(".", ""),
                                VoucherNumber = voucherNumber,
                                Voucher_Currency = result.Voucher_Currency,
                                Validity_From_Date = result.Validity_From_Date,
                                Expiry_Date = result.Expiry_Date,
                                Partner = "SAP"
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
                            Message = "Không có phản hồi từ SAP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
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
        }

        [HttpPost]
        [Route("UpdateReturnVoucher")]
        public HttpResponseMessage UpdateReturnVoucher(List<VoucherUpdateRequest> model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = ModelState.Values.First().Errors[0].ErrorMessage.ToString(),
                    Status = HttpStatusCode.BadRequest
                });
            }
            try
            {
                if (model == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Dữ liệu đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                var listActionStatus = new List<string> { "RDM", "EXP" };
                if (model.Any(x => string.IsNullOrEmpty(x.Status)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = "Trạng thái không được trống.",
                        Message = "Thất bại",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => !listActionStatus.Contains(x.Status)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = "Trạng thái không hợp lệ.",
                        Message = "Thất bại",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => string.IsNullOrEmpty(x.VoucherNumber)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = "BNMH không được trống.",
                        Message = "Thất bại",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => x.VoucherNumber.Length > 20))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã BNMH không được lớn hơn 20 ký tự",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                //Check chỉ một loại voucher
                var prifixVoucherROP = _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX) != null ? _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX).Value : "";
                if (!IsValidateVoucherROP(model.Select(x => x.VoucherNumber).ToArray(), prifixVoucherROP))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Danh sách voucher/coupon không đồng nhất",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var checkROP = GetROPConfigWebApi();
                if (checkROP != null && IsUpdateCreateRequestROP(model.Select(x => x.VoucherNumber).ToArray(), prifixVoucherROP))
                {
                    List<InsertVoucherROP> insertVoucherROPs = new List<InsertVoucherROP>();
                    for (int i = 0; i < model.Count; i++)
                    {
                        var serialNo = model[i].VoucherNumber;
                    }

                    var lstSeriNo = new List<VoucherUpdateSerial>();
                    foreach (var item in model)
                    {
                        lstSeriNo.Add(new VoucherUpdateSerial()
                        {
                            Partner = "",
                            CompanyCode = item.CompanyCode,
                            isVoucher = false,
                            articleNo = item.ArticleNo,
                            articleType = item.ArticleType,
                            value = 0,
                            status = item.Status,
                            voucherNumber = item.VoucherNumber,
                           
                        });
                    }

                    var request = new VoucherUpdateModel()
                    {
                        SiteCode = model.FirstOrDefault().SiteCode,
                        POSTerminal = model.FirstOrDefault().POSTerminal,
                        IsVoucher = false,
                        OrderNo = model.FirstOrDefault().OrderNo,
                        TotalBill = 0,
                        ListSeriNo = lstSeriNo
                    };

                    var resultROP = _rOPVoucherService.UpdateVoucherStatus(request, _redisClusterManager);
                    if (resultROP.Item1)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = resultROP.Item3 ?? null,
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = resultROP.Item3 ?? null,
                            Message = resultROP.Item2 ?? "Có lỗi API từ ROP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
                else //SAP WCM
                {
                    string ArticleNo = "";
                    if (StringHelper.Left(model.FirstOrDefault().POSTerminal, 1) == "P")
                    {
                        var getArticleNo = _memoryCacheService.GetPOSDataSetup("WWW_ARTICLE_DRW");
                        if (getArticleNo != null)
                        {
                            ArticleNo = getArticleNo.Value;
                            FileHelper.WriteLogs("UpdateReturnVoucher DRW with article: " + ArticleNo);
                        }
                    }

                    List<VoucherSerialNo_InputInput> objRequestList = new List<VoucherSerialNo_InputInput>();
                    for (int i = 0; i < model.Count; i++)
                    {
                        var serialNo = model[i].VoucherNumber;
                        var article_No = model[i].ArticleNo;
                        if (!string.IsNullOrEmpty(ArticleNo))
                        {
                            article_No = ArticleNo;
                        }

                        if (!string.IsNullOrEmpty(serialNo))
                        {
                            var objectRequest = new VoucherSerialNo_InputInput
                            {
                                Voucher_Type = "R",
                                Article_No = article_No,
                                SerialNo = model[i].VoucherNumber,
                                Voucher_Value = "",
                                Voucher_Currency = "",
                                Validity_From_Date = "",
                                Expiry_Date = "",
                                Processing_Type = "U",//A create,C check, U update
                                                      //Status = "RDM",
                                Status = model[i].Status,//"RDM","EXP"
                                Site = ""
                            };
                            objRequestList.Add(objectRequest);
                        }
                    }
                    var objResponseList = _returnVoucher.VoucherSerialNo(objRequestList.ToArray()).ToList();
                    var logRqPOS = JsonConvert.SerializeObject(model);
                    var logRqSAP = JsonConvert.SerializeObject(objRequestList);

                    ModelSAPLog = new LogSAPModel
                    {
                        StoreNo = model.FirstOrDefault()?.SiteCode,
                        POSTerminal = model.FirstOrDefault()?.POSTerminal,
                        VoucherNumber = model.FirstOrDefault()?.VoucherNumber,
                        ActionType = "UpdateReturnVoucher",
                        RequestPOS = logRqPOS,
                        RequestSAP = logRqSAP,
                        ResponseSAP = JsonConvert.SerializeObject(objResponseList),
                        CreatedDate = DateTime.Now
                    };

                    SapBLO.WriteLogSAP(ModelSAPLog);
                    if (objResponseList != null && objResponseList.Count() > 0)
                    {
                        //var result = objResponseList.FirstOrDefault();
                        //if (result.Return != null && result.Return.Trim() != "0")
                        //{
                        //    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        //    {
                        //        Data = null,
                        //        Message = result.Status,
                        //        Status = HttpStatusCode.BadRequest
                        //    });
                        //}

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = objResponseList?.Select(x => new VoucherStatusResponse
                            {
                                Status = x.Status,
                                Return = x.Return.Trim(),//0:thành công, 4: thất bại
                                Value = x.Voucher_Value.Trim(),
                                Voucher_Currency = x.Voucher_Currency,
                                Expiry_Date = x.Expiry_Date

                            }).ToList(),

                            Message = objResponseList.FirstOrDefault().Return ?? "",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = "Không có phản hồi từ SAP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("CreateReturnVoucher")]
        public HttpResponseMessage CreateReturnVoucher(List<CreateVoucherModel> model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = ModelState.Values.First().Errors[0].ErrorMessage.ToString(),
                    Status = HttpStatusCode.BadRequest
                });
            }
            try
            {
                if (model == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Dữ liệu đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => string.IsNullOrEmpty(x.VoucherNumber)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "VoucherNumber đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => string.IsNullOrEmpty(x.From_Date)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "From Date đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => string.IsNullOrEmpty(x.Expiry_Date)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Expiry_Date đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => x.Value <= 0))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Value không hợp lệ",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.Any(x => x.VoucherNumber.Length > 20))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã BNTT không được lớn hơn 20 ký tự",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                //Check voucher ROP or SAP
                var prifixVoucherROP = _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX) != null ? _memoryCacheService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX).Value : "";
                if (!IsValidateVoucherROP(model.Select(x => x.VoucherNumber).ToArray(), prifixVoucherROP))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Danh sách voucher/coupon không đồng nhất",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                //PROCESSING
                var checkROP = GetROPConfigWebApi();
                if (checkROP != null && IsUpdateCreateRequestROP(model.Select(x => x.VoucherNumber).ToArray(), prifixVoucherROP))
                {
                    var itemSapBNTT = _commonBLO.GetDataSetup().FirstOrDefault(x => x.Code == RedisConst.Redis_Key_SAPCODE_BNTT_ROP);
                    if (itemSapBNTT == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = "Chưa khai báo mã phát hàng Biên nhận thanh toán/Vui lòng liên hệ IT Helpdesk",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    model.ForEach(x => x.Article_No = itemSapBNTT.Value);

                    var resultROP = _rOPVoucherService.CreateVoucher(model, ArticleTypeEnum.ZPOS.ToString(), (int)VoucherROPStatusEnum.Actived);
                    if (resultROP.Item1)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = resultROP.Item3,
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = resultROP.Item2 ?? "Có lỗi API từ ROP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
                else
                {
                    string ArticleNo = "";
                    if(StringHelper.Left(model.FirstOrDefault().POSTerminal, 1) == "P")
                    {
                        var getArticleNo = _memoryCacheService.GetPOSDataSetup("WWW_ARTICLE_DRW");
                        if(getArticleNo != null)
                        {
                            ArticleNo = getArticleNo.Value;
                            FileHelper.WriteLogs("CreateReturnVoucher DRW with article: " + ArticleNo);
                        }
                    }

                    List<SAPReturnVoucher.VoucherSerialNo_InputInput> objRequestList = new List<VoucherSerialNo_InputInput>();
                    for (int i = 0; i < model.Count; i++)
                    {
                        var serialNo = model[i].VoucherNumber;
                        if (!string.IsNullOrEmpty(serialNo))
                        {
                            var objectRequest = new VoucherSerialNo_InputInput
                            {
                                Voucher_Type = "R",
                                Article_No = ArticleNo,
                                SerialNo = model[i].VoucherNumber,
                                Voucher_Value = model[i].Value.ToString(),
                                Voucher_Currency = "VND",
                                Validity_From_Date = model[i].From_Date,
                                Expiry_Date = model[i].Expiry_Date,
                                Processing_Type = "A",//A create,C check, U update
                                Status = "A",
                                Site = ""
                            };
                            objRequestList.Add(objectRequest);
                        }
                    }
                    var objResponseList = _returnVoucher.VoucherSerialNo(objRequestList.ToArray()).ToList();

                    var logRqPOS = JsonConvert.SerializeObject(model);
                    var logRqSAP = JsonConvert.SerializeObject(objRequestList);

                    ModelSAPLog = new LogSAPModel
                    {
                        StoreNo = model.FirstOrDefault()?.SiteCode,
                        POSTerminal = model.FirstOrDefault()?.POSTerminal,
                        VoucherNumber = model.FirstOrDefault()?.VoucherNumber,
                        ActionType = "CreateReturnVoucher",
                        RequestPOS = logRqPOS,
                        RequestSAP = logRqSAP,
                        ResponseSAP = JsonConvert.SerializeObject(objResponseList),
                        CreatedDate = DateTime.Now
                    };
                    SapBLO.WriteLogSAP(ModelSAPLog);

                    if (objResponseList != null && objResponseList.Count() > 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = objResponseList?.Select(x => new VoucherStatusResponse { Status = x.Status, Return = x.Return.Trim() }).ToList(),
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = "Không có phản hồi từ SAP",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
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
        }

        [HttpGet]
        [Route("EVoucher")]
        public HttpResponseMessage EVoucher(string CSN, string storeNo = "", string posTerminal = "", bool isLog = true)
        {
            try
            {
                //var model = new VoucherRequest { VoucherNumber = voucherNumber, StoreNo = storeNo };
                if (string.IsNullOrEmpty(CSN))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "CSN không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                List<DT_NECPOS_VoucherCouponInput> objRequestList = new List<DT_NECPOS_VoucherCouponInput>();
                var objectRequest = new DT_NECPOS_VoucherCouponInput
                {
                    CSN = CSN,
                    SerialNo = ""
                };

                objRequestList.Add(objectRequest);
                var data = _siVoucherOut.SI_VoucherCoupon_Request(objRequestList.ToArray()).ToList();
                if (isLog)
                {
                    ModelSAPLog = new LogSAPModel
                    {
                        StoreNo = storeNo,
                        POSTerminal = posTerminal,
                        VoucherNumber = CSN,
                        ActionType = "EVoucher",
                        RequestPOS = CSN,
                        RequestSAP = JsonConvert.SerializeObject(objectRequest),
                        ResponseSAP = JsonConvert.SerializeObject(data),
                        CreatedDate = DateTime.Now
                    };
                    SapBLO.WriteLogSAP(ModelSAPLog);
                }

                var result = data?.Where(d => d.Return == "0").Select(x => new EVoucherResponse
                {
                    CSN = x.CSN,
                    SerialNo = x.SerialNo,
                    ArticleNo = x.ArticleNo,
                    Amount = Convert.ToDecimal(x.Amount),
                    ValidFrom = DateTime.ParseExact(x.ValidFrom, "dd/MM/yyyy", CultureInfo.InvariantCulture),//dd/MM/yyyy
                    ValidTo = DateTime.ParseExact(x.ValidTo, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                    Description = x.Description,
                    Type = x.Type,
                    Return = x.Return,
                    Message = x.Message

                }).ToList();


                if (result.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Không tồn tại E-Voucher tương ứng với thẻ VinID này",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = result,
                    Message = "Success",
                    Status = HttpStatusCode.OK
                });
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
        }

        [HttpGet]
        [Route("SendCodeCpnVch")]
        public async Task<HttpResponseMessage> SendCodeCpnVch([Required] string storeNo, string posID, string bussinessDate, string orderNo, string itemNo, string phoneNumber = "")
        {
            try
            {
                long responseTime;
                _kibanaService.LogRequest("api/sap/SendCodeCpnVch", posID, "", $"{orderNo}|{itemNo}|{phoneNumber}");
                var st1 = new Stopwatch();
                st1.Start();

                if (string.IsNullOrEmpty(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "storeNo không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "posID không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(orderNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "orderNo không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(bussinessDate))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "bussinessDate không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(itemNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "itemNo không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }           
                if (Convert.ToDateTime(bussinessDate).Date != DateTime.Now.Date)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Ngày giao dịch không hợp lệ",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                
                var checkOrderIssued = _issueVoucherService.GetCpnVchCodeSend(_redisClusterManager, _kibanaService, null, orderNo);
                if (checkOrderIssued!= null && checkOrderIssued.ItemNo == itemNo)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = checkOrderIssued,
                        Message = $"Đơn hàng {orderNo} đã được cấp mã voucher {checkOrderIssued.SerialNumber}",
                        Status = HttpStatusCode.OK
                    });
                }

                var quota = await _issueVoucherService.GetCpnVchCodeSendQuota(_redisClusterManager, _kibanaService);
                int qtyOfDay = 0;
                int limitQty = 0;
                if (!string.IsNullOrEmpty(phoneNumber) && quota != null && quota.Count > 0)
                {
                    var quotaItem = quota.FirstOrDefault(x => x.ItemNo == itemNo && x.Blocked == false);
                    if (quotaItem != null) 
                    {
                        if (quotaItem.QtyIssue == 0) 
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, new ResultResponse
                            {
                                Data = null,
                                Message = $"ItemNo {itemNo} đã cấp hết số lượng voucher",
                                Status = HttpStatusCode.NotFound
                            });
                        }

                        limitQty = quotaItem.LimitQty;
                        //check theo DAY
                        if (quotaItem.QtyOfDay > 0)
                        {
                            qtyOfDay = quotaItem.QtyOfDay;
                            var checkPhoneNumber = _issueVoucherService.GetRemnQuotaVoucher(_redisClusterManager, _kibanaService, null, phoneNumber);
                            if (checkPhoneNumber != null && checkPhoneNumber.OrderDate.ToString("yyyyMMdd") == DateTime.Now.ToString("yyyyMMdd") && checkPhoneNumber.Qty == checkPhoneNumber.InitQuota) 
                            {
                                return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                                {
                                    Data = null,
                                    Message = $"Số điện thoại {phoneNumber} đã được cấp voucher hết hạn mức ngày {checkPhoneNumber.OrderDate.ToString("dd-MM-yyyy")}",
                                    Status = HttpStatusCode.Conflict
                                });
                            }
                        }
                    }
                }
                var model = new CpnVchCodeSendRequest
                {
                    StoreNo = storeNo,
                    PosID = posID,
                    BussinessDate = Convert.ToDateTime(bussinessDate).Date,
                    OrderNo = orderNo,
                    ItemNo = itemNo,
                    PhoneNumber = phoneNumber,
                    QtyOfDay = qtyOfDay,
                    LimitQty = limitQty
                };
                var data = _issueVoucherService.SendCodeToPOS(model);
                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
                _kibanaService.LogResponse("api/sap/SendCodeCpnVch", posID, responseTime, "", $"{orderNo}|{itemNo}|{phoneNumber} response: {JsonConvert.SerializeObject(data)}");
                if (data.Item1 == true)
                {
                    if(qtyOfDay > 0 && data.Item4!= null && !string.IsNullOrEmpty(data.Item4.SerialNumber))
                    {
                        var cpmCpdeSenData = new CpnVoucherCodeSendData()
                        {
                            SerialNumber = data.Item4.SerialNumber,
                            ItemNo = itemNo,
                            ItemName = data.Item4.ItemName,
                            OrderNo = orderNo,
                            StatusCode = data.Item4.StatusCode,
                            Status = data.Item4.Status,
                            StoreNo = storeNo,
                            PosID = posID,
                            ActicleType = data.Item4.ActicleType,
                            IsCheckItem = data.Item4.IsCheckItem,
                            MaxQtyUse = data.Item4.MaxQtyUse,
                            MaxAmount = data.Item4.MaxAmount,
                            FromDate = data.Item4.FromDate,
                            ToDate = data.Item4.ToDate,
                            ValueCpnVch = data.Item4.ValueCpnVch,
                            PhoneNumber = phoneNumber
                        };
                        _issueVoucherService.GetCpnVchCodeSend(_redisClusterManager, _kibanaService, cpmCpdeSenData, null);
                        var remnVoucherByPhone = new CpnVchCodeQuotaRemn()
                        {
                            ItemNo = itemNo,
                            OrderDate = Convert.ToDateTime(bussinessDate).Date,
                            PhoneNumber = phoneNumber,
                            Qty = 1,
                            InitQuota = qtyOfDay
                        };
                        _issueVoucherService.GetRemnQuotaVoucher(_redisClusterManager, _kibanaService, remnVoucherByPhone, null);
                    }

                    return Request.CreateResponse(data.Item2, new ResultResponse
                    {
                        Data = data.Item4 != null && !string.IsNullOrEmpty(data.Item4.SerialNumber) ? data.Item4 : null,
                        Message = data.Item3,
                        Status = data.Item2
                    });
                }
                else
                {
                    return Request.CreateResponse(data.Item2, new ResultResponse
                    {
                        Data = null,
                        Message = data.Item3,
                        Status = data.Item2
                    });
                }
            }
            catch (Exception ex)
            {
                var model = new CpnVchCodeSendRequest
                {
                    StoreNo = storeNo,
                    PosID = posID,
                    BussinessDate = Convert.ToDateTime(bussinessDate),
                    OrderNo = orderNo,
                    ItemNo = itemNo
                };
                ModelSAPLog = new LogSAPModel
                {
                    StoreNo = model.StoreNo,
                    POSTerminal = model.PosID,
                    VoucherNumber = "NotSerial",
                    ActionType = "SendCodeCpnVch",
                    RequestPOS = JsonConvert.SerializeObject(model),
                    RequestSAP = "",
                    ResponseSAP = JsonConvert.SerializeObject(ex),
                    CreatedDate = DateTime.Now
                };
                SapBLO.WriteLogSAP(ModelSAPLog);

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống:{JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet]
        [Route("retry")]
        public HttpResponseMessage LoyaltyRetry()
        {
            FileHelper.WriteLogs($"start UpdateRedeemCpnVchCodeSends");
            _rOPVoucherService.UpdateRedeemCpnVchCodeSends();
            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
            {
                Data = null,
                Message = $"Success",
                Status = HttpStatusCode.OK
            });
        }
        private SysWebApi GetROPConfigWebApi()
        {
            try
            {
                return _memoryCacheService.GetSysWebApi(ConfigurationManager.ConnectionStrings["DAPPER_CENTRAL_MD"].ConnectionString).FirstOrDefault(x => x.AppCode == PartnerEnum.ROP.ToString());
            }
            catch
            {
                return null;
            }
        }
        private void SaveLogToDB(string function, string POSTerminal, string voucherNo, string request, string requestROP, string response)
        {
            var modelSAPLog = new LogSAPModel
            {
                StoreNo = StringHelper.Left(POSTerminal, 4),
                POSTerminal = POSTerminal,
                VoucherNumber = voucherNo,
                ActionType = function,
                RequestPOS = request,
                RequestSAP = requestROP,
                ResponseSAP = response,
                CreatedDate = DateTime.Now
            };
            Task.Run(() => SapBLO.WriteLogSAP(modelSAPLog));
        }
        private bool IsCheckRequestROP(string voucher, string prifixVoucherROP)
        {
            if (PrefixVoucher(voucher, prifixVoucherROP).ToUpper() != "@SAP")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //ROP = true; SAP = false
        private bool IsUpdateCreateRequestROP(string[] vouchers, string prifixVoucherROP)
        {
            if (PrefixVoucher(vouchers.FirstOrDefault(), prifixVoucherROP).ToUpper() != "@SAP")
            {
                return true;
            }
            return false;
        }
        private bool IsValidateVoucherROP(string[] vouchers, string prifixVoucherROP)
        {
            //var prifixVoucherROP = _redisService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX) != null ? _redisService.GetPOSDataSetup(RedisConst.Redis_Key_WWW_ROP_V_PREFIX).Value : "";
            int rop = 0;
            int sap = 0;
            foreach (var v in vouchers)
            {
                if (PrefixVoucher(v, prifixVoucherROP).ToUpper() != "@SAP")
                {
                    rop++;
                }
                else
                {
                    sap++;
                }
            }
            if (rop > 0 && sap > 0)
            {
                return false;
            }
            return true;
        }
        private string PrefixVoucher(string voucher, string prifixVoucherROP)
        {
            string prefix_POS = "BP";
            try
            {
                var prefix_ROP_arr = StringHelper.ConverStringToArray(prifixVoucherROP, ';');
                string prefix_ROP = StringHelper.Left(voucher, 3).ToUpper();
                prefix_POS = StringHelper.Right(voucher, 2).ToUpper();
                if (prefix_ROP_arr.Count() > 0 && prefix_ROP_arr.Contains(prefix_ROP)) return prefix_ROP;
                if (prefix_ROP_arr.Count() > 0 && prefix_ROP_arr.Contains(prefix_POS) && voucher.Length >= 19) return prefix_POS;
                return "@SAP";
            }
            catch (Exception ex) 
            {
                FileHelper.Write2Logs("PrefixVoucher", ex.Message.ToString());
                return prefix_POS;
            }

        }
    }
}

