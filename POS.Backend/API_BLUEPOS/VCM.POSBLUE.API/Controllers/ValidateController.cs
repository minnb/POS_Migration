using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Coupon;
using TCX.API.Common.Dtos.Loyalty.MemberBusiness;
using TCX.API.Common.Dtos.Tax;
using TCX.API.Common.Dtos.Telegram;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore;
using TCX.WebApiCore.AppServices;
using VCM.POSBLUE.API.Models;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api/v2")]
    public class ValidateController : BaseController
    {
        private readonly MemoryCacheService _memoryCacheService;
        private readonly MemberBusinessService _memberBusinessService;
        private readonly DataRawService _dataRawService;
        public ValidateController() 
        {
            _memberBusinessService = new MemberBusinessService();
            _dataRawService = new DataRawService();
            _memoryCacheService = new MemoryCacheService();
        }

        [HttpPost]
        [Route("telegram/send")]
        public HttpResponseMessage TelegramSend([FromBody] MessageTelegramRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ExceptionModels();
            }
            try
            {               
                var sendTelegram = HttpClientService.SendMessageSMS(request.Message, request.MessageType, $"{request.MessageType}");
                if (sendTelegram.Item1)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = null,
                        Message = sendTelegram.Item2,
                        Status = HttpStatusCode.OK,
                        MessageTechnical = sendTelegram.Item2
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = sendTelegram.Item2,
                        Status = HttpStatusCode.BadRequest,
                        MessageTechnical = sendTelegram.Item2
                    });
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("api/v2/invoice/create", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi Exception:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }

        [HttpGet]
        [Route("validate/tax-code")]
        public HttpResponseMessage ValidateTaxCode(string taxCode)
        {
            try
            {
                var webApi = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetStringDbCentralMD()).FirstOrDefault(x=>x.AppCode == "PARTNER");
                if(webApi == null)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                    {
                        Data = null,
                        Message = $"Chưa khai báo webApi VietQR",
                        Status = HttpStatusCode.Conflict
                    });
                }

                var result = HttpClientService.CallWebApiPartner(webApi, "ValidateTaxCode", "GET", @"/" + taxCode, null);
                if (result.Item1)
                {
                    var taxData = StringHelper.StringToObject<HttResponsePartner>(result.Item2);
                    if(taxData != null && taxData.Meta.Code == 200)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = StringHelper.StringToObject<TaxCustInfo>(JsonConvert.SerializeObject(taxData.Data)),
                            Message = "Success",
                            Status = HttpStatusCode.OK,
                            MessageTechnical = JsonConvert.SerializeObject(taxData.Meta)
                        });
                    }
                    else if(taxData != null && taxData.Meta.Code == 9998)
                    {
                        return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                        {
                            Data = null,
                            Message = $"Có lỗi gọi webApi VietQR",
                            Status = HttpStatusCode.Conflict,
                            MessageTechnical = taxData.Meta.Message
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, new ResultResponse
                        {
                            Data = null,
                            Message = "",
                            Status = HttpStatusCode.NotFound,
                            MessageTechnical = ""
                        });
                    }
                }

                return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                {
                    Data = null,
                    Message = result.Item2,
                    Status = HttpStatusCode.Conflict,
                    MessageTechnical = result.Item2
                });
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("validate/tax-code.Exception", ex);
                return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi Exception:{ex.Message}",
                    Status = HttpStatusCode.Conflict
                });
            }
        }

        [HttpGet]
        [Route("validate/transaction")]
        public HttpResponseMessage ValidateTransaction([Required] string orderNo)
        {
            try
            {
                if(string.IsNullOrEmpty(orderNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Bạn phải chưa nhập Phiếu tính tiền",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                else if(orderNo.Length != 15)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Phiếu tính tiền không đúng",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (ValidateHelper.IsValidOrderNo(orderNo) == false)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Phiếu tính tiền không đúng định dạng",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var result = _dataRawService.ValidateTransaction(orderNo);
                if (result.Item1)
                {
                    return Request.CreateResponse(result.Item4, new ResultResponse
                    {
                        Data = result.Item3,
                        Message = "Success",
                        Status = result.Item4,
                        MessageTechnical = result.Item2
                    });
                }
                else
                {
                    return Request.CreateResponse(result.Item4, new ResultResponse
                    {
                        Data = result.Item3,
                        Message = result.Item2,
                        Status = result.Item4,
                        MessageTechnical = JsonConvert.SerializeObject(result.Item3)
                    });
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("api/v2/validate/transaction Exception", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi Exception:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("invoice/create")]
        public HttpResponseMessage InvoiceCreated([FromBody] InvoiceCreatedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ExceptionModels();
                }
                var validateRequest = ValidateHelper.IsValidInvoiceCreated(request);
                if (!validateRequest.Item1)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = validateRequest.Item2,
                        Status = HttpStatusCode.BadRequest,
                        MessageTechnical = validateRequest.Item2
                    });
                }

                string Id = Guid.NewGuid().ToString();
                List<InvoiceCreated> invoiceCreateds = new List<InvoiceCreated>();
                foreach(var item in request.OrderNo)
                {
                    invoiceCreateds.Add(new TCX.API.Common.Dtos.Tax.InvoiceCreated()
                    {
                        OrderNo = item,
                        SiteNo = request.SiteNo,
                        CompanyName = request.CompanyName??"",
                        CustomerName = request.CustomerName ?? "",
                        TaxCode = request.TaxCode ?? "",
                        Address = request.Address ?? "",
                        PhoneNumber = request.PhoneNumber ?? "",
                        Email = request.Email ?? "",
                        IsNotVAT = false,
                        IsNotVATPartner = "WCM",
                        Id = Id,
                        Passport = request.Passport ??"",
                        CCCD = request.CCCD??"",
                        DVQHNS = request.DVQHNS??""
                    });
                }

                var result = _dataRawService.InvoiceCreated(invoiceCreateds);
                if (result.Item1)
                {
                    var webApi = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetStringDbCentralMD()).FirstOrDefault(x => x.AppCode == "PARTNER");
                    Task.Run(async () => await HttpClientService.CallWebApiPartnerAsync(webApi, "UpdateTaxInfo", "POST", null, JsonConvert.SerializeObject(invoiceCreateds.FirstOrDefault())));
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = null,
                        Message = "Success",
                        Status = HttpStatusCode.OK,
                        MessageTechnical = result.Item3
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = result.Item2,
                        Status = HttpStatusCode.BadRequest,
                        MessageTechnical = result.Item3
                    });
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("invoice_create.Exception", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi Exception:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError,
                    MessageTechnical = JsonConvert.SerializeObject(ex)
                });
            }
        }

        [HttpPost]
        [Route("validate/member-business")]
        public HttpResponseMessage ValidateMemberBusinessWhenPayment([FromBody] ValidateMemberBusiness validateMemberBusiness)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ExceptionModels();
                }
                validateMemberBusiness.MemberCard = FormatHelper.PhoneNumberVietNam(validateMemberBusiness.MemberCard);

                if (!EncryptionHelper.Base64Decode(validateMemberBusiness.Key).Contains(string.Join("_", validateMemberBusiness.StoreNo, validateMemberBusiness.PosNo, validateMemberBusiness.MemberCard)))
                {
                    return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, string.Format(@"Key không đúng của máy POS {0}", validateMemberBusiness.PosNo), null);
                }

                return ResponseData.HttpResponseData(HttpStatusCode.OK, "OK", _memberBusinessService.ValidateMemberBusinessDataPayment(validateMemberBusiness.StoreNo, 
                                                                                                                                       validateMemberBusiness.PosNo, 
                                                                                                                                       validateMemberBusiness.MemberCard,
                                                                                                                                       validateMemberBusiness.Key));
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("api/v2/validate/member-business Exception", ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống:{ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
    }
}
