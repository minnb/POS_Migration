using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using TCX.API.Common.Const;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.ROP;
using TCX.API.Common.Dtos.WinCare;
using TCX.API.Common.Dtos.WinCustomer;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.API.Common.Utils;
using TCX.WebApiCore;
using VCM.POSBLUE.API.Models;
using VCM.POSBLUE.Business.Salary;
using VCM.POSBLUE.Model.Salary;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api")]
    public class WinCareController : BaseController
    {
        //private readonly string private_key = CryptographyConst.PRIVATE_KEY_GLOBAL;
        private SalaryBLO SalaryBLO { get; set; }
        private readonly string _connectStringCentralMD;
        private readonly WinCareService _winCareService;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly ROPVoucherService _rOPVoucherService;
        private readonly WinCustomerService _winCustomerService;
        private readonly KibanaService _kibanaService;
        public WinCareController()
        {
            SalaryBLO = new SalaryBLO();
            _winCareService = new WinCareService();
            _memoryCacheService = new MemoryCacheService();
            _rOPVoucherService = new ROPVoucherService();
            _winCustomerService = new WinCustomerService();
            _connectStringCentralMD = _memoryCacheService.GetConnectStringCentralMD();
            _kibanaService = new KibanaService();
        }

        [HttpPost]
        [Route("v2/wincustomer/wpay/shopping-accumulate")]
        public HttpResponseMessage WinPayAccumulation(WinPayAccumulationDto winPayAccumulationDto)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                var sysWebApi = _memoryCacheService.GetSysWebApi(_connectStringCentralMD);
                var conditionSetup = _memoryCacheService.GetWinPayAccumulateSetup(_winCustomerService.GetConnectStringLoyaltyDb());
                if(conditionSetup == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = @"Chưa khai báo điều kiện tích lũy WinPay, vui lòng liên hệ IT",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var checkConditionByOrderDate = conditionSetup.Where(x=>x.FromDate <= winPayAccumulationDto.OrderDate && x.ToDate >= winPayAccumulationDto.OrderDate).ToList();
                if (checkConditionByOrderDate == null || checkConditionByOrderDate.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = string.Format(@"Chương trình tích lũy Winpay {0} đã hết hạn hoặc chưa khai báo", winPayAccumulationDto.TenderType),
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var result = _winCustomerService.WinPayAccumulation(winPayAccumulationDto, sysWebApi, checkConditionByOrderDate, "V2");
                _kibanaService.LogResponse("v2/wincustomer/wpay/shopping-accumulate", winPayAccumulationDto.PosNo, 0, "", winPayAccumulationDto.OrderNo + "@@@" + JsonConvert.SerializeObject(result));
                if (result != null && result.Item1)
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
                        Message = result.Item2,
                        Status = HttpStatusCode.BadRequest
                    });
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
        [Route("v3/wincustomer/wpay/shopping-accumulate")]
        public HttpResponseMessage WinPayAccumulationV3(WinPayAccumulationDto winPayAccumulationDto)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                var sysWebApi = _memoryCacheService.GetSysWebApi(_connectStringCentralMD);
                var conditionSetup = _memoryCacheService.GetWinPayAccumulateSetup(_winCustomerService.GetConnectStringLoyaltyDb());
                if (conditionSetup == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = @"Chưa khai báo điều kiện tích lũy WinPay, vui lòng liên hệ IT",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var checkConditionByOrderDate = conditionSetup.Where(x => x.FromDate <= winPayAccumulationDto.OrderDate && x.ToDate >= winPayAccumulationDto.OrderDate).ToList();
                if (checkConditionByOrderDate == null || checkConditionByOrderDate.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = string.Format(@"Chương trình tích lũy Winpay {0} đã hết hạn hoặc chưa khai báo", winPayAccumulationDto.TenderType),
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var result = _winCustomerService.WinPayAccumulation(winPayAccumulationDto, sysWebApi, checkConditionByOrderDate, "V3");
                _kibanaService.LogResponse("v3/wincustomer/wpay/shopping-accumulate", winPayAccumulationDto.PosNo, 0, "", winPayAccumulationDto.OrderNo + "@@@" + JsonConvert.SerializeObject(result));
                if (result != null && result.Item1)
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
                        Message = result.Item2,
                        Status = HttpStatusCode.BadRequest
                    });
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
        [Route("v2/wincare/OrderSupplierPublic/GenOspQRLogin")]
        public HttpResponseMessage GenOspQRLogin(GenOspQRLogin request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                var result = _rOPVoucherService.GenOspQRLogin(request);
                if (result.Item1 && result.Item3 != null)
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
                        Message = result.Item2,
                        Status = HttpStatusCode.BadRequest
                    });
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("v2/wincare/collect-money/barcode")]
        public HttpResponseMessage GetInfoBarcode(SaleOrderWinPlusBarcode request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                var result = _winCareService.GetInfoBarcode(request);
                if (result.Item1 == 1 && result.Item3 != null)
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
                        Message = result.Item2,
                        Status = HttpStatusCode.BadRequest
                    });
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("v2/wincare/collect-money/confirm")]
        public HttpResponseMessage ConfirmCollectedCash(ConfirmCollectedCashRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                var result = _winCareService.ConfirmCollectedCash(request);
                if (result.Item1 == 1)
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
                        Message = result.Item2,
                        Status = HttpStatusCode.BadRequest
                    });
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("v2/wincare/notify")]
        public async Task<HttpResponseMessage> RequestSentNotifyUserAsync(SentNotifyPOSRequest request) 
        { 
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {
                var result = await _winCareService.RequestSentNotifyUserAsync(request.VoucherCode,request.SiteId,request.TypeId, request.OrderNo, request.RequestTime);
                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = null,
                    Message = result,
                    Status = HttpStatusCode.OK
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
        
        //OFF payslip over to api partner
        //[HttpPost]
        //[Route("v2/salary/view-salary-old")]
        //public HttpResponseMessage ViewSalary(SalaryRequest model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return ExceptionModels();
        //    }
        //    //dateView: yyyy-MM-dd
        //    try
        //    {
        //        #region Check Authorization
        //        var checkAuthen = AuthenAPI.AuthorizationBasic(ActionContext);
        //        if (checkAuthen.StatusCode != HttpStatusCode.OK)
        //        {
        //            return Request.CreateResponse(checkAuthen.StatusCode, new ResultResponse
        //            {
        //                Data = null,
        //                Message = $"Authorization has been denied for this request",
        //                Status = checkAuthen.StatusCode
        //            });
        //        }
        //        #endregion
        //        //CHECK CompanyCODE
        //        var arrCheck = StringHelper.ConverStringToArray(model.userAD, '.');
        //        string companyCode = "";
        //        string userMBC = "";
        //        if (arrCheck.Length > 0)
        //        {
        //            companyCode = arrCheck[0];
        //            userMBC = arrCheck[1];
        //        }

        //        if (!string.IsNullOrEmpty(companyCode) && !string.IsNullOrEmpty(userMBC))
        //        {
        //            var checkEmpl = SalaryBLO.GetEmplCodeFromHCM(companyCode.ToUpper(), userMBC);
        //            if (!checkEmpl.Item1)
        //            {
        //                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
        //                {
        //                    Data = null,
        //                    Message = $"Không tìm thấy thông tin nhân sự " + model.userAD,
        //                    Status = HttpStatusCode.BadRequest
        //                });
        //            }
        //            else
        //            {
        //                var result = SalaryBLO.Get_BangLuongThang_ByEmp(checkEmpl.Item2, Convert.ToDateTime(model.dateView));
        //                if (result.InfoStaff == null)
        //                {
        //                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
        //                    {
        //                        Data = null,
        //                        Message = $"Thông tin User không đúng",
        //                        Status = HttpStatusCode.BadRequest
        //                    });
        //                }

        //                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
        //                {
        //                    Data = result,
        //                    Message = $"OK",
        //                    Status = HttpStatusCode.OK
        //                });
        //            }
        //        }
        //        else
        //        {
        //            var login = SalaryBLO.Login(new LoginViewModel { UserName = model.userAD, PassWord = model.passAD });
        //            if (login.Item1 == ADConnectionStatus.LoginADSuccess)
        //            {
        //                var dataUser = login.Item2;
        //                var manv = dataUser.MaNV;

        //                var result = SalaryBLO.Get_BangLuongThang_ByEmp(manv, Convert.ToDateTime(model.dateView));
        //                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
        //                {
        //                    Data = result,
        //                    Message = $"OK",
        //                    Status = HttpStatusCode.OK
        //                });
        //            }
        //            else
        //            {
        //                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
        //                {
        //                    Data = null,
        //                    Message = $"User hoặc mật khẩu không đúng",
        //                    Status = HttpStatusCode.BadRequest
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
        //        {
        //            Data = null,
        //            Message = $"Lỗi hệ thống {JsonConvert.SerializeObject(ex)}",
        //            Status = HttpStatusCode.InternalServerError
        //        });
        //    }
        //}

        //[HttpPost]
        //[Route("v2/salary/view-salary-news")]
        //public HttpResponseMessage ViewSalaryV2(SalaryRequest model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return ExceptionModels();
        //    }
        //    //dateView: yyyy-MM-dd
        //    try
        //    {
        //        //CHECK CompanyCODE
        //        var arrCheck = StringHelper.ConverStringToArray(model.userAD, '.');
        //        string companyCode = "";
        //        string userMBC = "";
        //        if (arrCheck.Length > 0)
        //        {
        //            companyCode = arrCheck[0];
        //            userMBC = arrCheck[1];
        //        }

        //        if (!string.IsNullOrEmpty(companyCode) && !string.IsNullOrEmpty(userMBC))
        //        {
        //            var checkEmpl = SalaryBLO.GetEmplCodeFromHCM(companyCode.ToUpper(), userMBC);
        //            if (!checkEmpl.Item1)
        //            {
        //                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
        //                {
        //                    Data = null,
        //                    Message = $"Không tìm thấy thông tin nhân sự " + model.userAD,
        //                    Status = HttpStatusCode.BadRequest
        //                });
        //            }
        //            else
        //            {
        //                var result = SalaryBLO.Get_BangLuongThang_ByEmp(checkEmpl.Item2, Convert.ToDateTime(model.dateView));
        //                if (result.InfoStaff == null)
        //                {
        //                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
        //                    {
        //                        Data = null,
        //                        Message = $"Thông tin User không đúng",
        //                        Status = HttpStatusCode.BadRequest
        //                    });
        //                }

        //                //string encodeResult = EncryptionUtil.EncryptAES(JsonConvert.SerializeObject(result), private_key);
        //                //FileHelper.WriteLogs(EncryptionUtil.DecryptAES(encodeResult, private_key));

        //                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
        //                {
        //                    Data = EncryptionUtil.EncryptAES(JsonConvert.SerializeObject(result), private_key),
        //                    //Data = result,
        //                    Message = $"OK",
        //                    Status = HttpStatusCode.OK
        //                });
        //            }
        //        }
        //        else
        //        {
        //            var login = SalaryBLO.Login(new LoginViewModel { UserName = model.userAD, PassWord = model.passAD });
        //            if (login.Item1 == ADConnectionStatus.LoginADSuccess)
        //            {
        //                var dataUser = login.Item2;
        //                var manv = dataUser.MaNV;

        //                var result = SalaryBLO.Get_BangLuongThang_ByEmp(manv, Convert.ToDateTime(model.dateView));
        //                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
        //                {
        //                    Data = result,
        //                    Message = $"OK",
        //                    Status = HttpStatusCode.OK
        //                });
        //            }
        //            else
        //            {
        //                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
        //                {
        //                    Data = null,
        //                    Message = $"User hoặc mật khẩu không đúng",
        //                    Status = HttpStatusCode.BadRequest
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
        //        {
        //            Data = null,
        //            Message = $"Lỗi hệ thống {JsonConvert.SerializeObject(ex)}",
        //            Status = HttpStatusCode.InternalServerError
        //        });
        //    }
        //}
    }
}
