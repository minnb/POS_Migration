using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using TCX.API.Common.Const;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Capillary;
using TCX.API.Common.Dtos.Capillary.Coupons;
using TCX.API.Common.Dtos.Capillary.Customer;
using TCX.API.Common.Dtos.Capillary.Point;
using TCX.API.Common.Dtos.Capillary.Transaction;
using TCX.API.Common.Dtos.Capillary.Vouchers;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Loyalty.CX;
using TCX.API.Common.Dtos.Loyalty.MemberBusiness;
using TCX.API.Common.Dtos.Loyalty.ProgramPoints;
using TCX.API.Common.Dtos.Ops;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore.AppServices;
using TCX.WebApiCore.AppServices.TCB;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;
using TCX.WebApiCore.Shared;
using VCM.POSBLUE.Model.VINID;
using VCM.POSBLUE.Model.WinLife;

namespace TCX.WebApiCore
{
    public class LoyaltyService
    {
        private readonly RedisCacheService _redisCacheService;
        private readonly KibanaService _kibanaService;
        private readonly LoyaltyCapillaryService _loyaltyCapillaryService;
        private readonly WincodeService _wincodeService;
        private readonly MemberBusinessService _memberBusinessService;
        private readonly LoyaltyRepository _loyaltyRepository;
        private readonly LoyaltyExtentionService _loyaltyExtentionService;
        private readonly WinXService _winXService;
        private readonly Lazy<ProgramPointsService> _programPointsService;
        private readonly Lazy<WinsoreService> _winsoreService;
        private readonly Lazy<WinCareService> _winCareService;
        private readonly CentralMDRepository _centralMDRepository;
        public LoyaltyService()
        {
            _loyaltyCapillaryService = new LoyaltyCapillaryService();
            _redisCacheService = new RedisCacheService();
            _kibanaService = new KibanaService();
            _memberBusinessService = new MemberBusinessService();
            _loyaltyRepository = new LoyaltyRepository();
            _loyaltyExtentionService = new LoyaltyExtentionService();
            _wincodeService = new WincodeService();
            _winXService = new WinXService();
            _programPointsService = new Lazy<ProgramPointsService>(() => new ProgramPointsService());
            _winCareService = new Lazy<WinCareService>(() => new WinCareService());
            _winsoreService = new Lazy<WinsoreService>(() => new WinsoreService());
            _centralMDRepository = new CentralMDRepository();

        }
        public Tuple<bool, string, object> CustomerRegistration(RedisManager redisManager, MemoryCacheService _memoryCacheService, WinLife_Register_POS_Request modelPOS, string channel)
        {
            return _loyaltyCapillaryService.CustomerRegistration(_loyaltyRepository, redisManager, _memoryCacheService, modelPOS, channel);
        }
        public Tuple<bool, string, object> CustomerUpdate(MemoryCacheService _memoryCacheService, WinLife_SmartPOS_Update_Customer_Req_POS customer, string acquisition_channel)
        {
            return _loyaltyCapillaryService.CustomerUpdate(_memoryCacheService, customer, acquisition_channel);
        }
        public async Task<ResultResponse> GetInfoMemberCapillary(RedisManager redisManager, MemoryCacheService _memoryCacheService, string codeValue, string storeNo, string posID, string memberCapillary, string appCode = "PLH", bool isMobile = false)
        {
            bool isWincare = false;
            bool isRedeem = false;
            string memberCsn = codeValue;
            ResultResponse httpResponseMessage;
            string inputType = LoyaltyExtentionService.GetInputTypeMember(memberCapillary, codeValue);
            string virtualCard = "";
            string member_type = "WIN";
            if (memberCapillary == MemberCapillaryEnum.PHONE.ToString())
            {
                codeValue = FormatHelper.PhoneNumberWithCountryCode(codeValue);
                if (!RegexHelper.IsCharacters(codeValue))
                {
                    return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, "Số điện thoại Hội viên không hợp lệ, vui lòng nhập lại thông tin", null);
                }
            }
            else if (memberCapillary == MemberCapillaryEnum.WINCARE.ToString())
            {
                var dataWincare = await _winCareService.Value.GetInfoCustomerStaff(redisManager, codeValue, posID);
                if (dataWincare.Item1 == true)
                {
                    memberCapillary = MemberCapillaryEnum.ID.ToString();
                    codeValue = dataWincare.Item2;
                    inputType = $"WIN-{inputType}-{dataWincare.Item4}";
                    virtualCard = dataWincare.Item3;
                    isWincare = true;
                }
                else
                {
                    return ResponseHelper.ResponseData(HttpStatusCode.NotFound, dataWincare.Item2, null, messageTechnical: dataWincare.Item3);
                }
            }
            else if (memberCapillary == MemberCapillaryEnum.WINX.ToString())
            {
                var dataWinx = await _winXService.ResolveWinXUserIdCapillary(_memoryCacheService, posID, codeValue);
                if (!string.IsNullOrEmpty(dataWinx.Item1))
                {
                    memberCapillary = MemberCapillaryEnum.ID.ToString();
                    codeValue = dataWinx.Item1;
                    inputType = $"{inputType}-{codeValue}";
                    virtualCard = dataWinx.Item3;
                    isWincare = false;
                }
                else
                {
                    return ResponseHelper.ResponseData(HttpStatusCode.NotFound, dataWinx.Item2, null, messageTechnical: dataWinx.Item3);
                }
            }
            try
            {
                var sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetStringDbCentralMD());
                var result = await _loyaltyCapillaryService.GetCustomerLoyaltyDetail(_loyaltyRepository, redisManager, _memoryCacheService, sysWebApiDto, storeNo, posID, codeValue, memberCapillary);
                int statusCode = result.Item4;
                if (result.Item1)
                {
                    var getMemmoryCacheConfigLoyalty = _memoryCacheService.GetCacheConfigLoyalty(_loyaltyRepository.ConnectStringLoyaltyDb());
                    var dataCustDetail = JsonConvert.DeserializeObject<GetCustDetailCapillaryResponse>(JsonConvert.SerializeObject(result.Item3));
                    var modelCustomer = dataCustDetail.Response.Customers.Customer.FirstOrDefault();
                    var extended_fields = modelCustomer.Extended_fields.Field;
                    var pointSummary = modelCustomer.Points_summaries.Points_summary;
                    bool birthdayGiftInd = false;

                    if (isWincare && !string.IsNullOrEmpty(_loyaltyCapillaryService.GetValueFieldCapillary(extended_fields, "member_type"))
                        && _loyaltyCapillaryService.GetValueFieldCapillary(extended_fields, "member_type") == MemberTypeCapillaryEnum.STAFF.ToString()
                        && LoyaltyExtentionService.CheckStoreApplyStaffPoints(_memoryCacheService, storeNo))
                    {
                        isRedeem = true;
                        member_type = MemberTypeCapillaryEnum.STAFF.ToString();
                    }
                    else if (!isWincare && inputType.Contains(MemberTypeCapillaryEnum.WINX.ToString()))
                    {
                        isRedeem = true;
                        member_type = MemberTypeCapillaryEnum.WINX.ToString();
                    }

                    //get points
                    var dataPoint = LoyaltyExtentionService.GetPoints(getMemmoryCacheConfigLoyalty, modelCustomer, pointSummary, isWincare, member_type);

                    //check member business 
                    MemberBusinessData memberBusinessData = LoyaltyExtentionService.GetMemberBusinessData(_memberBusinessService, getMemmoryCacheConfigLoyalty, storeNo, posID, codeValue);

                    (string, List<ExtendedField>) extendedField = (null, null);
                    extendedField = GetOtherStatusLoyalty(getMemmoryCacheConfigLoyalty, _memoryCacheService, sysWebApiDto, storeNo, posID, codeValue, _loyaltyCapillaryService.GetValueFieldCapillary(modelCustomer.Custom_fields.Field, "pos_enroll") ?? "");

                    //get virtualCard 
                    if (string.IsNullOrEmpty(virtualCard))
                    {
                        virtualCard = modelCustomer.User_id ?? modelCustomer.Mobile;
                    }
                    var pointConversion = CapillaryHelper.PointConversionVND(_memoryCacheService, dataPoint.Item2);
                    InfoMemberModel custDetail = new InfoMemberModel
                    {
                        CardNumber = FormatHelper.PhoneNumberVietNam(modelCustomer.Mobile),
                        VirtualCard = virtualCard,
                        CMND = "",
                        MemberName = modelCustomer.Firstname,
                        Title = FormatHelper.GetTitleGender(_loyaltyCapillaryService.GetValueFieldCapillary(extended_fields, "gender") ?? ""),
                        CardLevel = _loyaltyExtentionService.CardLevelMapping(_memoryCacheService, appCode, modelCustomer.Current_slab),
                        MemberCSN = memberCsn,
                        PhoneNumber = FormatHelper.PhoneNumberVietNam(modelCustomer.Mobile),
                        OtherInfo = _loyaltyCapillaryService.GetValueFieldCapillary(extended_fields, "acquisition_channel") ?? "",
                        QRCode = string.IsNullOrEmpty(virtualCard) == false ? $"{inputType}" : "",
                        Dob = _loyaltyCapillaryService.GetValueFieldCapillary(extended_fields, "dob_date") ?? "",
                        DateOfBirth = _loyaltyCapillaryService.GetValueFieldCapillary(extended_fields, "dob_date") ?? "",
                        BirthdayGiftInd = birthdayGiftInd,//Quà tặng sinh nhật
                        MemberPoint = dataPoint.Item2,
                        TotalPoint = dataPoint.Item1,
                        RedemptionValue = pointConversion.Item2,
                        ExtraPoint = false,
                        CurrentRate = pointConversion.Item1,
                        IsOfflineVinID = false,
                        IsShowMessage = false,
                        IsRedeem = isRedeem,
                        Status = "Hoạt động",
                        System = "CAP",//KM HV WIN
                        ClubCode = MemberCapillaryEnum.WINCARE.ToString(),//KM HV WIN
                        Email = _loyaltyCapillaryService.GetValueFieldCapillary(extended_fields, "email") ?? "",
                        Gender = _loyaltyCapillaryService.GetValueFieldCapillary(extended_fields, "gender") ?? "",
                        Address = _loyaltyCapillaryService.GetValueFieldCapillary(modelCustomer.Custom_fields.Field, "customer_address") ?? "",
                        ExternalId = _loyaltyCapillaryService.GetValueFieldCapillary(extended_fields, "member_type") ?? "",
                        AvailablePromotion = GetWinCodePromotion(storeNo, posID, FormatHelper.PhoneNumberVietNam(modelCustomer.Mobile), modelCustomer.Coupons),
                        MemberBusiness = memberBusinessData,
                        OtherStatus = extendedField.Item1,
                        ExtendedFields = extendedField.Item2,
                        MemberType = member_type,
                        Source = result.Item5,
                        PointsSummaries = await _programPointsService.Value.GetProgramPointsSummaries(_memoryCacheService, storeNo, codeValue),
                    };

                    if (isWincare)
                    {
                        redisManager.StringSet<string>($"{RedisConst.Redis_Key_MASANER_MemberStaff}:{custDetail.VirtualCard}", FormatHelper.PhoneNumberVietNam(modelCustomer.Mobile), 3600);
                    }

                    await Task.Run(() => redisManager.HashSet<InfoMemberModel>(RedisConst.GetRedisKeyLoyaltyBalancePoints(custDetail.PhoneNumber), custDetail.PhoneNumber, custDetail));
                    httpResponseMessage = ResponseHelper.ResponseData(HttpStatusCode.OK, result.Item2 ?? "OK", custDetail);
                }
                else
                {
                    if (result.Item2.Contains("408_requestTimedOut") || result.Item2.Contains("A task was canceled") || result.Item4 == 408)
                    {
                        var objError = StringHelper.StringToObject<ServerErrorResponses>(JsonConvert.SerializeObject(result.Item3));
                        if (objError != null)
                        {
                            objError.DataResponse = "";
                            objError.DataRequest = "";
                        }
                        _kibanaService.LogResponse("TimeoutCapillary", posID, 0, "", $"GetCustomerLoyalty:{posID}_{codeValue}_{isMobile} Request Capillary timeout:{result.Item2}_{JsonConvert.SerializeObject(result.Item3)}");
                        _kibanaService.SendMessageSMS($"**{result.Item2}**", MessageTypeEnum.Warning.ToString(), $"WebApi kiểm tra thông tin hội viên WIN **Capillary**", appCode: AppCodeMessageEnum.BLUEPOS_CAP_TIMEOUT.ToString());
                        await OpsMonitoringHelper.OpsLogging(new Ops_Logging_Data()
                        {
                            Server_code = HostHelper.GetIpAddress(),
                            App_module = "LoyaltyService.GetInfoMemberCapillary",
                            Error_code = "RequestTimeout",
                            Error_type = HttpStatusCode.RequestTimeout.ToString(),
                            Severity = OpsStatusEnum.error.ToString(),
                            Http_status = (int)HttpStatusCode.RequestTimeout,
                            Endpoint = "GetInfoMemberCapillary",
                            Error_message = JsonConvert.SerializeObject(result.Item3),
                            Request_id = "",
                            Context = new
                            {
                                posID = posID,
                                codeValue = codeValue,
                                isMobile = isMobile,
                                responseError = result.Item3
                            }
                        });
                        return ResponseHelper.ResponseData(HttpStatusCode.RequestTimeout, result.Item2 ?? "RequestTimeout", null, JsonConvert.SerializeObject(result.Item3));
                    }
                    if (statusCode == 500)
                    {
                        return ResponseHelper.ResponseData(HttpStatusCode.NotFound, result.Item2 ?? "Số điện thoại chưa đăng ký hội viên", result.Item3);
                    }

                    var objErrorServer = StringHelper.StringToObject<ServerErrorResponses>(JsonConvert.SerializeObject(result.Item3));
                    if (objErrorServer != null)
                    {
                        if (HttpStatusConst.HttpSatusServerError.Contains(objErrorServer.StatusCode))
                        {
                            await OpsMonitoringHelper.OpsLogging(new Ops_Logging_Data()
                            {
                                Server_code = HostHelper.GetIpAddress(),
                                App_module = "LoyaltyService.GetInfoMemberCapillary",
                                Error_code = "HttpSatusServerError",
                                Error_type = HttpStatusCode.SwitchingProtocols.ToString(),
                                Severity = OpsStatusEnum.error.ToString(),
                                Http_status = objErrorServer.StatusCode,
                                Endpoint = "GetInfoMemberCapillary",
                                Error_message = JsonConvert.SerializeObject(result.Item3),
                                Request_id = objErrorServer.RequestId,
                                Context = new
                                {
                                    posID = posID,
                                    codeValue = codeValue,
                                    isMobile = isMobile,
                                    responseError = result.Item3
                                }
                            });
                            _kibanaService.SendMessageSMS($"**{JsonConvert.SerializeObject(result.Item3)}**", MessageTypeEnum.Warning.ToString(), $"**Capillary** lỗi HttpStatus: {objErrorServer.StatusCode}", appCode: AppCodeMessageEnum.BLUEPOS_CAP_ERROR.ToString());
                            return ResponseHelper.ResponseData(HttpStatusCode.SwitchingProtocols, $"Chuyển truy vấn hội viên CAP sang offline", result.Item3, JsonConvert.SerializeObject(result.Item3));
                        }
                    }
                    return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, $"Truy vấn thông tin hội viên WIN thất bại, vui lòng thử lại", result.Item3, result.Item2);
                }
                return httpResponseMessage;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs(string.Format(@"LoyaltyService.GetInfoMemberCapillary {0}", codeValue), ex);
                return ResponseHelper.ResponseData(HttpStatusCode.ExpectationFailed, $"{codeValue} truy vấn thông tin hội viên WIN thất bại, vui lòng thử lại", null, JsonConvert.SerializeObject(ex));
            }
        }
        public async Task<Tuple<ResultResponse, PointModePOSResponse>> AddTransactionCapillaryV2(MemoryCacheService _memoryCacheService, RedisManager _redisManager, VinIDSalesRequest request)
        {
            request.CardNumber = FormatHelper.PhoneNumberWithCountryCode(request.CardNumber);
            if (!RegexHelper.IsCharacters(request.CardNumber))
            {
                return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.BadRequest, "Số điện thoại không hợp lệ, vui lòng scan lại thông tin", null), null);
            }
            try
            {
                long balance = 0;
                string redeemId = "";
                bool isStaffBill = false;
                //check staff bill
                if (!string.IsNullOrEmpty(request.VirtualCard) 
                    && request.VirtualCard.Length == 12
                    && _redisManager.KeyExists($"{RedisConst.Redis_Key_MASANER_MemberStaff}:{request.VirtualCard}", null))
                {
                    isStaffBill = true;
                }
                //redeem points  if have spend points
                if (request.SpendPoints > 0)
                {
                    var checkRedeem = _redisManager.HashGet<PointsDataCap>(RedisConst.Redis_Key_Loyalty_RedeemPoints, request.OrderNo);
                    if (checkRedeem != null)
                    {
                        if(!string.IsNullOrEmpty(checkRedeem.Redemption_id) && NumberHelper.ConvertScientificToNumber<long>(checkRedeem.Points_redeemed) == request.SpendPoints)
                        {
                            redeemId = checkRedeem.Redemption_id;
                            balance = NumberHelper.ConvertScientificToNumber<long>(checkRedeem.Balance);
                            await LoyaltyHelper.PointRedeemLogging(_redisManager, _kibanaService, request, checkRedeem);
                        }
                        else
                        {
                            return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.Conflict, $"Đơn hàng {request.OrderNo} đã sử dụng điểm Hội viên, mã tham chiếu: {checkRedeem}", null), null);
                        }
                    }
                    else
                    {
                        var validateSpendPoints = CapillaryHelper.IsValidateSpendPoints(_centralMDRepository, _redisManager, request, isStaffBill);
                        if (!validateSpendPoints.Item1)
                        {
                            return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.BadRequest, validateSpendPoints.Item2, null), null);
                        }

                        var checkPointRedeem = _loyaltyCapillaryService.PointRedeem(_loyaltyRepository, _redisManager, _memoryCacheService, request.MerchantId, request.TerminalId, request.OrderNo, request.CardNumber, request.SpendPoints, JsonConvert.SerializeObject(request), isStaffBill);
                        if (!checkPointRedeem.Item1)
                        {
                            return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.BadRequest, checkPointRedeem.Item2, checkPointRedeem.Item3), null);
                        }
                        else
                        {
                            var checkResponseRedeem = JsonConvert.DeserializeObject<PointRedeemCapResponse>(JsonConvert.SerializeObject(checkPointRedeem.Item3));
                            redeemId = checkResponseRedeem.Response.Responses.Points.Redemption_id;
                            balance = NumberHelper.ConvertScientificToNumber<long>(checkResponseRedeem.Response.Responses.Points.Balance);
                            await LoyaltyHelper.PointRedeemLogging(_redisManager, _kibanaService, request, checkResponseRedeem.Response.Responses.Points);
                            _redisManager.HashSet<PointsDataCap>(RedisConst.Redis_Key_Loyalty_RedeemPoints, request.OrderNo, checkResponseRedeem.Response.Responses.Points);
                        }
                    }
                }

                //if staff points bill
                if (isStaffBill)
                {
                    await Task.Run(() =>
                    {
                        RabbitMQProducer.ProducerRabbtMQCluster(_kibanaService, RabitMQConst.Queue_Wincare_TopUpPoints,
                                                                                  request.VirtualCard,
                                                                                  JsonConvert.SerializeObject(request));
                    });

                    if(string.IsNullOrEmpty(redeemId))
                    {
                        balance = await _loyaltyExtentionService.GetBlancePoints(_redisManager, request.CardNumber);
                    }
                    var pointConversion = CapillaryHelper.PointConversionVND(_memoryCacheService, balance);
                    PointModePOSResponse pointEarnTopUp = new PointModePOSResponse
                    {
                        PointEarn = 0,
                        PointRedeem = request.SpendPoints,
                        CurrentRate = pointConversion.Item1,
                        RedemptionValue = pointConversion.Item2,
                        IsOfflineVinID = false,
                        EmpCode = "Capillary",
                        MasanerPackageInd = false,
                        StaffPercentage = 0,
                        NormCustPercentage = 0,
                        RedemptionId = redeemId,
                        OrderNo = request.OrderNo ?? "",
                        Balance = balance
                    };

                    await AddTransactionCapillary(_memoryCacheService, _redisManager, request, true);
                    _redisManager.HashSet<string>(RedisConst.GetRedisKeyLoyaltyMemberPoints(request.OrderNo), request.OrderNo, request.CardNumber, DateTimeHelper.GetTotalSecondMidnight(1));
                    return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.OK, "OK", pointEarnTopUp), pointEarnTopUp);
                }

                var resultCap = await AddTransactionCapillary(_memoryCacheService, _redisManager, request, isStaffBill);
                //add to queue if add transaction fail or offline vinid
                if (resultCap.Item1 != null 
                    && resultCap.Item1.Status != HttpStatusCode.OK)
                {
                    await Task.Run(() =>
                    {
                        RabbitMQProducer.ProducerRabbtMQCluster(_kibanaService, RabitMQConst.Queue_Loyalty_Member_Points,
                                                                                  request.VirtualCard,
                                                                                  JsonConvert.SerializeObject(request));
                    });
                }

                PointModePOSResponse pointEarn = new PointModePOSResponse
                {
                    PointEarn = (resultCap.Item2 != null ? (long)resultCap.Item2.PointEarn : 0),
                    PointRedeem = request.SpendPoints,
                    CurrentRate = resultCap.Item2 != null ? resultCap.Item2.CurrentRate : 0,
                    IsOfflineVinID = false,
                    EmpCode = "Capillary",
                    MasanerPackageInd = false,
                    StaffPercentage = 0,
                    NormCustPercentage = 0,
                    RedemptionId = redeemId,
                    OrderNo = request.OrderNo ?? "",
                    Balance = resultCap.Item2 != null ? resultCap.Item2.Balance : 0,
                    RedemptionValue = resultCap.Item2 != null ? resultCap.Item2.RedemptionValue : 0
                };

                //logging
                if(resultCap.Item2 != null && resultCap.Item2.PointEarn > 0)
                {
                    await LoyaltyHelper.AddTransactionLogging(_kibanaService, _redisManager, resultCap, request, _loyaltyRepository);
                }
                //result
                return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.OK, "OK", pointEarn), pointEarn);
            }
            catch (Exception ex)
            {
                return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.InternalServerError, $"Lỗi xử lý giao dịch hội viên WIN", null, JsonConvert.SerializeObject(ex)), null);
            }
        }
        public async Task<Tuple<ResultResponse, PointModePOSResponse>> AddTransactionCapillary(MemoryCacheService _memoryCacheService, RedisManager _redisManager, VinIDSalesRequest request, bool isStaffBill)
        {
            request.CardNumber = FormatHelper.PhoneNumberWithCountryCode(request.CardNumber);
            if (!RegexHelper.IsCharacters(request.CardNumber))
            {
                return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.BadRequest, "Số điện thoại không hợp lệ, vui lòng scan lại thông tin", null), null);
            }
            try
            {
                //CAP
                int statusCode = 0;
                string redeemId = "";

                var result = await _loyaltyCapillaryService.AddTransactionToCap(_loyaltyRepository, _redisManager, _memoryCacheService, CapillaryHelper.CreateTransactionCapillary(request), isStaffBill);
                if (result.Item1)
                {
                    if (result.Item2 == HttpStatusCode.ServiceUnavailable.ToString())
                    {
                        return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.ServiceUnavailable, MessageConst.ApiServiceUnavailable, null), null);
                    }

                    PointModePOSResponse pointEarn = new PointModePOSResponse();
                    var checkResponse = StringHelper.StringToObject<AddTransactionResponse>(JsonConvert.SerializeObject(result.Item3));
                    if (checkResponse != null && checkResponse.CreatedId > 0)
                    {
                        if (checkResponse.SideEffects == null || checkResponse.SideEffects.Count == 0)
                        {
                            pointEarn.PointEarn = 0;
                        }
                        else
                        {
                            pointEarn.PointEarn = (long)checkResponse.SideEffects.FirstOrDefault().AwardedPoints; //Tích điểm
                        }
                        //balance points
                        if (checkResponse.Customer != null)
                        {
                            pointEarn.Balance = checkResponse.Customer.MainPoints; //Số dư điểm sau khi tích điểm
                        }
                        else
                        {
                            pointEarn.Balance =  await _loyaltyExtentionService.GetBlancePoints(_redisManager, request.CardNumber) + pointEarn.PointEarn;
                        }

                        var pointConversion = CapillaryHelper.PointConversionVND(_memoryCacheService, pointEarn.Balance??0);
                        pointEarn.RedemptionValue = pointConversion.Item2;
                        pointEarn.PointRedeem = request.SpendPoints;
                        pointEarn.CurrentRate = pointConversion.Item1;
                        pointEarn.IsOfflineVinID = false;
                        pointEarn.EmpCode = "Capillary";
                        pointEarn.MasanerPackageInd = false;
                        pointEarn.StaffPercentage = 0;
                        pointEarn.NormCustPercentage = 0;
                        pointEarn.RedemptionId = redeemId;
                        pointEarn.OrderNo = request.OrderNo ?? "";
                        pointEarn.CreatedId = checkResponse.CreatedId.ToString();
                        
                        //calc earn points
                        if (request.TransLine != null && checkResponse.RawSideEffects != null && checkResponse.RawSideEffects.Count > 0)
                        {
                            pointEarn.TransLine = _loyaltyExtentionService.CalcEarnLineItemsV2(request, checkResponse.RawSideEffects);
                        }
                    }
                    _kibanaService.LogResponse("api/v2/loyalty/transaction/add", request.PosID, 0, "", request.OrderNo + "|" + request.CardNumber + "|" + JsonConvert.SerializeObject(pointEarn));
                    return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.OK, "OK", pointEarn), pointEarn);
                }
                else
                {
                    if (request.SpendPoints > 0 && !string.IsNullOrEmpty(redeemId))
                    {
                        var checkPointRedeem = _loyaltyCapillaryService.PointReverse(_loyaltyRepository, _redisManager, _memoryCacheService, request.MerchantId, request.TerminalId, request.CardNumber, redeemId, request.SpendPoints, request.OrderNo);
                        var checkReverseRsp = JsonConvert.DeserializeObject<RevertPointsRedeemResponse>(JsonConvert.SerializeObject(checkPointRedeem.Item3));
                        RevertPointsRedeemRequest revertData = new RevertPointsRedeemRequest()
                        {
                            OrderNo = request.OrderNo,
                            MemberCard = request.CardNumber,
                            Points = request.SpendPoints,
                            RedeemId    = redeemId,
                            StoreNo = request.MerchantId,
                            Reason = $"Revert redeem points for order {request.OrderNo} after fail to add transaction"
                        };
                        await Task.Run(() => LoyaltyHelper.RevertPointRedeemLogging(_redisManager, _kibanaService, revertData, checkReverseRsp));
                    }

                    if (result.Item2.Contains("408_requestTimedOut") || result.Item2.Contains("A task was canceled"))
                    {
                        string messageError = $"AddTransaction:{request.OrderNo}_{request.CardNumber} Request Capillary timeout";
                        _kibanaService.LogResponse("TimeoutCapillary", request.PosID, 0, "", messageError);
                        _kibanaService.SendMessageSMS(messageError, "Warning", "AddTransaction Capillary timeout");
                    }
                    if (statusCode == 500)
                    {
                        return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.NotFound, $"Khách hàng không tồn tại", result.Item3), null);
                    }
                    return new Tuple<ResultResponse, PointModePOSResponse>(CheckStatusLoyaltyOffline(_memoryCacheService, AppCodeEnum.CAP.ToString(), result.Item3, result.Item2, request.OrderNo), null);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<ResultResponse, PointModePOSResponse>(ResponseHelper.ResponseData(HttpStatusCode.InternalServerError, $"Lỗi kết nối WebApi Capillary", null, JsonConvert.SerializeObject(ex)), null);
            }
        }
        public async Task<ResultResponse> RefundPointCapillary(MemoryCacheService _memoryCacheService, RedisManager _redisManager, VinIDRefundRequest request)
        {
            StringHelper.Loggingz($"RefundPointCapillary: {JsonConvert.SerializeObject(request)}");
            request.CardNumber = FormatHelper.PhoneNumberWithCountryCode(request.CardNumber);
            long balance = 0;
            if (!RegexHelper.IsCharacters(request.CardNumber))
            {
                return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, "Số điện thoại không hợp lệ, vui lòng scan lại thông tin", null);
            }
            try
            {
                //check Masaner
                if (LoyaltyExtentionService.IsRevertTopUpPoints(_memoryCacheService) && _redisManager.KeyExists(RedisConst.Redis_Key_MASANER_MemberStaff, FormatHelper.PhoneNumberVietNam(request.CardNumber)))
                {
                    await Task.Run(() =>
                    {
                        RabbitMQProducer.ProducerRabbtMQCluster(_kibanaService, RabitMQConst.Queue_Wincare_Revert_TopUpPoints,
                                                                                  request.CardNumber,
                                                                                  JsonConvert.SerializeObject(request));
                    });
                }
                string reversalId = "";
                if (request.SpendPoints > 0)
                {
                    if (string.IsNullOrEmpty(request.RedemptionId))
                    {
                        return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, string.Format("Đơn hàng {0} thiếu RedemptionId - vui lòng liên hệ IT", request.OrigOrderNo), null);
                    }
                    var checkPointRedeem = _loyaltyCapillaryService.PointReverse(_loyaltyRepository, _redisManager, _memoryCacheService, request.MerchantId, request.TerminalId, request.CardNumber, request.RedemptionId ?? "", request.SpendPoints, request.OrigOrderNo);
                    if (!checkPointRedeem.Item1)
                    {
                        return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, checkPointRedeem.Item2, checkPointRedeem.Item3);
                    }
                    else
                    {
                        var checkReverseRsp = JsonConvert.DeserializeObject<PointReverseCapResponse>(JsonConvert.SerializeObject(checkPointRedeem.Item3));
                        reversalId = checkReverseRsp.ReversalId;
                        balance = NumberHelper.ConvertScientificToNumber<long>(checkReverseRsp.Balance);
                        await LoyaltyHelper.PointReverseLogging(_redisManager, _kibanaService, request, checkReverseRsp);
                    }
                }

                var transactionCapPOSRequest = CapillaryHelper.RefundTransactionCapillary(request);
                var result = await _loyaltyCapillaryService.ReturnTransactionToCap(_loyaltyRepository, _redisManager, _memoryCacheService, transactionCapPOSRequest, JsonConvert.SerializeObject(request), request.OrderNo);
                RefundTransationResponse checkResponse = null;
                PointModePOSResponse pointRsp = new PointModePOSResponse();
                if (result.Item1)
                {
                    if (result.Item2 == HttpStatusCode.ServiceUnavailable.ToString())
                    {
                        return ResponseHelper.ResponseData(HttpStatusCode.ServiceUnavailable, MessageConst.ApiServiceUnavailable, null);
                    }
                    checkResponse = StringHelper.StringToObject<RefundTransationResponse>(JsonConvert.SerializeObject(result.Item3));
                    if (checkResponse != null && checkResponse.CreatedId > 0)
                    {
                        long awardedPoints = 0;
                        if (checkResponse.SideEffects == null || checkResponse.SideEffects.Count == 0)
                        {
                            pointRsp.PointEarn = 0;
                        }
                        else
                        {
                            var sideEffects = JsonConvert.DeserializeObject<List<SideEffect>>(JsonConvert.SerializeObject(checkResponse.SideEffects));
                            pointRsp.PointEarn = (int)sideEffects.FirstOrDefault().AwardedPoints * (-1); //Tích điểm
                            var extraCampaign = new List<PointModePOSData>();
                            foreach (var item in sideEffects)
                            {
                                if (item.Type == "points")
                                {
                                    extraCampaign.Add(new PointModePOSData()
                                    {
                                        LoyaltyMerchantId = request.MerchantId,
                                        EarnedPoints = item.AwardedPoints < 0 ? item.AwardedPoints * (-1) : item.AwardedPoints,
                                        Amount = item.RawAwardedPoints < 0 ? item.RawAwardedPoints * (-1) : item.RawAwardedPoints,
                                        EntityType = item.EntityType,
                                        Type = item.Type
                                    });
                                    awardedPoints += item.AwardedPoints;
                                }
                            }
                            pointRsp.ExtraEarnByCampaign = extraCampaign;
                        }
                        pointRsp.PointRedeem = request.SpendPoints;
                        pointRsp.CurrentRate = 0;
                        pointRsp.IsOfflineVinID = false;
                        pointRsp.EmpCode = "Capillary";
                        pointRsp.MasanerPackageInd = false;
                        pointRsp.StaffPercentage = 0;
                        pointRsp.NormCustPercentage = 0;
                        pointRsp.RedemptionId = request.RedemptionId ?? "";
                        pointRsp.OrderNo = request.OrderNo ?? "";
                        pointRsp.ReversalId = reversalId ?? "";

                        WinXService enostaService = new WinXService();
                        var checkReturnEnosta = await enostaService.UpdateTransactionReturn(_memoryCacheService, transactionCapPOSRequest, request, checkResponse.CreatedId.ToString());
                        if (checkReturnEnosta.Item1 == HttpStatusCode.OK && checkReturnEnosta.Item3 != null)
                        {
                            pointRsp.TransLine = checkReturnEnosta.Item3;
                            if(awardedPoints == 0 && pointRsp.TransLine.Count > 0)
                            {
                                awardedPoints = pointRsp.TransLine.Where(x => x.EarnPoints > 0).Sum(x => x.EarnPoints) * (-1);
                            }
                        }
                        await LoyaltyHelper.RefundTransactionLogging(_redisManager, _kibanaService, request, checkResponse, pointRsp.TransLine, awardedPoints);
                    }
                  
                    long earnPoints = 0;
                    if (pointRsp.TransLine != null && pointRsp.TransLine.Count > 0)
                    {
                        earnPoints = pointRsp.TransLine.Where(x => x.EarnPoints > 0).Sum(x => x.EarnPoints);
                    }
                    if(balance > 0)
                    {
                        balance += earnPoints;
                    }
                    else
                    {
                        balance = (await _loyaltyExtentionService.GetBlancePoints(_redisManager, request.CardNumber)) + earnPoints;
                    }

                    pointRsp.Balance = balance;
                    pointRsp.TransLine = null; //clear line item return

                    var pointConversion = CapillaryHelper.PointConversionVND(_memoryCacheService, pointRsp.Balance ?? 0);
                    pointRsp.RedemptionValue = pointConversion.Item2;
                    pointRsp.CurrentRate = pointConversion.Item1;

                    return ResponseHelper.ResponseData(HttpStatusCode.OK, "Success", pointRsp);
                }
                else
                {
                    if (result.Item2.Contains("408_requestTimedOut") || result.Item2.Contains("A task was canceled"))
                    {
                        string messE = $"RefundPointCapillary:{request.OrderNo}_{request.CardNumber} Request Capillary timeout";
                        _kibanaService.LogResponse("TimeoutCapillary", request.TerminalId, 0, "", messE);
                        _kibanaService.SendMessageSMS(messE, "Warning", $"{request.OrderNo}_{request.CardNumber} refundPointCapillary timeout");
                    }
                    return CheckStatusLoyaltyOffline(_memoryCacheService, AppCodeEnum.CAP.ToString(), result.Item3, result.Item2, request.OrderNo);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("RefundPointCapillary Exception", ex);
                return ResponseHelper.ResponseData(HttpStatusCode.InternalServerError, $"Lỗi kết nối WebApi Capillary", null, JsonConvert.SerializeObject(ex));
            }
        }
        public async Task<ResultResponse> ToUpPointCapillary(MemoryCacheService _memoryCacheService, RedisManager redisManager, LoyaltyRepository loyaltyRepository, PointTopupPOSRequest pointTopupPOSRequest)
        {
            ResultResponse httpResponseMessage = new ResultResponse();
            if (!LoyaltyExtentionService.CheckStoreApplyStaffPoints(_memoryCacheService, pointTopupPOSRequest.StoreNo))
            {
                httpResponseMessage = ResponseHelper.ResponseData(HttpStatusCode.BadRequest, $"Store {pointTopupPOSRequest.StoreNo} không có trong danh sách chạy chương trình StaffPoints", null);
            }
            else
            {
                var checkTopUp = await _loyaltyCapillaryService.PointToUp(_memoryCacheService, redisManager, loyaltyRepository, pointTopupPOSRequest);
                if (checkTopUp.Item1)
                {
                    httpResponseMessage = ResponseHelper.ResponseData(HttpStatusCode.OK, checkTopUp.Item2 ?? "OK", checkTopUp.Item3);
                }
            }
            return httpResponseMessage;
        }
        public async Task<ResultResponse> RevertPointsRedeem(RedisManager redisManager, MemoryCacheService _memoryCacheService, RevertPointsRedeemRequest request)
        {
            await Task.Delay(1);
            var result = _loyaltyCapillaryService.PointReverse(_loyaltyRepository, redisManager, _memoryCacheService, request.StoreNo, request.StoreNo, request.MemberCard, request.RedeemId, request.Points, request.OrderNo);
            if (result.Item1)
            {
                var checkReverseRsp = JsonConvert.DeserializeObject<RevertPointsRedeemResponse>(JsonConvert.SerializeObject(result.Item3));
                await LoyaltyHelper.RevertPointRedeemLogging(redisManager, _kibanaService, request, checkReverseRsp);
                return ResponseHelper.ResponseData(HttpStatusCode.OK, result.Item2 ?? "OK", result.Item3);
            }
            else
            {
                return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, result.Item2, result.Item3);
            }
        }
        public async Task<ResultResponse> RevertTopUpPoints(MemoryCacheService _memoryCacheService, RevertTopUpPointsPOSRequest request)
        {
            try
            {
                var requestCAP = new RevertTopUpPointsCapRequest()
                {
                    PointsToBeAdjusted = request.PointsToBeAdjusted.ToString(),
                    PointCategoryTypes = "REGULAR",
                    ProgramId = 0,
                    ReasonOfReturn = $"Revert TopUp Points OrderNo:{request.OrderNo}"
                };
                var result = await _loyaltyCapillaryService.RevertTopUpPoints(_memoryCacheService, requestCAP, request.MemberCard, request.StoreNo);
                if (result.Item1)
                {
                    return ResponseHelper.ResponseData(HttpStatusCode.OK, result.Item2 ?? "OK", result.Item3);
                }
                else
                {
                    return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, result.Item2, result.Item3);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("RevertTopUpPoints.Exception", ex);
                return ResponseHelper.ResponseData(HttpStatusCode.InternalServerError, $"Lỗi kết nối WebApi Capillary", null, JsonConvert.SerializeObject(ex));
            }
        }
        public async Task<ResultResponse> GetPointHistory(MemoryCacheService _memoryCacheService, string storeNo, string phoneNumber)
        {
            try
            {
                var result = await _loyaltyCapillaryService.GetPointsLedger(_memoryCacheService, storeNo, storeNo + "00", phoneNumber);
                if (result.Item1)
                {
                    return ResponseHelper.ResponseData(HttpStatusCode.OK, result.Item2 ?? "OK", result.Item3);
                }
                else
                {
                    return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, result.Item2, result.Item3);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("RevertTopUpPoints.Exception", ex);
                return ResponseHelper.ResponseData(HttpStatusCode.InternalServerError, $"Lỗi kết nối WebApi Capillary", null, JsonConvert.SerializeObject(ex));
            }
        }
        public ResultResponse TransactionMemberBusiness(MemberBusinessRequest request)
        {
            if (!RegexHelper.IsCharacters(request.CardNumber))
            {
                return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, "Số điện thoại không hợp lệ, vui lòng scan lại thông tin", null);
            }
            try
            {
                //check memberBusiness
                if (request.Status.ToUpper() == "CREATE")
                {
                    //var validateKey = _memberBusinessService.ValidateMemberBusinessDataPayment(request.StoreNo, request.PosNo, request.CardNumber, request.Key);
                    //if(validateKey != null && validateKey.IsChecked == false)
                    //{
                    //    return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, validateKey.Description, validateKey);
                    //}

                    var checkMemberBusiness = _memberBusinessService.MemberBusinessTransaction(request);
                    if (!checkMemberBusiness.Item1)
                    {
                        return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, checkMemberBusiness.Item2, null);
                    }

                    return ResponseHelper.ResponseData(HttpStatusCode.OK, checkMemberBusiness.Item2, null);
                }
                else
                {
                    var checkRefund = _memberBusinessService.DeleteMemberRemnItem(request.OrderNo, request.CardNumber);
                    if (checkRefund.Item1)
                    {
                        return ResponseHelper.ResponseData(HttpStatusCode.OK, request.Status.ToUpper() + "_OK", null);
                    }
                    return ResponseHelper.ResponseData(HttpStatusCode.BadGateway, checkRefund.Item2, null);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.ResponseData(HttpStatusCode.InternalServerError, $"Lỗi Exception " + ExceptionHelper.GetExceptionMessage(ex), null);
            }
        }
        public async Task<ResultResponse> AddTransactionProgramPointsAsync(MemoryCacheService _memoryCacheService, ProgramPointsTransactionDto request)
        {
            if (!RegexHelper.IsCharacters(request.MemberCard))
            {
                return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, "Số điện thoại không hợp lệ, vui lòng scan lại thông tin", null);
            }
            try
            {
                request.MemberCard = FormatHelper.PhoneNumberVietNam(request.MemberCard);
                var result = await _programPointsService.Value.AddTransactionProgramPoints(_memoryCacheService, request);
                if (result.Item1)
                {
                    return ResponseHelper.SusscessResponse(HttpStatusCode.OK, result.Item3, result.Item2);
                }
                else
                {
                    return ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, result.Item2, result.Item3);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.ResponseData(HttpStatusCode.InternalServerError, $"Lỗi Exception " + ExceptionHelper.GetExceptionMessage(ex), null);
            }
        }
        public ResultResponse RedeemCouponCapillary(RedisManager redisManager, MemoryCacheService _memoryCacheService, UpdateStatusVoucherPartnerRequest request, bool isValidate = false)
        {
            if (!string.IsNullOrEmpty(request.PhoneNumber) && !RegexHelper.IsCharacters(request.PhoneNumber))
            {
                return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, "Số điện thoại không hợp lệ, vui lòng scan lại thông tin", null);
            }
            try
            {
                request.PhoneNumber = FormatHelper.PhoneNumberWithCountryCode(request.PhoneNumber);
                var resultResponse = _loyaltyCapillaryService.CouponRedeemCapillary(
                                            _loyaltyRepository,
                                            redisManager,
                                            _memoryCacheService,
                                            request.StoreNo,
                                            request.PosNo,
                                            request.OrderNo,
                                            request.PhoneNumber,
                                            request.SerialNo);
                if (resultResponse.Item1)
                {
                    List<DataVoucherPartnerResponse> codeDone = new List<DataVoucherPartnerResponse>();
                    List<DataVoucherPartnerResponse> codeError = new List<DataVoucherPartnerResponse>();
                    var result = StringHelper.StringToObject<RedeemCouponResponseCap>(JsonConvert.SerializeObject(resultResponse.Item3));
                    if (result.Redemption != null && result.Redemption.Count > 0)
                    {
                        foreach (var item in result.Redemption)
                        {
                            if (item.RedemptionStatus.Code == 700 && item.RedemptionStatus.Success)
                            {
                                codeDone.Add(new DataVoucherPartnerResponse()
                                {
                                    Amount = (int)item.CouponValue,
                                    Code = item.Code,
                                    Msg = item.RedemptionStatus.Message ?? "",
                                    Status = item.RedemptionStatus.Code.ToString()
                                });
                            }
                            else
                            {
                                var messError = _loyaltyCapillaryService.GetMessageCapillary(_memoryCacheService.GetNotifyConfig(_memoryCacheService.GetConnectStringCentralMD()), "StatusCode", $"{item.RedemptionStatus.Code}_{item.RedemptionStatus.Message}");
                                codeError.Add(new DataVoucherPartnerResponse()
                                {
                                    Amount = (int)item.CouponValue,
                                    Code = item.Code,
                                    Msg = item.RedemptionStatus.Message ?? "",
                                    Status = item.RedemptionStatus.Code.ToString()
                                });
                                return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, codeError, $"Voucher {item.Code} {messError}");
                            }
                        }
                        return ResponseHelper.SusscessResponse(HttpStatusCode.OK, codeDone, result.Redemption.FirstOrDefault().RedemptionStatus.Message ?? "OK");
                    }
                    return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, codeDone,
                        _loyaltyCapillaryService.GetMessageCapillary(_memoryCacheService.GetNotifyConfig(_memoryCacheService.GetConnectStringCentralMD()), "StatusCode", resultResponse.Item2, ""),
                        resultResponse.Item2);
                }
                else
                {
                    return ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, _loyaltyCapillaryService.GetMessageCapillary(_memoryCacheService.GetNotifyConfig(_memoryCacheService.GetConnectStringCentralMD()), "StatusCode", resultResponse.Item2, ""), resultResponse.Item3, resultResponse.Item2);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.ResponseData(HttpStatusCode.InternalServerError, $"RedeemCouponCapillary.Exception " + ExceptionHelper.GetExceptionMessage(ex), null);
            }
        }
        public ResultResponse ValidateVouchersCapillary(MemoryCacheService _memoryCacheService, CheckVoucherPartnerPOSRequest modelPOS)
        {
            if (!string.IsNullOrEmpty(modelPOS.PhoneNumber) && !RegexHelper.IsCharacters(modelPOS.PhoneNumber))
            {
                return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, "Số điện thoại không hợp lệ, vui lòng scan lại thông tin", null);
            }
            try
            {
                modelPOS.PhoneNumber = FormatHelper.PhoneNumberVietNam(modelPOS.PhoneNumber);
                List<VouchersCapillary> vouchers = new List<VouchersCapillary>();
                foreach (var voucher in modelPOS.SerialNo)
                {
                    vouchers.Add(new VouchersCapillary()
                    {
                        Code = voucher
                    });
                }

                List<LineItemVouchersCapillary> items = new List<LineItemVouchersCapillary>();
                if (modelPOS.Items != null)
                {
                    foreach (var item in modelPOS.Items)
                    {
                        items.Add(new LineItemVouchersCapillary()
                        {
                            Amount = item.LineAmount,
                            ItemCode = item.ItemNo,
                            Pos_line_no = item.LineNo
                        });
                    }
                }

                var request = new ValidateVoucherCapillary()
                {
                    BillAmount = modelPOS.Items.Sum(x => x.LineAmount),
                    Vouchers = vouchers,
                    LineItems = items
                };

                var result = _loyaltyCapillaryService.ValidateVouchersCapillary(
                                            _memoryCacheService.GetSysWebApi(_memoryCacheService.GetStringDbCentralMD()),
                                            request,
                                            modelPOS.StoreNo,
                                            modelPOS.PosNo,
                                            modelPOS.PhoneNumber);
                if (result.Item1 && result.Item3.Warnings != null && result.Item1 && result.Item3.Warnings.FirstOrDefault() != null)
                {
                    var warning = StringHelper.StringToObject<WarningsCAP>(JsonConvert.SerializeObject(result.Item3.Warnings.FirstOrDefault()));
                    if (warning != null && !warning.Status && warning.Code != "700")
                    {
                        var messError = _loyaltyCapillaryService.GetMessageCapillary(_memoryCacheService.GetNotifyConfig(_memoryCacheService.GetConnectStringCentralMD()), "StatusCode", $"{warning.Code}_{warning.Message}");
                        return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null, messError, JsonConvert.SerializeObject(warning));
                    }
                }

                if (result.Item1 && result.Item3.Vouchers != null && result.Item3.Vouchers.Count > 0)
                {
                    List<DataVoucherPartnerResponse> dataVoucherPartnerResponses = new List<DataVoucherPartnerResponse>();
                    var checkFirstVoucher = result.Item3.Vouchers.FirstOrDefault(x => x.IsRedeemable == false);
                    if (checkFirstVoucher != null)
                    {
                        string messError = _loyaltyCapillaryService.GetMessageCapillary(
                            _memoryCacheService.GetNotifyConfig(_memoryCacheService.GetConnectStringCentralMD()), "StatusCode", checkFirstVoucher.StatusCode.ToString(), checkFirstVoucher.Code);
                        string statusError = "000";
                        if (checkFirstVoucher.Warnings != null && checkFirstVoucher.Warnings.FirstOrDefault() != null)
                        {
                            var wa = StringHelper.StringToObject<WarningsCAP>(JsonConvert.SerializeObject(checkFirstVoucher.Warnings.FirstOrDefault()));
                            if (string.IsNullOrEmpty(messError))
                            {
                                messError = $"Capillary code: {wa.Code} {wa.Message}";
                            }
                            statusError = wa.Code.ToString();
                        }

                        int discountAmount = (int)checkFirstVoucher.DiscountValue;
                        if (result.Item3.Recommends != null)
                        {

                        }
                        dataVoucherPartnerResponses.Add(new DataVoucherPartnerResponse()
                        {
                            Code = checkFirstVoucher.Code,
                            VoucherAmount = (int)checkFirstVoucher.DiscountValue,
                            Amount = discountAmount,
                            Msg = $"{messError} {checkFirstVoucher.Description}",
                            Status = statusError,
                            IsApplySku = checkFirstVoucher.LineItems != null,
                            Remark = null
                        });
                        return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, dataVoucherPartnerResponses, messError, JsonConvert.SerializeObject(checkFirstVoucher));
                    }

                    foreach (var item in result.Item3.Vouchers)
                    {
                        if (!item.IsRedeemable)
                        {
                            return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest,
                                dataVoucherPartnerResponses,
                                _loyaltyCapillaryService.GetMessageCapillary(
                            _memoryCacheService.GetNotifyConfig(_memoryCacheService.GetConnectStringCentralMD()), "StatusCode", item.StatusCode.ToString(), item.Code) ?? $"Voucher {item.Code} không hợp lệ",
                                JsonConvert.SerializeObject(item));
                        }
                        dataVoucherPartnerResponses.Add(new DataVoucherPartnerResponse()
                        {
                            Code = item.Code,
                            Amount = (int)item.DiscountValue,
                            Msg = item.StatusMessage,
                            Status = item.StatusCode.ToString(),
                            IsApplySku = item.LineItems != null,
                            Remark = null
                        });
                    }
                    return ResponseHelper.SusscessResponse(HttpStatusCode.OK, dataVoucherPartnerResponses, result.Item2);
                }
                else
                {
                    var errorVoucher = StringHelper.StringToObject<VouchersCapillaryResponseError>(result.Item2);
                    if (errorVoucher != null && errorVoucher.Errors != null)
                    {
                        return ResponseHelper.SusscessResponse(HttpStatusCode.BadRequest, null,
                        _loyaltyCapillaryService.GetMessageCapillary(_memoryCacheService.GetNotifyConfig(_memoryCacheService.GetConnectStringCentralMD()), "StatusCode", result.Item2, errorVoucher.Errors.FirstOrDefault().Code),
                        result.Item2);
                    }
                    return ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest,
                        _loyaltyCapillaryService.GetMessageCapillary(_memoryCacheService.GetNotifyConfig(_memoryCacheService.GetConnectStringCentralMD()), "StatusCode", result.Item2, ""),
                        result.Item3, JsonConvert.SerializeObject(result));
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.ResponseData(HttpStatusCode.Forbidden, $"ValidateVouchersCapillary.Exception " + ExceptionHelper.GetExceptionMessage(ex), JsonConvert.SerializeObject(ex));
            }
        }
        public ResultResponse GetSerialInfo(MemoryCacheService _memoryCacheService, string storeNo, string posNo, string serial_id)
        {
            try
            {
                var result = _loyaltyCapillaryService.GetSerialInfo(_memoryCacheService, storeNo, posNo, serial_id);
                if (result.Item1)
                {
                    return ResponseHelper.SusscessResponse(HttpStatusCode.OK, result.Item3, result.Item2);
                }
                else
                {
                    return ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, result.Item2, result.Item3);
                }
            }
            catch (Exception ex)
            {
                return ResponseHelper.ResponseData(HttpStatusCode.Forbidden, $"GetSerialInfo.Exception " + ExceptionHelper.GetExceptionMessage(ex), null);
            }
        }
        public ResultResponse UpdateMobileEnroll(MemoryCacheService _memoryCacheService, string storeNo, string posNo, string phoneNumber, string mobile_enroll)
        {
            try
            {
                _ = Task.Run(() =>
                        _loyaltyCapillaryService.UpdateMobileEnroll(_memoryCacheService, storeNo, posNo, phoneNumber, mobile_enroll)
                    ).ConfigureAwait(false);
                //_loyaltyCapillaryService.UpdateMobileEnroll(_memoryCacheService, storeNo, posNo, phoneNumber, mobile_enroll);
                return ResponseHelper.SusscessResponse(HttpStatusCode.OK, null, "mobile_enroll");
            }
            catch (Exception ex)
            {
                return ResponseHelper.ResponseData(HttpStatusCode.Forbidden, $"Lỗi UpdateMobileEnroll.Exception " + ExceptionHelper.GetExceptionMessage(ex), null);
            }
        }
        public VinIDSalesRequest MappingAddTransactionCAP(List<TransactionOfflineDto> transactionOfflineDtos)
        {
            return _loyaltyCapillaryService.MappingAddTransactionCAP(transactionOfflineDtos);
        }
        public VinIDRefundRequest MappingReturnTransactionCAP(List<TransactionOfflineDto> transactionOfflineDtos)
        {
            return _loyaltyCapillaryService.MappingReturnTransactionCAP(transactionOfflineDtos);
        }
        private ResultResponse CheckStatusLoyaltyOffline(MemoryCacheService memoryCacheService, string appCode, object response, string message, string orderNo = "")
        {
            try
            {
                var configStatus = memoryCacheService.GetNotifyConfig(AppGlobals.ConnStrCentralMD).Where(x => x.AppCode == appCode && x.IsOffline == true).ToList();
                if (configStatus != null && configStatus.Count > 0)
                {
                    var checkError = StringHelper.StringToObject<CheckErrorCapillaryResponse>(JsonConvert.SerializeObject(response));
                    //FileHelper.WriteLogs(JsonConvert.SerializeObject(checkError));
                    if (checkError != null)
                    {
                        var dataErrors = StringHelper.StringToObject<IList<ErrorData>>(JsonConvert.SerializeObject(checkError.Errors));
                        if (dataErrors != null && dataErrors.Count > 0)
                        {
                            foreach (var error in dataErrors)
                            {
                                if (configStatus.Select(x => x.Status).ToArray().Contains(error.Code.ToString()))
                                {
                                    return ResponseHelper.ResponseData(HttpStatusCode.OK, error.Message ?? "Success",
                                                                        new PointModePOSResponse()
                                                                        {
                                                                            PointEarn = 0,
                                                                            PointRedeem = 0,
                                                                            RedemptionValue = 0,
                                                                            Balance = 0,
                                                                            IsOfflineVinID = true,
                                                                            OrderNo = orderNo ?? "",
                                                                            StatusCode = error.Code
                                                                        });
                                }
                            }
                        }
                        else
                        {
                            var warningCAP = StringHelper.StringToObject<List<WarningsCAP>>(JsonConvert.SerializeObject(checkError.Warnings));
                            if (warningCAP != null)
                            {
                                return ResponseHelper.ResponseData(HttpStatusCode.OK, $"{warningCAP.FirstOrDefault().Code}_{warningCAP.FirstOrDefault().Message}",
                                                                        new PointModePOSResponse()
                                                                        {
                                                                            PointEarn = 0,
                                                                            PointRedeem = 0,
                                                                            RedemptionValue = 0,
                                                                            Balance = 0,
                                                                            IsOfflineVinID = true,
                                                                            OrderNo = orderNo ?? ""
                                                                        },
                                                                        JsonConvert.SerializeObject(warningCAP)
                                                                        );
                            }
                        }

                        var checkWarrings = StringHelper.StringToObject<List<WarningsCAP>>(JsonConvert.SerializeObject(checkError.Warnings));
                        if (checkWarrings != null && checkWarrings.Count > 0)
                        {
                            foreach (var w in checkWarrings)
                            {
                                if (configStatus.Select(x => x.Status).ToArray().Contains(w.Code.ToString()))
                                {
                                    return ResponseHelper.ResponseData(HttpStatusCode.OK, w.Message ?? "Success",
                                                                        new PointModePOSResponse()
                                                                        {
                                                                            PointEarn = 0,
                                                                            PointRedeem = 0,
                                                                            RedemptionValue = 0,
                                                                            Balance = 0,
                                                                            IsOfflineVinID = true,
                                                                            OrderNo = orderNo ?? ""
                                                                        },
                                                                        JsonConvert.SerializeObject(checkWarrings));
                                }
                            }
                        }
                        if (checkWarrings != null && checkWarrings.Count > 0)
                        {
                            foreach (var w in checkWarrings)
                            {
                                if (configStatus.Select(x => x.Status).ToArray().Contains(w.Code.ToString()))
                                {
                                    return ResponseHelper.ResponseData(HttpStatusCode.OK, w.Message ?? "Success",
                                                                        new PointModePOSResponse()
                                                                        {
                                                                            PointEarn = 0,
                                                                            PointRedeem = 0,
                                                                            RedemptionValue = 0,
                                                                            Balance = 0,
                                                                            IsOfflineVinID = true,
                                                                            OrderNo = orderNo ?? ""
                                                                        }, JsonConvert.SerializeObject(checkWarrings));
                                }
                            }
                        }
                    }

                    var checkServerError = StringHelper.StringToObject<ServerErrorResponses>(JsonConvert.SerializeObject(response));
                    if (checkServerError != null && checkServerError.StatusCode == (int)HttpStatusCode.GatewayTimeout)
                    {
                        return ResponseHelper.ResponseData(HttpStatusCode.OK, HttpStatusCode.GatewayTimeout.ToString(),
                                                            new PointModePOSResponse()
                                                            {
                                                                PointEarn = 0,
                                                                PointRedeem = 0,
                                                                RedemptionValue = 0,
                                                                Balance = 0,
                                                                IsOfflineVinID = true,
                                                                OrderNo = orderNo ?? ""
                                                            }, JsonConvert.SerializeObject(checkServerError));
                    }
                }
                return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, message, response);
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs($"CheckStatusLoyaltyOffline.Exception {response}", ex);
                return ResponseHelper.ResponseData(HttpStatusCode.ExpectationFailed, message, response, JsonConvert.SerializeObject(ex));
            }
        }
        public ResultResponse OtherStatusUpdate(MemoryCacheService _memoryCacheService, OtherStatusUpdate otherStatusUpdate)
        {
            try
            {
                switch (otherStatusUpdate.OtherStatus)
                {
                    case "WINMONEY_TO_WINPAY":
                        var result = _loyaltyExtentionService.WinMoneyConversionWinPay(_memoryCacheService, _loyaltyRepository, otherStatusUpdate.StoreNo, otherStatusUpdate.PosNo, otherStatusUpdate.PhoneNumber, otherStatusUpdate.CashierID, true);
                        if (result != null)
                        {
                            return ResponseHelper.ResponseData(HttpStatusCode.OK, "OK", result);
                        }
                        else
                        {
                            return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, string.Format("{0} không có trong danh sách chuyển đổi", otherStatusUpdate.PhoneNumber), result);
                        }
                }
                return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, otherStatusUpdate.OtherStatus + " không đúng", null);
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("Exception CheckStatusLoyaltyOffline", ex);
                return ResponseHelper.ResponseData(HttpStatusCode.BadRequest, ex.Message, null);
            }
        }
        public ResultResponse UpdateWinscore(string posNo, string phoneNumber, int status)
        {
            try
            {
                var result = _winsoreService.Value.UpdateStatusWinscoreAsync(posNo, phoneNumber, status);
                if (result.Item1 == HttpStatusCode.OK)
                {
                    return ResponseHelper.ResponseData(HttpStatusCode.OK, "Sucess", null);
                }
                return ResponseHelper.ResponseData(result.Item1, result.Item2, null);
            }
            catch (Exception ex)
            {
                return ResponseHelper.ResponseData(HttpStatusCode.ExpectationFailed, ex.Message, null);
            }
        }
        public Tuple<bool, string> GetTransactionDetailCapillary(MemoryCacheService _memoryCacheService, string storeNo, string posNo, string orderNo, string type = "REGULAR")
        {
            try
            {
                return _loyaltyCapillaryService.GetTransactionDetail(_memoryCacheService, storeNo, posNo, orderNo, type);
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("GetTransactionDetailCapillary", posNo, 0, "", orderNo + " @response: " + JsonConvert.SerializeObject(ex));
                return null;
            }
        }
        public async Task<List<LoyaltyRetryDto>> LoyaltyRetry(MemoryCacheService _memoryCacheService, RedisManager _redisHelper)
        {
            return await _loyaltyCapillaryService.LoyaltyRetry(_loyaltyRepository, _redisHelper, _memoryCacheService);
        }
        public object GetTransIdDetailCapillary(MemoryCacheService _memoryCacheService, string storeNo, string posNo, long transId)
        {
            try
            {
                var result = _loyaltyCapillaryService.GetTransactionIdDetail(_memoryCacheService, storeNo, posNo, transId);
                if (result.Item1 && result.Item3 != null)
                {
                    return result.Item3;
                }
                return null;
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("GetTransactionDetailCapillary", posNo, 0, "", transId.ToString() + " @response: " + JsonConvert.SerializeObject(ex));
                return null;
            }
        }
        public List<BLUEAvailablePromotion> GetWinCodePromotion(string storeNo, string posNo, string phoneNumber, CouponsCapillary coupons)
        {
            List<BLUEAvailablePromotion> promotions = new List<BLUEAvailablePromotion>();
            var checkWincode = _wincodeService.GetWinCodeByStore("VCM", storeNo, phoneNumber);
            if (checkWincode.Item1 && checkWincode.Item3 != null && checkWincode.Item3.Count > 0)
            {
                foreach (var w in checkWincode.Item3)
                {
                    promotions.Add(new BLUEAvailablePromotion()
                    {
                        MerchantId = w.MerchantId,
                        WinCode = w.WinCode,
                        PromotionCode = w.ProgramCode,
                        Quantity = w.Quantity,
                        DiscountType = w.DiscountType,
                        InitQuota = 0,
                        RemainQuota = 0
                    });
                }
            }

            //check coupon CAP
            //if (coupons != null && coupons.Coupon != null)
            //{
            //    foreach (var coupon in coupons.Coupon)
            //    {
            //        if (coupon.Redeemed.ToUpper() == "false".ToUpper())
            //        {
            //            promotions.Add(new BLUEAvailablePromotion()
            //            {
            //                Serial = coupon.Series_id,
            //                PromotionCode = coupon.Code,
            //                MerchantId = PartnerEnum.CAP.ToString(),
            //                WinCode = coupon.Series_id,
            //                Quantity = 1,
            //                DiscountType = $"{DiscountTypeEnum.COUPON}",
            //                InitQuota = 0,
            //                RemainQuota = 0
            //            });
            //        }
            //    }
            //}

            //check offer CBNV
            //var checkOfferStaff = _offerEmployeeService.GetOfferStaffRemnDB(_redisClusterManager, posNo, phoneNumber);
            //if(checkOfferStaff.Item2 != null)
            //{
            //    promotions.Add(new BLUEAvailablePromotion()
            //    {
            //        MerchantId = PartnerEnum.POS.ToString(),
            //        WinCode = checkOfferStaff.Item2.ClubCode,
            //        Quantity = 1,
            //        DiscountType = $"{DiscountTypeEnum.MASANER}_{checkOfferStaff.Item2.StaffCode}",
            //        InitQuota = checkOfferStaff.Item2.InitQuota,
            //        RemainQuota = checkOfferStaff.Item2.RemainQuota
            //    });
            //}
            return promotions;
        }
        private (string, List<ExtendedField>) GetOtherStatusLoyalty(List<MemoryCacheConfig> getMemmoryCacheConfigLoyalty, MemoryCacheService _memoryCacheService, List<SysWebApiDto> getSysWebApiConfig, string storeNo, string posID, string codeValue, string custom_fields = "")
        {
            try
            {
                List<ExtendedField> extendedFields = new List<ExtendedField>();
                string otherStatus = null;
                if (getMemmoryCacheConfigLoyalty != null)
                {
                    //List ExtendedField
                    var lstCacheConfig = getMemmoryCacheConfigLoyalty.Where(x => x.Code == OrtherStatusLoyal.WinMoneyConversion.ToString() && x.Blocked == false).ToList();
                    if (lstCacheConfig.Count > 0)
                    {
                        foreach (var item in lstCacheConfig)
                        {
                            var checkStore = StringHelper.ConverStringToArray(item.ListStore, ';');
                            if (checkStore != null && checkStore.Length > 0 && checkStore.Contains(storeNo))
                            {
                                var getDataOtherStatus = _redisCacheService.GetExtendedFieldLoyalty();
                                if (getDataOtherStatus != null)
                                {
                                    var checkOtherStatus = getDataOtherStatus.FirstOrDefault(x => x.Code.ToUpper() == item.Code.ToUpper() && x.StoreCode == storeNo && x.PhoneNumber == FormatHelper.PhoneNumberVietNam(codeValue));
                                    if (checkOtherStatus != null)
                                    {
                                        extendedFields.Add(new ExtendedField()
                                        {
                                            Name = checkOtherStatus.Code,
                                            Value = checkOtherStatus.Desc,
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                //List WINSCORE
                if (getSysWebApiConfig.FirstOrDefault(x => x.AppCode == AppCodeEnum.WINSCORE.ToString() && x.Blocked == false) != null)
                {
                    var checkWinscore = _winsoreService.Value.GetStatusWinscoreAsync(posID, FormatHelper.PhoneNumberVietNam(codeValue));
                    if (checkWinscore.Item1 == HttpStatusCode.OK)
                    {
                        var messConfig = _memoryCacheService.GetNotifyConfig(_memoryCacheService.GetConnectStringCentralMD()).Where(x => x.AppCode == AppCodeEnum.WINSCORE.ToString()).ToList();
                        string name = null;
                        if (checkWinscore.Item3 == 1)
                        {
                            name = OtherStatusEnum.WINSCORE_CONFIRM.ToString();
                        }
                        else if (checkWinscore.Item3 == 2)
                        {
                            name = OtherStatusEnum.WINSCORE.ToString();
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            extendedFields.Add(new ExtendedField()
                            {
                                Name = name,
                                Value = checkWinscore.Item2 ?? messConfig.FirstOrDefault(x => x.Status.ToUpper() == name.ToUpper()).Message,
                            });
                        }
                    }
                }
                return (otherStatus, extendedFields);
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("GetOtherStatusLoyalty", posID, 0, "", JsonConvert.SerializeObject(ex));
                return (null, null);
            }
        }
        public void RetryUpdateStatusWinscore()
        {
            _winsoreService.Value.RetryUpdateStatusWinscore();
        }

    }
}