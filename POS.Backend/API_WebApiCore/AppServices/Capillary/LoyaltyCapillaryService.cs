using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Results;
using TCX.API.Common.AppService;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Capillary;
using TCX.API.Common.Dtos.Capillary.Coupons;
using TCX.API.Common.Dtos.Capillary.Customer;
using TCX.API.Common.Dtos.Capillary.Point;
using TCX.API.Common.Dtos.Capillary.Transaction;
using TCX.API.Common.Dtos.Capillary.Vouchers;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;
using VCM.POSBLUE.Model.GiftBox;
using VCM.POSBLUE.Model.WinLife;
using PaymentEntryLoyalty = TCX.API.Common.Dtos.Loyalty.PaymentEntryLoyalty;
using TransLineLoyalty = TCX.API.Common.Dtos.Loyalty.TransLineLoyalty;
using VinIDRefundRequest = TCX.API.Common.Dtos.Loyalty.VinIDRefundRequest;
using VinIDSalesRequest = TCX.API.Common.Dtos.Loyalty.VinIDSalesRequest;
using WinLife_SmartPOS_Update_Customer_Req_POS = TCX.API.Common.Dtos.Loyalty.WinLife_SmartPOS_Update_Customer_Req_POS;

namespace TCX.WebApiCore
{
    public class LoyaltyCapillaryService
    {
        private readonly CapillaryService _capillaryLoyaltyService;
        private readonly KibanaService _kibanaService;
        private readonly string _parentAddTransaction = "AddTransaction";
        private readonly string _parentReturnTransaction = "ReturnTransaction";
        private readonly string _connectStringDb = AppGlobals.ConnStrCentralMD;
        private readonly string _connectStringLoyaltyDb = AppGlobals.ConnStrLoyalty;
        public LoyaltyCapillaryService() 
        {
            _capillaryLoyaltyService = new CapillaryService();
            _kibanaService = new KibanaService();
        }
        public async Task<Tuple<bool, string, object>> ReturnTransactionToCap(LoyaltyRepository loyaltyRepository, RedisManager _redisManager, MemoryCacheService _memoryCacheService, AddTransactionCapPOSRequest request, string posRequest, string orderNo)
        {
            request.MemberCard = FormatHelper.PhoneNumberWithCountryCode(request.MemberCard);
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == LoyaltyHelper.CheckSwithLoyaltyTheWINX(_redisManager, loyaltyRepository, request.StoreNo).ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "AddTransaction".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);
                
                Tuple<bool, string, long, object> result = null;

                if (request.TransactionType == 2 && request.Type == TransactionTypeCapEnum.RETURN.ToString())
                {
                    ReturnFullTransactionCapillaryRequest requestCAP = new ReturnFullTransactionCapillaryRequest
                    {
                        Type = request.Type.ToUpper(),
                        ReturnType = ReturnTypeCapEnum.FULL.ToString(),
                        BillNumber = request.BillNumber,
                        BillingDate = request.BillingDate,
                        BillAmount = (int)request.BillAmount,
                        DeliveryStatus = "RETURNED",
                        Note = orderNo
                    };

                    _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, request.PosNo, "", request.MemberCard + $"-{orderNo}-" + StringHelper.ObjectToStringLowercase(requestCAP));
                    result = await _capillaryLoyaltyService.AddTransaction(_sysWebApiDto, requestCAP, request.StoreNo, request.MemberCard, request.BillNumber, _memoryCacheService.GetNotifyConfig(_connectStringDb));
                    _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, request.PosNo, result.Item3, "", request.MemberCard + $"-{orderNo}-" + JsonConvert.SerializeObject(result.Item4));
                }
                else if (request.TransactionType == 3 && request.Type == TransactionTypeCapEnum.RETURN.ToString())
                {
                    ReturnPartialTransactionCapillaryRequest requestCAP = new ReturnPartialTransactionCapillaryRequest
                    {
                        Type = request.Type.ToUpper(),
                        ReturnType = ReturnTypeCapEnum.LINE_ITEM.ToString(),
                        BillNumber = request.BillNumber,
                        BillAmount = (int)request.BillAmount,
                        BillingDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Note = orderNo
                    };
                    List<PaymentModesCapillary> paymentModesCapillaries = new List<PaymentModesCapillary>();

                    foreach (var paymentMode in request.PaymentModes)
                    {
                        paymentModesCapillaries.Add(new PaymentModesCapillary()
                        {
                            Mode = paymentMode.Mode,
                            Value = paymentMode.Value,
                            Attributes = new AttributesCardTypeCapillary()
                            {
                                Card_type = paymentMode.Attributes.Card_type
                            }
                        });
                    }
                    List<ReturnLineItemsCapillary> lineItemsCapillaries = new List<ReturnLineItemsCapillary>();
                    foreach (var item in request.LineItemsV2)
                    {
                        lineItemsCapillaries.Add(new ReturnLineItemsCapillary()
                        {
                            Discount = item.Discount,
                            Amount = (int)item.Amount,
                            Rate = (int)item.Rate,
                            ItemCode = item.ItemCode,
                            Description = item.Description,
                            Qty = item.Qty,
                            ReturnType = ReturnTypeCapEnum.LINE_ITEM.ToString(),
                            Type= TransactionTypeCapEnum.RETURN.ToString()
                        });
                    }
                    requestCAP.LineItemsV2 = lineItemsCapillaries;
                    requestCAP.PaymentModes = paymentModesCapillaries;

                    _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, request.PosNo, "", request.MemberCard + @"@" + StringHelper.ObjectToStringLowercase(requestCAP));
                    result = await _capillaryLoyaltyService.AddTransaction(_sysWebApiDto, requestCAP, request.StoreNo, request.MemberCard, request.BillNumber, _memoryCacheService.GetNotifyConfig(_connectStringDb));
                    _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, request.StoreNo, result.Item3, "", request.MemberCard + $"-{orderNo}-" + JsonConvert.SerializeObject(result.Item4));
                }
                else
                {
                    return new Tuple<bool, string, object>(false, @"TransactionType không đúng - vui lòng liên hệ IT", null);
                }

                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, request.PosNo, result.Item3, "", request.MemberCard + "@" + (result.Item4 != null ? JsonConvert.SerializeObject(result.Item4) : (request.MemberCard +@"@"+ request.BillNumber)));

                if (result.Item3 == -9999)
                {
                    _redisManager.Set(_parentReturnTransaction +":"+Guid.NewGuid().ToString(), JsonConvert.SerializeObject(request), 518400);
                    return new Tuple<bool, string, object>(true, HttpStatusCode.ServiceUnavailable.ToString(), null);
                }

                return new Tuple<bool, string, object>(result.Item1, result.Item2, result.Item4);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public async Task<Tuple<bool, string, object>> AddTransactionToCap(LoyaltyRepository loyaltyRepository, RedisManager redisManager, MemoryCacheService _memoryCacheService, AddTransactionCapPOSRequest request, bool isStaff = false)
        {
            request.MemberCard = FormatHelper.PhoneNumberWithCountryCode(request.MemberCard);
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == LoyaltyHelper.CheckSwithLoyaltyTheWINX(redisManager, loyaltyRepository, request.StoreNo).ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "AddTransaction".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                AddTransactionCapillaryRequest requestCAP = new AddTransactionCapillaryRequest
                {
                    Type = request.Type.ToUpper(),
                    BillNumber = request.BillNumber,
                    Discount = request.Discount,
                    BillAmount = request.BillAmount,
                    BillingDate = request.BillingDate
                };

                if (request.IsMobile)
                {
                    requestCAP.CustomFields = new Iden_mode_field()
                    {
                        Iden_mode = "MOBILE"
                    };
                }

                List<PaymentModesCapillary> paymentModesCapillaries = new List<PaymentModesCapillary>();
                foreach (var paymentMode in request.PaymentModes)
                {
                    paymentModesCapillaries.Add(new PaymentModesCapillary()
                    {
                        Mode = paymentMode.Mode,
                        Value = paymentMode.Value,
                        Attributes = new AttributesCardTypeCapillary()
                        {
                            Card_type = paymentMode.Attributes.Card_type
                        }
                    });
                }

                List<LineItemsCapillary> lineItemsCapillaries = new List<LineItemsCapillary>();
                foreach (var item in request.LineItemsV2)
                {
                    lineItemsCapillaries.Add(new LineItemsCapillary()
                    {
                        ItemCode = item.ItemCode,
                        Description = item.Description,
                        Qty = item.Qty,
                        Discount = item.Discount,
                        Amount = item.Amount,
                        Rate = item.Rate,
                        Serial = item.Serial,
                        ExtendedFields = item.ExtendedFields,
                    });
                }
                string orderChannel = _sysWebApiDto.Authorization;
                if(request.OrderChannel.ToUpper() == "WINMART")
                {
                    orderChannel = request.OrderChannel;
                }
                TransactionExtendedFields extendedFields = new TransactionExtendedFields()
                {
                    Amount_excluding_tax = request.ExtendedFields.Amount_excluding_tax,
                    Delivery_date = request.ExtendedFields.Delivery_date.Date,
                    Tax_amount = request.ExtendedFields.Tax_amount,
                    Store_associate_id = request.ExtendedFields.Store_associate_id,
                    Order_channel = orderChannel,
                    Order_date_time = request.ExtendedFields.Order_date_time,
                    Delivery_charges = 0,
                    Cashier_id = request.ExtendedFields.Cashier_id,
                    App_mode = request.IsMobile ? "MOBILE" : "",
                    Dist_code = isStaff ? "STAFF" : ""
                };
                requestCAP.PaymentModes = paymentModesCapillaries;
                requestCAP.LineItemsV2 = lineItemsCapillaries;
                requestCAP.ExtendedFields = extendedFields;

                var programId = LoyaltyExtentionService.GetProgramIdCapillary(_memoryCacheService.GetCacheConfigLoyalty(_memoryCacheService.GetStringDbLoyalty()), "", isStaff);

                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, request.PosNo, "", request.MemberCard + @"@"+ StringHelper.ObjectToStringLowercase(requestCAP));
                var result = await _capillaryLoyaltyService.AddTransaction(_sysWebApiDto, requestCAP, request.StoreNo, request.MemberCard, request.BillNumber, _memoryCacheService.GetNotifyConfig(_connectStringDb), programId);
                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, request.PosNo, result.Item3, "", request.BillNumber + "@" + (result.Item4 != null ? $"{request.MemberCard}|{request.BillNumber}|{JsonConvert.SerializeObject(result.Item4)}" : request.BillNumber));
                
                if(result.Item3 == -9999)
                {
                    _kibanaService.LogException("AddTransactionToCap", request.PosNo, 0, "", request.MemberCard + " Exception: ServiceUnavailable");
                    return new Tuple<bool, string, object>(true, HttpStatusCode.ServiceUnavailable.ToString(), null);
                }

                return new Tuple<bool, string, object>(result.Item1, result.Item2, result.Item4);
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("AddTransactionToCap", request.PosNo, 0, "", request.MemberCard + "-Exception-" + JsonConvert.SerializeObject(ex)); 
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public Tuple<bool, string> GetTransactionDetail(MemoryCacheService _memoryCacheService, string storeNo, string posNo, string orderNo, string type = "REGULAR")
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string>(false, MessConst.NotFounDataConfig);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "AddTransactionDetail".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string>(false, MessageConst.ApiNotFounDataConfig);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" }
                };

                string param = $"/{orderNo}?tenders=true&type={type}";
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, orderNo, "", param);
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(_sysWebApiDto.UserName + "." + storeNo, _sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(_sysWebApiDto.Host, router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(_sysWebApiDto.Version),
                                                                _sysWebApiDto.HttpProxy,
                                                                _sysWebApiDto.Bypasslist
                                                                );

                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                StringHelper.Loggingz($"GetTransactionDetail ByBillNumber: {response}");
                //_kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, posNo, responseTime, "", $"orderNo: {orderNo}--" + response ?? JsonConvert.SerializeObject(responseErr));
                if (!string.IsNullOrEmpty(response) && responseErr.StatusCode == 200)
                {
                    return new Tuple<bool, string>(true, response);
                }
                else
                {
                    return new Tuple<bool, string>(false, $"Lỗi kết nối WebApi Capillary");
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, $"Lỗi GetSerialInfo.Exception: {ex.Message}");
            }
        }
        public async Task<Tuple<bool, string, object, int, string>> GetCustomerLoyaltyDetail(LoyaltyRepository loyaltyRepository, RedisManager redisManager, MemoryCacheService _memoryCacheService, List<SysWebApiDto> _lstSysWebApiDto, string storeNo, string posNo, string phoneNumber, string memberCapillary)
        {
            string phoneVietNam = phoneNumber;
            if (memberCapillary == MemberCapillaryEnum.PHONE.ToString())
            {
                phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
                phoneVietNam = FormatHelper.PhoneNumberVietNam(phoneNumber);
            }
            string appCode = LoyaltyHelper.CheckSwithLoyaltyTheWINX(redisManager, loyaltyRepository, storeNo);
            var _sysWebApiDto = _lstSysWebApiDto?.Where(x => x.AppCode == appCode).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object, int, string>(false, MessConst.NotFounDataConfig, null, 404, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetCustomer".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object, int, string>(false, MessageConst.ApiNotFounDataConfig, null, 404, appCode);
                
                string param = _sysWebApiDto.Host + $"?format=json&mobile={phoneNumber}{router.Notes}&store_id={storeNo}";
                if( memberCapillary == MemberCapillaryEnum.ID.ToString())
                {
                    param = _sysWebApiDto.Host + $"?format=json&id={phoneNumber}{router.Notes}&store_id={storeNo}";
                }

                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, posNo, "", param);
                var result = await _capillaryLoyaltyService.GetCustomerDetail(_sysWebApiDto, storeNo, posNo, phoneNumber, memberCapillary, _memoryCacheService.GetNotifyConfig(_connectStringDb));               
                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, posNo, result.Item3, "", phoneVietNam + "@" + (result.Item4!= null ? JsonConvert.SerializeObject(result.Item4) : phoneNumber));
                return new Tuple<bool, string, object, int, string>(result.Item1, result.Item2, result.Item4, result.Item5, appCode);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object, int, string>(false, ex.Message.ToString(), null, 400, appCode);
            }
        }
        public Tuple<bool, string, object> CustomerRegistration(LoyaltyRepository loyaltyRepository,RedisManager redisManager, MemoryCacheService _memoryCacheService, WinLife_Register_POS_Request customer, string acquisition_channel)
        {
            customer.phoneNo = FormatHelper.PhoneNumberWithCountryCode(customer.phoneNo);
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == LoyaltyHelper.CheckSwithLoyaltyTheWINX(redisManager, loyaltyRepository, customer.storeCode).ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "CustomerRegistration".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                var customerRegistrationRequest = new CustomerRegistrationRequest
                {
                    LoyaltyInfo = new LoyaltyInfoCapillary()
                    {
                        LoyaltyType = "loyalty"
                    }
                };

                List<IdentifiersCapillary> identifiersCapillaries = new List<IdentifiersCapillary>
                {
                    new IdentifiersCapillary()
                    {
                        Type = "mobile",
                        Value = customer.phoneNo
                    }
                };

                if(!string.IsNullOrEmpty(customer.email))
                {
                    identifiersCapillaries.Add(new IdentifiersCapillary()
                    {
                        Type = "email",
                        Value = customer.email ?? ""
                    });
                }

                List<ProfilesCapillary> profilesCapillaries = new List<ProfilesCapillary>
                {
                    new ProfilesCapillary()
                    {
                        FirstName = customer.fullName,
                        Identifiers = identifiersCapillaries,
                        Source = "INSTORE",
                        Fields = new RegisterFieldsCapillary()
                        {
                            Staff_id = customer.userPOS,
                            Masan_referral_code = customer.posCode,
                            Masan_customer_id = "", //customer.phoneNo,
                            Customer_address = customer.address ?? "",
                        }
                    }
                };
                customerRegistrationRequest.Profiles = profilesCapillaries;
                ExtendedFieldsCapillary extendedFieldsCapillary = new ExtendedFieldsCapillary()
                {
                    Gender = customer.gender == "M" ? GenderEnum.Male.ToString() : GenderEnum.Female.ToString(),
                    Acquisition_channel = acquisition_channel,
                    Dob_date = customer.dob,
                    Sub_area = customer.ward ?? "",
                    Area = customer.district ?? "",
                    State = customer.province?? "",
                    City = customer.city ?? "",
                };

                customerRegistrationRequest.ExtendedFields = extendedFieldsCapillary;
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, customer.posCode, "", StringHelper.ObjectToStringLowercase(customerRegistrationRequest));

                var result = _capillaryLoyaltyService.CustomerRegistration(_sysWebApiDto, customerRegistrationRequest, customer.storeCode, customer.phoneNo, _memoryCacheService.GetNotifyConfig(_connectStringDb));

                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, customer.posCode, result.Item3, "", customer.phoneNo + "@" + (result.Item4 != null ? JsonConvert.SerializeObject(result.Item4) : customer.phoneNo));
                _kibanaService.LoggingToLoyaltyDB(customer.posCode, router.Route, "CustomerRegistration", result.Item3, customer.phoneNo, JsonConvert.SerializeObject(customer), JsonConvert.SerializeObject(customerRegistrationRequest), JsonConvert.SerializeObject(result.Item4));

                return new Tuple<bool, string, object>(result.Item1, result.Item2, result.Item4);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public Tuple<bool, string, object> CustomerUpdate(MemoryCacheService _memoryCacheService, WinLife_SmartPOS_Update_Customer_Req_POS customer, string acquisition_channel)
        {
            customer.phoneNo = FormatHelper.PhoneNumberWithCountryCode(customer.phoneNo);
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "UpdateCustomer".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                CustomerUpdateCapillary customerUpdateCapillary = new CustomerUpdateCapillary
                {
                    LoyaltyInfo = new LoyaltyInfoCapillary()
                    {
                        LoyaltyType = "loyalty"
                    }
                };
                var profiles = new List<ProfilesUpdateCapillary>
                {
                    new ProfilesUpdateCapillary()
                    {
                        Source = "INSTORE",
                        FirstName = customer.fullName,
                        Fields = new RegisterFieldsCapillary()
                        {
                            Customer_address = customer.address
                        }
                    }
                };
                customerUpdateCapillary.Profiles = profiles;

                ExtFieldsCustomerUpdateCapillary extendedFieldsCapillary = new ExtFieldsCustomerUpdateCapillary()
                {
                    Gender = customer.gender == "M" ? GenderEnum.Male.ToString() : GenderEnum.Female.ToString(),
                    Acquisition_channel = acquisition_channel,
                    Dob_date = customer.dob,
                    Sub_area = customer.ward ?? "",
                    Area = customer.district ?? "",
                    State = customer.province ?? "",
                    City = customer.city ?? "",
                };
                customerUpdateCapillary.ExtendedFields = extendedFieldsCapillary;
                
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, customer.posCode, "", StringHelper.ObjectToStringLowercase(extendedFieldsCapillary));

                var result = _capillaryLoyaltyService.CustomerUpdate(_sysWebApiDto, customerUpdateCapillary, customer.storeCode, customer.phoneNo, _memoryCacheService.GetNotifyConfig(_connectStringDb));

                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, customer.posCode, result.Item3, "", customer.phoneNo + "@" + (result.Item4 != null ? JsonConvert.SerializeObject(result.Item4) : customer.phoneNo));
                
                return new Tuple<bool, string, object>(result.Item1, result.Item2, result.Item4);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public async Task<Tuple<bool, string, object>> GetPointsLedger(MemoryCacheService _memoryCacheService, string storeNo, string posNo, string phoneNumber)
        {
            phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetPointsLedger".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                var result = await _capillaryLoyaltyService.GetPointsLedger(_sysWebApiDto, storeNo, phoneNumber);               
               
                return new Tuple<bool, string, object>(result.Item1, result.Item2, result.Item4);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public Tuple<bool, string, object> PointRedeem(LoyaltyRepository loyaltyRepository, RedisManager redisManager, MemoryCacheService _memoryCacheService, string storeNo, string posNo, string orderNo, string phoneNumber, long pointRedeem, string posRequest = "", bool isStaff = false)
        {
            phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == LoyaltyHelper.CheckSwithLoyaltyTheWINX(redisManager, loyaltyRepository, storeNo) .ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "RedeemPoints".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                PointsRedeemCapRequest pointsRedeemCapRequest = new PointsRedeemCapRequest();
                var redeemData = new RedeemCap
                {
                    Points_redeemed = pointRedeem.ToString(),
                    Transaction_number = orderNo.ToString(),
                    Customer = new CustRedeemCap()
                    {
                        Mobile = phoneNumber
                    }
                };

                List<RedeemCap> redeemCap = new List<RedeemCap>
                {
                    redeemData
                };

                pointsRedeemCapRequest.Root = new RootPointRedeemCap()
                {
                    Redeem = redeemCap
                };

                var programId = LoyaltyExtentionService.GetProgramIdCapillary(_memoryCacheService.GetCacheConfigLoyalty(_memoryCacheService.GetStringDbLoyalty()), "", isStaff);
                if (programId == null || (programId != null && programId.FirstOrDefault() == null))
                {
                    return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);
                }

                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, posNo, "", phoneNumber + $" redeem program_id {programId} vs request: {pointsRedeemCapRequest}");
                var result = _capillaryLoyaltyService.PointsRedeem(_sysWebApiDto, pointsRedeemCapRequest, storeNo, programId.FirstOrDefault(), _memoryCacheService.GetNotifyConfig(_connectStringDb));
                
                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, posNo, result.Item3, "", phoneNumber + "@" + (result.Item4 != null ? JsonConvert.SerializeObject(result.Item4) : phoneNumber));
                _kibanaService.LoggingToLoyaltyDB(posNo, router.Route, "PointRedeem", result.Item3, orderNo, posRequest, JsonConvert.SerializeObject(pointsRedeemCapRequest), JsonConvert.SerializeObject(result.Item4), phoneNumber);

                return new Tuple<bool, string, object>(result.Item1, result.Item2, result.Item4);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public Tuple<bool, string, object> PointReverse(LoyaltyRepository loyaltyRepository, RedisManager redisManager, MemoryCacheService _memoryCacheService, string storeNo, string posNo, string phoneNumber, string redemptionId, long pointReverse, string orderNo)
        {
            phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
            
            string appCode = LoyaltyHelper.CheckSwithLoyaltyTheWINX(redisManager, loyaltyRepository, storeNo).ToUpper();

            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == appCode).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "PointReverse".ToUpper() && x.Blocked == false);

                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                PointReverseCapRequest dataRequest = new PointReverseCapRequest()
                {
                    PointsToBeReversed = pointReverse,
                    RedemptionId = redemptionId,
                };
                var identifier = new IdentifiersCapillary()
                {
                    Value = phoneNumber,
                    Type = "mobile"
                };
                dataRequest.Identifier = identifier;

                var result = _capillaryLoyaltyService.PointReverse(_sysWebApiDto, dataRequest, storeNo, _memoryCacheService.GetNotifyConfig(_connectStringDb));

                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, posNo, result.Item3, "", phoneNumber + "@" + (result.Item4 != null ? JsonConvert.SerializeObject(result.Item4) : phoneNumber));
                _kibanaService.LoggingToLoyaltyDB(posNo, router.Route, "PointReverse", result.Item3, orderNo, JsonConvert.SerializeObject(identifier), JsonConvert.SerializeObject(dataRequest), JsonConvert.SerializeObject(result.Item4), phoneNumber);

                return new Tuple<bool, string, object>(result.Item1, result.Item2, result.Item4);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public Tuple<bool, string, object> CheckPointsRedeemable(MemoryCacheService _memoryCacheService, string storeNo, string posNo, string phoneNumber, int point)
        {
            phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "PointsRedeemable".ToUpper() && x.Blocked == false);

                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                var result = _capillaryLoyaltyService.PointsRedeemable(_sysWebApiDto, phoneNumber, point, storeNo, _memoryCacheService.GetNotifyConfig(_connectStringDb));

                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, posNo, result.Item3, "", phoneNumber + "@" + (result.Item4 != null ? JsonConvert.SerializeObject(result.Item4) : phoneNumber));

                return new Tuple<bool, string, object>(result.Item1, result.Item2, result.Item4);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public Tuple<bool, string, object> CouponIsRedeemable(MemoryCacheService _memoryCacheService, string storeNo, string posNo, string phoneNumber, string code, bool details)
        {
            int statusCode = 0;
            phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "CouponIsRedemption".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);
                string param = @"?mobile=" + phoneNumber + "&code=" + code + "&details=" + details;

                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, posNo, "", FormatHelper.PhoneNumberVietNam(phoneNumber));
                var result = _capillaryLoyaltyService.RedemptionValdationData(_sysWebApiDto, storeNo, phoneNumber, code, details, ref statusCode, _memoryCacheService.GetNotifyConfig(_connectStringDb));
                                
                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, posNo, result.Item3, "", FormatHelper.PhoneNumberVietNam(phoneNumber) + "@" + (result.Item4 != null ? JsonConvert.SerializeObject(result.Item4) : phoneNumber));
                //_kibanaService.LoggingToLoyaltyDB(posNo, router.Route, "CouponIsRedeemable", result.Item3, phoneNumber, FormatHelper.PhoneNumberVietNam(phoneNumber), param, JsonConvert.SerializeObject(result.Item4));

                return new Tuple<bool, string, object>(result.Item1, result.Item2, result.Item4);
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ex.Message.ToString(), null);
            }
        }
        public Tuple<bool, string, ValidateVoucherCapillaryResponse> ValidateVouchersCapillary(List<SysWebApiDto> _lstSysWebApiDto, ValidateVoucherCapillary voucherData, string storeNo, string posNo, string phoneNumber)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
            var _sysWebApiDto = _lstSysWebApiDto?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, ValidateVoucherCapillaryResponse>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "ValidateVouchers".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, ValidateVoucherCapillaryResponse>(false, MessageConst.ApiNotFounDataConfig, null);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" }
                };

                string param = $"?store_id={storeNo}&xn_mobile={phoneNumber}";
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, posNo, "", param);
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(_sysWebApiDto.UserName + "." + storeNo, _sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(_sysWebApiDto.Host, router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(voucherData),
                                                                NumberHelper.GetTimeOutApi(_sysWebApiDto.Version),
                                                                _sysWebApiDto.HttpProxy,
                                                                _sysWebApiDto.Bypasslist
                                                                );

                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                StringHelper.Loggingz(response);
                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, posNo, responseTime, "", FormatHelper.PhoneNumberVietNam(phoneNumber) + $"vouchers: {StringHelper.ObjectToStringLowercase(voucherData.Vouchers)}__response:" + response ?? JsonConvert.SerializeObject(responseErr));
                if (!string.IsNullOrEmpty(response) && responseErr.StatusCode == 200)
                {
                    var dataResponse = StringHelper.StringToObject<ValidateVoucherCapillaryResponse>(response);
                    if(dataResponse != null && dataResponse.Vouchers != null)
                    {
                        return new Tuple<bool, string, ValidateVoucherCapillaryResponse>(true, $"OK", dataResponse);
                    }
                    return new Tuple<bool, string, ValidateVoucherCapillaryResponse>(false, response, null);
                }
                else if (responseErr!= null && _capillaryLoyaltyService.httpStatusErrorCapillary.Contains(responseErr.StatusCode.ToString()))
                {
                    return new Tuple<bool, string, ValidateVoucherCapillaryResponse>(false, $"HTTP {responseErr.StatusCode} Cappillary Server Error", null);
                }
                else if (response.ToUpper().Contains("Unauthorized".ToUpper()))
                {
                    return new Tuple<bool, string, ValidateVoucherCapillaryResponse>(false, $"Cửa hàng {storeNo} chưa được đăng ký trên Capillary", null);
                }
                else
                {
                    return new Tuple<bool, string, ValidateVoucherCapillaryResponse>(false, JsonConvert.SerializeObject(responseErr), null);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, ValidateVoucherCapillaryResponse>(false, $"ValidateVouchersCapillary.Exception: {ex.Message}", null);
            }
        }
        public Tuple<bool, string, object> CouponRedeemCapillary(LoyaltyRepository loyaltyRepository, RedisManager redisManager, MemoryCacheService _memoryCacheService, string storeNo, string posNo, string orderNo, string phoneNumber, List<LstVoucherPartner> codes)
        {
            long responseTime = 0;
            string response = "";
            var request_id = Guid.NewGuid().ToString();
            ServerErrorResponses responseErr = new ServerErrorResponses();
            phoneNumber = FormatHelper.PhoneNumberWithCountryCode(phoneNumber);
            var sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == LoyaltyHelper.CheckSwithLoyaltyTheWINX(redisManager,loyaltyRepository,storeNo) .ToUpper()).FirstOrDefault();
            if (sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "CouponRedeem".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                List<CodeCouponCapillary> lstCode = new List<CodeCouponCapillary>();
                foreach (var code in codes)
                {
                    lstCode.Add(new CodeCouponCapillary()
                    {
                        Code = code.Code,
                    }
                    );
                }
                var bodyData = new RedeemCouponRequestCap()
                {
                    RedemptionRequestList = lstCode.ToList(),
                    TransactionNumber = orderNo,
                    User = new UserCapillary() { Mobile = phoneNumber }
                };
                _kibanaService.LogRequest(sysWebApiDto.Host + router.Route, posNo, "", $"{orderNo}_{FormatHelper.PhoneNumberVietNam(phoneNumber)}_request:" + StringHelper.ObjectToStringLowercase(bodyData));
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + storeNo, sysWebApiDto.Password);
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-API-REQUEST", request_id },
                    { "x-winx-loyalty-client", "POS" }
                };

                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, router.Route + router.Notes ?? "",
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(bodyData),
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );

                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                StringHelper.Loggingz("CouponRedeemCapillary:" + response);
                _kibanaService.LogResponse(sysWebApiDto.Host + router.Route, posNo, responseTime, "", FormatHelper.PhoneNumberVietNam(phoneNumber) + $"_with requestId:{request_id} === {JsonConvert.SerializeObject(codes)}_" + response ?? JsonConvert.SerializeObject(responseErr));
                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = StringHelper.StringToObject<RedeemCouponResponseCap>(response);
                    if (dataResponse != null && dataResponse.Redemption != null)
                    {
                        return new Tuple<bool, string, object>(true, "OK", dataResponse);
                    }
                    else
                    {
                        _kibanaService.SendMessageSMS($"OrderNo {orderNo} redeem listVouchers: {JsonConvert.SerializeObject(codes)} ===> with response: {response}", MessageTypeEnum.Warning.ToString(), $"{orderNo} lỗi khi sử dụng voucher **Capillary**", appCode: AppCodeMessageEnum.BLUEPOS_CAP_ERROR.ToString());
                        return new Tuple<bool, string, object>(false, response, null);
                    }
                }
                else if (responseErr != null && _capillaryLoyaltyService.httpStatusErrorCapillary.Contains(responseErr.StatusCode.ToString()))
                {
                    _kibanaService.SendMessageSMS($"OrderNo {orderNo} redeem listVouchers: {JsonConvert.SerializeObject(codes)} with response: {response}", MessageTypeEnum.Warning.ToString(), $"**Capillary** lỗi HttpStatus: {responseErr.StatusCode}", appCode: AppCodeMessageEnum.BLUEPOS_CAP_ERROR.ToString());
                    return new Tuple<bool, string, object>(false, $"HTTP {responseErr.StatusCode} Cappillary Server Error", null);
                }
                else
                {
                    return new Tuple<bool, string, object>(false, JsonConvert.SerializeObject(responseErr), null);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, $"CouponRedeemCapillary.Exception: {ex.Message}", null);
            }
        }
        public Tuple<bool, string, object> GetSerialInfo(MemoryCacheService _memoryCacheService, string storeNo, string posNo, string serial_id)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetSerialDetail".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" }
                };

                string param = $"?ids={serial_id}";
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, serial_id, "", param);
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(_sysWebApiDto.UserName + "." + storeNo, _sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(_sysWebApiDto.Host, router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(_sysWebApiDto.Version),
                                                                _sysWebApiDto.HttpProxy,
                                                                _sysWebApiDto.Bypasslist
                                                                );

                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                FileHelper.WriteLogs(response);
                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, posNo, responseTime, "", $"series_id: {serial_id}--" + response ?? JsonConvert.SerializeObject(responseErr));

                return new Tuple<bool, string, object>(false, response, null);
                //if (!string.IsNullOrEmpty(response) && responseErr.StatusCode == 200)
                //{
                //    var dataResponse = JsonConvert.DeserializeObject<CheckActiveCouponResponseCap>(response);
                //    if (dataResponse != null)
                //    {
                //        if (dataResponse.Response.Customers != null && dataResponse.Response.Customers.Customer.FirstOrDefault().Coupons.Coupon != null)
                //        {
                //            return new Tuple<bool, string, object>(true,
                //                                                $"Serial {serial_id} hợp lệ",
                //                                                dataResponse.Response.Customers.Customer.FirstOrDefault().Coupons.Coupon);
                //        }
                //        else
                //        {
                //            return new Tuple<bool, string, object>(false,
                //                                                $"Serial {serial_id}  không hợp lệ",
                //                                                dataResponse.Response.Customers.Customer.FirstOrDefault().Coupons.Coupon);
                //        }
                //    }
                //    else
                //    {
                //        return new Tuple<bool, string, object>(false, response, null);
                //    }
                //}
                //else if (responseErr.StatusCode == (int)HttpStatusCode.BadRequest)
                //{
                //    return new Tuple<bool, string, object>(false, $"Serial {serial_id} không hợp lệ", response);
                //}
                //else
                //{
                //    return new Tuple<bool, string, object>(false, $"Lỗi kết nối WebApi Capillary", responseErr);
                //}
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, $"Lỗi GetSerialInfo.Exception: {ex.Message}", ex);
            }
        }
        public Tuple<bool, string, object> UpdateMobileEnroll(MemoryCacheService _memoryCacheService, string storeNo, string posNo, string phoneNumber, string mobile_enroll)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "UpdateMobileEnroll".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" }
                };
                var fields = new List<FieldCapillary>
                {
                    new FieldCapillary()
                    {
                        Name = "pos_enroll",
                        Value = mobile_enroll
                    },
                    new FieldCapillary()
                    {
                        Name = "store_id",
                        Value = storeNo
                    }
                };
                var custom_fields = new Custom_fields()
                {
                    Field = fields
                };

                var mobileData = new MobileEnrollCustomer
                {
                    Mobile = FormatHelper.PhoneNumberWithCountryCode(phoneNumber),
                    Custom_fields = custom_fields
                };

                var customerData = new List<MobileEnrollCustomer>
                {
                    mobileData
                };

                var requestBody = new UpdateMobileEnrollRequest
                {
                    Root = new MobileEnrollRoot()
                    {
                        Customer = customerData
                    }
                };
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, posNo, "", $"{mobileData.Mobile}|{mobile_enroll}|" + JsonConvert.SerializeObject(requestBody));
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(_sysWebApiDto.UserName + "." + storeNo, _sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(_sysWebApiDto.Host, router.Route,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(requestBody),
                                                                NumberHelper.GetTimeOutApi(_sysWebApiDto.Version),
                                                                _sysWebApiDto.HttpProxy,
                                                                _sysWebApiDto.Bypasslist
                                                                );
                
                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, posNo, responseTime, "", $"{mobileData.Mobile}|{mobile_enroll}|" + response ?? JsonConvert.SerializeObject(responseErr));
                return new Tuple<bool, string, object>(true, StringHelper.ObjectToStringLowercase(requestBody), null);
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("UpdateMobileEnroll", posNo, 0, "", $"{phoneNumber}|{mobile_enroll}|" + JsonConvert.SerializeObject(ex));    
                return new Tuple<bool, string, object>(false, $"Lỗi UpdateMobileEnroll.Exception: {ex.Message}", ex);
            }
        }
        public Tuple<bool, string, object> GetTransactionIdDetail(MemoryCacheService _memoryCacheService, string storeNo, string posNo, long transactionId)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            var _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
            if (_sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var router = _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetTransIdDetail".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" }
                };

                string param = $"/{transactionId}?mlp=true&points_breakup=true";
                _kibanaService.LogRequest(_sysWebApiDto.Host + router.Route, transactionId.ToString(), "", param);
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(_sysWebApiDto.UserName + "." + storeNo, _sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(_sysWebApiDto.Host, router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(_sysWebApiDto.Version),
                                                                _sysWebApiDto.HttpProxy,
                                                                _sysWebApiDto.Bypasslist
                                                                );

                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                FileHelper.WriteLogs(response);
                _kibanaService.LogResponse(_sysWebApiDto.Host + router.Route, posNo, responseTime, "", $"orderNo: {transactionId}--" + response ?? JsonConvert.SerializeObject(responseErr));
                
                return new Tuple<bool, string, object>(true, response, null);
                //if (!string.IsNullOrEmpty(response) && responseErr.StatusCode == 200)
                //{
                //    var dataResponse = StringHelper.StringToObject<TransactionDetailCapillaryResponse>(response);
                //    if (dataResponse != null && dataResponse.Data != null)
                //    {
                //        return new Tuple<bool, string, TransactionDetailCapillaryResponse>(true, "OK", dataResponse);
                //    }
                //    else
                //    {
                //        return new Tuple<bool, string, TransactionDetailCapillaryResponse>(false, response, null);
                //    }
                //}
                //else
                //{
                //    return new Tuple<bool, string, TransactionDetailCapillaryResponse>(false, $"Lỗi kết nối WebApi Capillary", null);
                //}
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, $"Lỗi GetSerialInfo.Exception: {ex.Message}", null);
            }
        }
        public async Task<Tuple<bool, string, object>> PointToUp(MemoryCacheService _memoryCacheService, RedisManager redisManager, LoyaltyRepository loyaltyRepository, PointTopupPOSRequest pointTopupPOSRequest)
        {
            long responseTime = 0;
            string response = "";
            var request_id = Guid.NewGuid().ToString();
            ServerErrorResponses responseErr = new ServerErrorResponses();
            pointTopupPOSRequest.PhoneNumber = FormatHelper.PhoneNumberWithCountryCode(pointTopupPOSRequest.PhoneNumber);

            string appCode = LoyaltyHelper.CheckSwithLoyaltyTheWINX(redisManager, loyaltyRepository, pointTopupPOSRequest.StoreNo).ToUpper();

            var sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == appCode).FirstOrDefault();
            if (sysWebApiDto == null) return new Tuple<bool, string, object>(false, MessConst.NotFounDataConfig, null);
            try
            {
                var programIdStaff = LoyaltyExtentionService.GetProgramIdCapillary(_memoryCacheService.GetCacheConfigLoyalty(_memoryCacheService.GetStringDbLoyalty()), LoyaltyProgramEnum.CapillaryStaffMembershipPoints.ToString(), true);
                if (programIdStaff == null || (programIdStaff != null && programIdStaff.FirstOrDefault() == null))
                {
                    return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);
                }

                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "PointTopUp".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);

                var requestTopUpCapi = new List<RequestTopupCapillaryRequest>
                {
                    new RequestTopupCapillaryRequest()
                    {
                        Customer = new CustomerTopupCapillary() { Mobile = pointTopupPOSRequest.PhoneNumber },
                        Comments = pointTopupPOSRequest.Comments,
                        Reason = pointTopupPOSRequest.OrderNo,
                        Type = "GOODWILL",
                        Base_type = "POINTS",
                        Points = pointTopupPOSRequest.Points.ToString()
                    }
                };

                var bodyData = new PointTopupCapillaryRequest()
                {
                    Root = new RootTopupCapillaryRequest() { Request = requestTopUpCapi }
                };
                
                _kibanaService.LogRequest(sysWebApiDto.Host + router.Route, pointTopupPOSRequest.PosNo, "", $"{pointTopupPOSRequest.OrderNo}_{FormatHelper.PhoneNumberVietNam(pointTopupPOSRequest.PhoneNumber)}_request:" + StringHelper.ObjectToStringLowercase(bodyData));
                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + pointTopupPOSRequest.StoreNo, sysWebApiDto.Password);
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-API-REQUEST", request_id },
                };
                var param = $"{router.Route}{router.Notes ?? ""}" + $"&program_id={programIdStaff.FirstOrDefault()}";
                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, 
                                                                param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(bodyData),
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );

                var result = await apiCallHelper.HttpClientWithApiAsync();
                StringHelper.Loggingz($"PointToUp: {sysWebApiDto.Host + param} response: " + JsonConvert.SerializeObject(result));
                response = result.Item1;
                responseTime = result.Item2;
                
                _kibanaService.LogResponse(sysWebApiDto.Host + router.Route, pointTopupPOSRequest.PosNo, responseTime, "", FormatHelper.PhoneNumberVietNam(pointTopupPOSRequest.PhoneNumber) + $"_with requestId:{request_id} " + response ?? JsonConvert.SerializeObject(result));
                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = StringHelper.StringToObject<PointTopUpCapillaryResponse>(response);
                    if (dataResponse != null && dataResponse.Response != null
                            && dataResponse.Response.Status != null
                            && dataResponse.Response.Status.Code == 200
                            && dataResponse.Response.Requests.Request.FirstOrDefault()!= null 
                            && dataResponse.Response.Requests.Request.FirstOrDefault().Item_status != null
                            && dataResponse.Response.Requests.Request.FirstOrDefault().Item_status.Code == 9100)
                    {
                        return new Tuple<bool, string, object>(true, dataResponse.Response.Requests.Request.FirstOrDefault().Item_status.Message??"OK", dataResponse);
                    }
                    else
                    {
                        return new Tuple<bool, string, object>(false, response, null);
                    }
                }
                else if (responseErr != null && _capillaryLoyaltyService.httpStatusErrorCapillary.Contains(responseErr.StatusCode.ToString()))
                {
                    _kibanaService.SendMessageSMS($"**Topup Points (Issue GoodWill Points)** requestId: {request_id} with response: {response}", MessageTypeEnum.Error.ToString(), $"**Capillary** lỗi HttpStatus: {responseErr.StatusCode}", appCode: AppCodeMessageEnum.BLUEPOS_CAP_ERROR.ToString());
                    return new Tuple<bool, string, object>(false, $"HTTP {responseErr.StatusCode} Cappillary Server Error", null);
                }
                else
                {
                    return new Tuple<bool, string, object>(false, JsonConvert.SerializeObject(responseErr), null);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, $"PointToUpCapillary.Exception: {ex.Message}", null);
            }
        }
        public async Task<Tuple<bool, string, object>> RevertTopUpPoints(MemoryCacheService _memoryCacheService, RevertTopUpPointsCapRequest request, string phoneNumber, string storeNo)
        {
            try
            {
                var sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringDb)?.Where(x => x.AppCode.ToUpper() == AppCodeEnum.Capillary.ToString().ToUpper()).FirstOrDefault();
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "RevertTopUpPoints".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);
                var param = router.Route + StringHelper.ReplaceString(router.Notes, "xxx", FormatHelper.PhoneNumberWithCountryCode(phoneNumber));

                var programIdStaff = LoyaltyExtentionService.GetProgramIdCapillary(_memoryCacheService.GetCacheConfigLoyalty(_memoryCacheService.GetStringDbLoyalty()), LoyaltyProgramEnum.CapillaryStaffMembershipPoints.ToString());
                if (programIdStaff == null || (programIdStaff != null && programIdStaff.FirstOrDefault() == null))
                {
                    return new Tuple<bool, string, object>(false, MessageConst.ApiNotFounDataConfig, null);
                }
                request.ProgramId = NumberHelper.TryParseInt(programIdStaff.FirstOrDefault());

                //create token
                string accessToken = EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + storeNo, sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host,
                                                                param,
                                                                null,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(request),
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );
                _kibanaService.LogRequest(sysWebApiDto.Host + param, storeNo, "", $"{phoneNumber} {StringHelper.ObjectToStringLowercase(request)}");
                var result = await apiCallHelper.HttpClientWithApiAsync();

                StringHelper.Loggingz($"{sysWebApiDto.Host + param} RevertTopUpPoints.Response: {JsonConvert.SerializeObject(result)}");
                _kibanaService.LogResponse(sysWebApiDto.Host + param, storeNo, result.Item2, "", $"{phoneNumber} {JsonConvert.SerializeObject(result)}");
                var checkResponse = StringHelper.StringToObject<RevertTopUpPointsCapResponse>(result.Item1);

                if (checkResponse != null && checkResponse.Status.Contains("success"))
                {
                    return new Tuple<bool, string, object>(true, checkResponse.Message, checkResponse);
                }
                else
                {
                    return new Tuple<bool, string, object>(false, checkResponse.Message, checkResponse);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string, object>(false, ExceptionHelper.GetExceptionMessage(ex), ex.InnerException);
            }
        }
        //function
        public string GetValueFieldCapillary(List<FieldCapillary> fieldCapillaries, string name)
        {
            string result = string.Empty;
            try
            {
                var data = fieldCapillaries.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
                if(data!= null)
                {
                    result = data.Value??"";
                }
            }
            catch {
                return result;
            }
            return result;
        }
        public async Task<List<LoyaltyRetryDto>> LoyaltyRetry(LoyaltyRepository loyaltyRepository, RedisManager _redisManager, MemoryCacheService _memoryCacheService)
        {
            try
            {
                int i = 0;
                List<LoyaltyRetryDto> result = new List<LoyaltyRetryDto>();
                bool checkStatus = false;
                var addTransaction = _redisManager.GetAllKeys(_parentAddTransaction);
                var returnTransaction = _redisManager.GetAllKeys(_parentReturnTransaction);
                var configMess = _memoryCacheService.GetNotifyConfig(_connectStringLoyaltyDb).Where(x => x.AppCode == "CAP" && x.IsOffline == true).Select(x => x.Status).ToArray();
                //FileHelper.WriteLogs(JsonConvert.SerializeObject(configMess));
                if (addTransaction.Count > 0)
                {                   
                    FileHelper.WriteLogs(string.Format("Redis {0} : {1} ", _parentAddTransaction, addTransaction.Count.ToString()));
                    foreach (var key in addTransaction)
                    {
                        i++;
                        var bodyJson = _redisManager.StringGetAsync<AddTransactionCapPOSRequest>(key).Result;
                        var checkRetry = await AddTransactionToCap(loyaltyRepository , _redisManager, _memoryCacheService, bodyJson);
                        FileHelper.WriteLogs("Response: " + JsonConvert.SerializeObject(checkRetry));
                        foreach(var status in configMess)
                        {
                            if (checkRetry.Item2.Contains(status))
                            {
                                checkStatus = true ;
                                break;
                            }
                        }
                        if (checkRetry.Item1 || checkRetry.Item2.Contains("604") || checkStatus)
                        {
                            result.Add(new LoyaltyRetryDto()
                            {
                                OrderNo = bodyJson.BillNumber,
                                PhoneNumber = bodyJson.MemberCard,
                                Status = "Success"
                            });
                            _redisManager.Delete(key);
                            FileHelper.WriteLogs("LoyaltyRetry processed: " + key);
                        }
                        if (i == 20)
                        {
                            i = 0;
                            break;
                        }
                    }
                }

                if (returnTransaction.Count > 0)
                {
                    FileHelper.WriteLogs(string.Format("Redis {0} : {1} ", _parentReturnTransaction, returnTransaction.Count.ToString()));
                    foreach (var key in returnTransaction)
                    {
                        var bodyJson = _redisManager.StringGetAsync<AddTransactionCapPOSRequest>(key).Result;
                        var checkRetry = await ReturnTransactionToCap(loyaltyRepository, _redisManager, _memoryCacheService, bodyJson, "Retry", bodyJson.BillNumber);
                        i++;
                        foreach (var status in configMess)
                        {
                            if (checkRetry.Item2.Contains(status))
                            {
                                checkStatus = true;
                                break;
                            }
                        }

                        if (checkRetry.Item1 || checkRetry.Item2.Contains("604") || checkStatus)
                        {
                            result.Add(new LoyaltyRetryDto()
                            {
                                OrderNo = bodyJson.BillNumber,
                                PhoneNumber = bodyJson.MemberCard,
                                Status = "Success"
                            });
                            _redisManager.Delete(key);
                            FileHelper.WriteLogs("LoyaltyRetry processed: " + key);
                        }
                        if (i == 20)
                        {
                            i = 0;
                            break;
                        }
                    }
                }
                return result;
            }
            catch(Exception ex) 
            {
                FileHelper.WriteExpLogs("LoyaltyRetry from Redis: ", ex);
                return null;
            }
        }
        public VinIDSalesRequest MappingAddTransactionCAP(List<TransactionOfflineDto> transactionOfflineDtos)
        {
            var fistRow = transactionOfflineDtos.FirstOrDefault(x => x.TableName == "TransHeader");
            var lines = transactionOfflineDtos.Where(x=>x.TableName== "TransLine").ToList();
            var payment = transactionOfflineDtos.Where(x => x.TableName == "TransPaymentEntry").ToList();

            string billNumber = fistRow.OrderNo;
            if(fistRow.TransactionType == 3)
            {
                billNumber = fistRow.OrigOrderNo;
            }

            VinIDSalesRequest result = new VinIDSalesRequest
            {
                QRCode = "",
                CardNumber = fistRow.CardNumber,
                MerchantId = fistRow.StoreNo,
                TerminalId = fistRow.PosNo,
                InvoiceNo = fistRow.OrderNo,
                SpendPoints = 0,
                BillAmount = lines.Where(x => x.LineAmountIncVAT > 0).Sum(x => x.LineAmountIncVAT),
                OrderNo = fistRow.OrderNo,
                IsOffline = true,
                OrderAmount = lines.Where(x => x.LineAmountIncVAT > 0).Sum(x=>x.LineAmountIncVAT),
                VirtualCard = "",
                OrderTime = fistRow.OrderTime,
                TransactionType = fistRow.TransactionType,
                OrigOrderNo = fistRow.OrigOrderNo
            };

            List<TransLineLoyalty> translines = new List<TransLineLoyalty>();
            foreach(var line in lines)
            {
                translines.Add(new TransLineLoyalty()
                {
                    ItemCode = line.ItemNo,
                    Description = line.Description,
                    UnitOfMeasure = line.UnitOfMeasure,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    DiscountAmount = line.DiscountAmount,
                    VatAmount = line.VatAmmount,
                    LineAmountIncVAT = line.LineAmountIncVAT,
                    DiscountType = 0, 
                    Size = ""
                });
            }
            List<PaymentEntryLoyalty> transPayment = new List<PaymentEntryLoyalty>();
            int i = 1;
            foreach(var p in payment)
            {
                transPayment.Add(new PaymentEntryLoyalty()
                {
                    LineNo = i * 1000,
                    TenderType = p.TenderType,
                    AmountTendered = p.AmountTendered,
                    CardType = p.CardType
                });
                i++;
            }
            result.TransPaymentEntry = transPayment;
            result.TransLine = translines;
            return result;
        }
        public VinIDRefundRequest MappingReturnTransactionCAP(List<TransactionOfflineDto> transactionOfflineDtos)
        {
            var fistRow = transactionOfflineDtos.FirstOrDefault(x => x.TableName == "TransHeader");
            var lines = transactionOfflineDtos.Where(x => x.TableName == "TransLine").ToList();
            var payment = transactionOfflineDtos.Where(x => x.TableName == "TransPaymentEntry").ToList();

            string billNumber = fistRow.OrderNo;
            if (fistRow.TransactionType == 3)
            {
                billNumber = fistRow.OrigOrderNo;
            }

            VinIDRefundRequest result = new VinIDRefundRequest
            {
                QRCode = "",
                CardNumber = fistRow.CardNumber,
                MerchantId = fistRow.StoreNo,
                TerminalId = fistRow.PosNo,
                InvoiceNo = fistRow.OrderNo,
                SpendPoints = 0,
                RedemptionId = "",
                RefundAmount = lines.Where(x => x.LineAmountIncVAT > 0).Sum(x => x.LineAmountIncVAT),
                OrderNo = fistRow.OrderNo,
                IsOffline = true,
                OrderAmount = lines.Where(x => x.LineAmountIncVAT > 0).Sum(x => x.LineAmountIncVAT),
                IsScanAndGo = false,
                OrderTime = fistRow.OrderTime,
                TransactionType = fistRow.TransactionType,
                OrigOrderNo = fistRow.OrigOrderNo
            };

            List<TransLineLoyalty> translines = new List<TransLineLoyalty>();
            foreach (var line in lines)
            {
                translines.Add(new TransLineLoyalty()
                {
                    ItemCode = line.ItemNo,
                    Description = line.Description,
                    UnitOfMeasure = line.UnitOfMeasure,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    DiscountAmount = line.DiscountAmount,
                    VatAmount = line.VatAmmount,
                    LineAmountIncVAT = line.LineAmountIncVAT,
                    DiscountType = 0,
                    Size = ""
                });
            }
            List<PaymentEntryLoyalty> transPayment = new List<PaymentEntryLoyalty>();
            int i = 1;
            foreach (var p in payment)
            {
                transPayment.Add(new PaymentEntryLoyalty()
                {
                    LineNo = i * 1000,
                    TenderType = p.TenderType,
                    AmountTendered = p.AmountTendered,
                    CardType = p.CardType
                });
                i++;
            }
            result.TransPaymentEntry = transPayment;
            result.TransLine = translines;
            return result;
        }
        public string GetMessageCapillary(List<NotifyConfigDto> notifyConfigDto, string actionType, string response, string code = "")
        {
            return _capillaryLoyaltyService.GetMessageCapillary(notifyConfigDto, actionType, response, code);
        }
    }
}