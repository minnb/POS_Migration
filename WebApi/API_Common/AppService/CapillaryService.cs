using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Capillary.Customer;
using TCX.API.Common.Dtos.Capillary.Point;
using TCX.API.Common.Dtos.Capillary.Redemption;
using TCX.API.Common.Dtos.Capillary.Transaction;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using VTCX.API.Common.Enums;

namespace TCX.API.Common.AppService
{
    public interface ICapillaryLoyaltyService
    {
        Task<Tuple<bool, string, long, object, int>> GetCustomerDetail(SysWebApiDto sysWebApiDto,string storeNo, string posNo, string phoneNumber, string memberCapillary, List<NotifyConfigDto> notifyConfig = null);
        Tuple<bool, string, long, object> CustomerRegistration(SysWebApiDto sysWebApiDto, CustomerRegistrationRequest customer, string storeNo, string phoneNumber="", List<NotifyConfigDto> notifyConfig = null);
        Tuple<bool, string, long, object> CustomerUpdate(SysWebApiDto sysWebApiDto, CustomerUpdateCapillary customer, string storeNo, string phoneNumber = "", List<NotifyConfigDto> notifyConfig = null);
        Task<Tuple<bool, string, long, object>> AddTransaction(SysWebApiDto sysWebApiDto, object transation, string storeNo, string memberCard, string orderNo = "", List<NotifyConfigDto> notifyConfig = null, List<string> programId = null);
        Task<Tuple<bool, string, long, object>> GetPointsLedger(SysWebApiDto sysWebApiDto, string storeNo, string memberCard, List<NotifyConfigDto> notifyConfig = null);
        Tuple<bool, string, long, object> PointsRedeem(SysWebApiDto sysWebApiDto, PointsRedeemCapRequest pointsRedeemCapRequest, string storeNo, string programId, List<NotifyConfigDto> notifyConfig = null);
        Tuple<bool, string, long, object> PointReverse(SysWebApiDto sysWebApiDto, PointReverseCapRequest request, string storeNo, List<NotifyConfigDto> notifyConfig = null);
        Tuple<bool, string, long, object> PointsRedeemable(SysWebApiDto sysWebApiDto, string memberCard, int point, string storeNo, List<NotifyConfigDto> notifyConfig = null);
        Tuple<bool, string, long, object> RedemptionValdationData(SysWebApiDto sysWebApiDto, string store, string mobile, string code, bool details, ref int statusCode, List<NotifyConfigDto> notifyConfig = null);
    }
    public class CapillaryService: ICapillaryLoyaltyService
    {
        public readonly string[] httpStatusErrorCapillary= new string[] { "524", "520", "500" }; 
        public CapillaryService()
        {

        }
        public async Task<Tuple<bool, string, long, object>> AddTransaction(SysWebApiDto sysWebApiDto, object transation, string storeNo, string memberCard, string orderNo = "", List<NotifyConfigDto> notifyConfig = null, List<string> programId =  null)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "AddTransaction".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, long, object>(false, MessageConst.ApiNotFounDataConfig, 0, null);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "x-winx-loyalty-client", "POS" }
                };

                string param = @"?source=instore&identifierValue=" + memberCard + @"&identifierName=mobile" + router.Notes ?? "";
                if(programId != null && !string.IsNullOrEmpty(programId.FirstOrDefault()))
                {
                    param = @"?source=instore&identifierValue=" + memberCard + $"&identifierName=mobile&program_id={programId.FirstOrDefault()}" + router.Notes ?? "";
                }
                string accessToken = Shared.EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + storeNo, sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(transation),
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );
                var result = await apiCallHelper.HttpClientWithApiAsync();
                response = result.Item1;
                responseTime = result.Item2;
                responseErr = StringHelper.StringToObject<ServerErrorResponses>(JsonConvert.SerializeObject(result.Item3));
                StringHelper.Loggingz("AddTransaction:" + response);
                if (!string.IsNullOrEmpty(response))
                {
                    AddTransactionResponse dataResponse = StringHelper.StringToObject<AddTransactionResponse>(response) ?? StringHelper.StringToObject<List<AddTransactionResponse>>(response).FirstOrDefault();
                    
                    if (dataResponse != null && dataResponse.CreatedId > 0)
                    {
                        if(dataResponse.Warnings != null && dataResponse.Warnings.Count == 0)
                        {
                            return new Tuple<bool, string, long, object>(true, "Success", responseTime, dataResponse);
                        }
                        else
                        {
                            var checkError = JsonConvert.DeserializeObject<ErrorCapillaryResponse>(response);
                            if (checkError.Errors != null && checkError.Errors.Count > 0)
                            {
                                return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Errors.ToString(), JsonConvert.SerializeObject(checkError.Errors.FirstOrDefault())), responseTime, checkError);
                            }

                            if (!string.IsNullOrEmpty(JsonConvert.SerializeObject(dataResponse.Warnings)))
                            {
                                return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Warnings.ToString(), JsonConvert.SerializeObject(dataResponse)), responseTime, dataResponse);
                            }
                            return new Tuple<bool, string, long, object>(false, @"Lỗi: " + JsonConvert.SerializeObject(dataResponse), responseTime, dataResponse);
                        }
                    }
                    else
                    {
                        var responseErrorRawSideEffects = StringHelper.StringToObject<AddTransactionErrorResponse>(response);
                        var checkError = JsonConvert.DeserializeObject<ErrorCapillaryResponse>(response);
                        if (checkError.Errors != null)
                        {
                            return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Warnings.ToString(), JsonConvert.SerializeObject(checkError.Errors.FirstOrDefault())), responseTime, checkError);
                        }
                        return new Tuple<bool, string, long, object>(false, @"Lỗi kết quả trả về từ WebApi Capillary " + response, responseTime, checkError);
                    }
                }
                else
                {
                    return new Tuple<bool, string, long, object>(false, @"Lỗi kết nối tới WebApi Capillary", responseTime, null);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("AddTransaction Exception:" + JsonConvert.SerializeObject(ex) + " ==>" + response);
                var responseErrorRawSideEffects = StringHelper.StringToObject<AddTransactionErrorResponse>(response);
                if (responseErrorRawSideEffects != null && responseErrorRawSideEffects.CreatedId > 0)
                {
                    if (responseErrorRawSideEffects.Warnings != null && responseErrorRawSideEffects.Warnings.Count == 0)
                    {
                        return new Tuple<bool, string, long, object>(true, "Success", responseTime, responseErrorRawSideEffects);
                    }
                }

                var dataResponseError = StringHelper.StringToObject<List<AddTransactionResponse>>(response);
                if (dataResponseError != null && dataResponseError.FirstOrDefault() != null && dataResponseError.FirstOrDefault().CreatedId > 0) 
                {
                    return new Tuple<bool, string, long, object>(true, "Success", responseTime, dataResponseError.FirstOrDefault());
                }

                if (httpStatusErrorCapillary.Contains(response))
                {
                    FileHelper.WriteLogs(JsonConvert.SerializeObject(transation));
                    return new Tuple<bool, string, long, object>(false, response, -9999, ex.InnerException);
                }
                if (CheckContainsResponse(response, "Unauthorized"))
                {
                    return new Tuple<bool, string, long, object>(false, string.Format("(401) Unauthorized. Store {0} chưa được tạo trên Capillary", storeNo), responseTime, ex.InnerException);
                }
                
                if (responseErr != null && responseErr.StatusCode > 0) return new Tuple<bool, string, long, object>(false, response, responseTime, responseErr);
                
                if (!string.IsNullOrEmpty(response)) return new Tuple<bool, string, long, object>(false, response, responseTime, ex.InnerException);
                
                return new Tuple<bool, string, long, object>(false, ExceptionHelper.GetExceptionMessage(ex), -9999, ex.InnerException);
            }
        }
        public Tuple<bool, string, long, object> CustomerRegistration(SysWebApiDto sysWebApiDto, CustomerRegistrationRequest customer, string storeNo, string phoneNumber = "", List<NotifyConfigDto> notifyConfig = null)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "CustomerRegistration".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, long, object>(false, MessageConst.ApiNotFounDataConfig, 0, null);
                //create token
                string accessToken = Shared.EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + storeNo, sysWebApiDto.Password);
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" },
                    { "X-CAP-API-ATTRIBUTION-LOOKUP", "masan.vn.integration.pos" },
                    { "x-winx-loyalty-client", "POS" }
                };

                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, router.Route + router.Notes ?? "",
                                                                null,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(customer),
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );

                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);

                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = JsonConvert.DeserializeObject<CustomerRegistrationResponse>(response);
                    if (dataResponse != null && dataResponse.CreatedId > 0)
                    {
                        return new Tuple<bool, string, long, object>(true, "Đăng ký thành viên thành công", responseTime, dataResponse);
                    }
                    else
                    {
                        var checkError = JsonConvert.DeserializeObject<ErrorCapillaryResponse>(response);
                        if (checkError.Errors != null)
                        {
                            return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Warnings.ToString(), JsonConvert.SerializeObject(checkError.Errors.FirstOrDefault())), responseTime, checkError);
                        }

                        return new Tuple<bool, string, long, object>(false, @"Lỗi kết quả trả về từ WebApi Capillary " + response, responseTime, checkError);
                    }
                }
                else
                {
                    return new Tuple<bool, string, long, object>(false, @"Lỗi kết nối tới WebApi Capillary", responseTime, null);
                }

            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("CustomerRegistration Exception response:" + response);
                FileHelper.WriteLogs("CustomerRegistration Exception:" + JsonConvert.SerializeObject(ex));

                if (CheckContainsResponse(response, "Unauthorized"))
                {
                    return new Tuple<bool, string, long, object>(false, string.Format("(401) Unauthorized. Store {0} chưa được tạo trên Capillary", storeNo), responseTime, ex.InnerException);
                }

                if (!string.IsNullOrEmpty(response)) return new Tuple<bool, string, long, object>(false, response, responseTime, ex.InnerException);

                return new Tuple<bool, string, long, object>(false, ExceptionHelper.GetExceptionMessage(ex), responseTime, ex.InnerException);
            }
        }
        public Tuple<bool, string, long, object> CustomerUpdate(SysWebApiDto sysWebApiDto, CustomerUpdateCapillary customer, string storeNo, string phoneNumber = "", List<NotifyConfigDto> notifyConfig = null)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "UpdateCustomer".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, long, object>(false, MessageConst.ApiNotFounDataConfig, 0, null);
                //create token
                string accessToken = Shared.EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + storeNo, sysWebApiDto.Password);
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" },
                    { "X-CAP-API-ATTRIBUTION-LOOKUP", "masan.vn.integration.pos" },
                    { "x-winx-loyalty-client", "POS" }
                };
                string param = @"?source=INSTORE&identifierName=mobile&identifierValue=" + phoneNumber;

                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, router.Route + param,
                                                                null,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "PUT",
                                                                StringHelper.ObjectToStringLowercase(customer),
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );

                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);

                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = JsonConvert.DeserializeObject<CustomerRegistrationResponse>(response);
                    if (dataResponse != null && dataResponse.CreatedId > 0)
                    {
                        return new Tuple<bool, string, long, object>(true, "Cập nhật thông tin thành công", responseTime, dataResponse);
                    }
                    else
                    {
                        var checkError = JsonConvert.DeserializeObject<ErrorCapillaryResponse>(response);
                        if (checkError.Errors != null)
                        {
                            return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Errors.ToString(), response), responseTime, checkError);
                        }
                        return new Tuple<bool, string, long, object>(false, @"Lỗi kết quả trả về từ WebApi Capillary " + response, responseTime, checkError);
                    }
                }
                else
                {
                    return new Tuple<bool, string, long, object>(false, @"Lỗi kết nối tới WebApi Capillary", responseTime, null);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("CustomerUpdate Exception response:" + response);
                FileHelper.WriteLogs("CustomerUpdate Exception:" + JsonConvert.SerializeObject(ex));

                if (CheckContainsResponse(response, "Unauthorized"))
                {
                    return new Tuple<bool, string, long, object>(false, string.Format("(401) Unauthorized. Store {0} chưa được tạo trên Capillary", storeNo), responseTime, ex.InnerException);
                }
                
                if (responseErr != null && responseErr.StatusCode > 0) return new Tuple<bool, string, long, object>(false, response, responseTime, responseErr);

                if (!string.IsNullOrEmpty(response)) return new Tuple<bool, string, long, object>(false, response, responseTime, ex.InnerException);

                return new Tuple<bool, string, long, object>(false, ExceptionHelper.GetExceptionMessage(ex), responseTime, ex.InnerException);
            }
        }
        public async Task<Tuple<bool, string, long, object, int>> GetCustomerDetail(SysWebApiDto sysWebApiDto, string storeNo, string posNo, string phoneNumber,string memberCapillary, List<NotifyConfigDto> notifyConfig = null)
        {
            var request_id = Guid.NewGuid().ToString();
            int statusCode = 400;
            long responseTime = 0;
            string response = string.Empty;
            ServerErrorResponses responseErr = new ServerErrorResponses();
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetCustomer".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string,long, object, int>(false, MessageConst.ApiNotFounDataConfig,0, null, 401);
                //create token
                string accessToken = Shared.EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + storeNo, sysWebApiDto.Password);
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" },
                    { "X-API-REQUEST", request_id },
                    { "x-winx-loyalty-client", "POS" }
                };

                string param = @"?format=json&mobile=" + phoneNumber + router.Notes??"";
                if(memberCapillary == MemberCapillaryEnum.ID.ToString())
                {
                    param = @"?format=json&id=" + phoneNumber + router.Notes ?? "";
                }
                param += "&user_id=true&store_id=" + storeNo;
                ApiCallHelperV2 apiCallHelper = new ApiCallHelperV2(sysWebApiDto.Host, router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy
                                                                );

                var result = await apiCallHelper.HttpClientWithApi();
                response = result.Item1;
                responseTime = result.Item2;
                responseErr = result.Item3;
                responseErr.RequestId = request_id;
                StringHelper.Loggingz("GetCustomerDetail:" + JsonConvert.SerializeObject(response));
                if (!string.IsNullOrEmpty(response))
                {
                    if (response.Contains("408_requestTimedOut") || response.Contains("A task was canceled"))
                    {
                        return new Tuple<bool, string, long, object, int>(false, 
                                                                    $"RequestId: {request_id} " + response, 
                                                                    responseTime, responseErr, 408);
                    }

                    var dataResponse = JsonConvert.DeserializeObject<GetCustDetailCapillaryResponse>(response);
                    if(dataResponse != null)
                    {
                        statusCode = dataResponse.Response.Status.Code;
                        if (dataResponse.Response.Status.Code == 200)
                        {
                            return new Tuple<bool, string, long, object, int>(true, dataResponse.Response.Status.Message?? "Success", responseTime, dataResponse, 200);
                        }

                        return new Tuple<bool, string, long, object, int>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Item_status.ToString(), response), responseTime, dataResponse, statusCode);
                    }
                    else
                    {
                        return new Tuple<bool, string, long, object,int>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Item_status.ToString(), response) ?? response, responseTime, null, statusCode);
                    }
                }
                else
                {
                    return new Tuple<bool, string, long, object, int>(false, @"Lỗi kết nối tới WebApi Capillary", responseTime, responseErr, statusCode);
                }
            }
            catch(Exception ex) 
            {
                FileHelper.WriteExpLogs("GetCustomerDetail",ex);
                if (CheckContainsResponse(response, ObjectErrorEnum.Unauthorized.ToString()))
                {
                    statusCode = 401;
                    return new Tuple<bool, string, long, object, int>(false, string.Format("(401) Unauthorized. Store {0} chưa được tạo trên Capillary", storeNo), responseTime, ex.InnerException, statusCode);
                }

                if (CheckContainsResponse(response, ObjectErrorEnum.Item_status.ToString()))
                {
                    return new Tuple<bool, string, long, object, int>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Item_status.ToString(), response) ?? response, responseTime, null, statusCode);
                }

                if (CheckContainsResponse(response, ObjectErrorEnum.Errors.ToString()))
                {
                    return new Tuple<bool, string, long, object, int>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Errors.ToString(), response) ?? response, responseTime, null, statusCode);
                }
                
                if (responseErr != null && responseErr.StatusCode > 0) return new Tuple<bool, string, long, object,int>(false, response, responseTime, responseErr, statusCode);

                return new Tuple<bool, string, long, object, int>(false,$"RequestId: {request_id} Exception: {ExceptionHelper.GetExceptionMessage(ex)}", responseTime, ex.InnerException, statusCode);
            }
        }
        public async Task<Tuple<bool, string, long, object>> GetPointsLedger(SysWebApiDto sysWebApiDto, string storeNo, string memberCard, List<NotifyConfigDto> notifyConfig = null)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetPointsLedger".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, long, object>(false, MessageConst.ApiNotFounDataConfig, 0, null);

                string param = @"?identifierValue="+ memberCard  + router.Notes;
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" },
                    { "X-API-REQUEST", Guid.NewGuid().ToString() },
                };
                
                //create token
                string accessToken = Shared.EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + storeNo, sysWebApiDto.Password);
                ApiCallHelperV2 apiCallHelper = new ApiCallHelperV2(
                                                                sysWebApiDto.Host, 
                                                                router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy
                                                                );

                var result = await apiCallHelper.HttpClientWithApi();
                response = result.Item1;
                responseTime = result.Item2;
                responseErr = result.Item3;
                StringHelper.Loggingz(response);
                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = JsonConvert.DeserializeObject<CustomerDetailCapDto>(response);
                    if (dataResponse != null)
                    {
                        return new Tuple<bool, string, long, object>(true, "Success", responseTime, dataResponse);
                    }
                    else
                    {
                        var checkError = JsonConvert.DeserializeObject<ErrorCapillaryResponse>(response);
                        if (checkError.Errors != null)
                        {
                            return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Errors.ToString(), response), responseTime, checkError);
                        }

                        return new Tuple<bool, string, long, object>(false, @"Lỗi kết quả trả về từ WebApi Capillary " + response, responseTime, checkError);
                    }
                }
                else
                {
                    return new Tuple<bool, string, long, object>(false, @"Lỗi kết nối tới WebApi Capillary", responseTime, null);
                }

            }
            catch (Exception ex)
            {
                if (CheckContainsResponse(response, "Unauthorized"))
                {
                    return new Tuple<bool, string, long, object>(false, string.Format("(401) Unauthorized. Store {0} chưa được tạo trên Capillary", storeNo), responseTime, ex);
                }

                if (responseErr != null && responseErr.StatusCode > 0) return new Tuple<bool, string, long, object>(false, response, responseTime, responseErr);

                if (!string.IsNullOrEmpty(response)) return new Tuple<bool, string, long, object>(false, response, responseTime, ex);

                return new Tuple<bool, string, long, object>(false, JsonConvert.SerializeObject(ex), responseTime, ex);
            }
        }
        public Tuple<bool, string, long, object> PointReverse(SysWebApiDto sysWebApiDto, PointReverseCapRequest request, string storeNo, List<NotifyConfigDto> notifyConfig = null)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "PointReverse".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, long, object>(false, MessageConst.ApiNotFounDataConfig, 0, null);
                //create token
                string accessToken = Shared.EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + storeNo, sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, router.Route,
                                                                null,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(request),
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );

                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                StringHelper.Loggingz($"PointReverse: " + response);
                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = JsonConvert.DeserializeObject<PointReverseCapResponse>(response);
                    if (dataResponse != null && dataResponse.OrgId > 0)
                    {
                        return new Tuple<bool, string, long, object>(true, "Success", responseTime, dataResponse);
                    }
                    else
                    {
                        var checkError = JsonConvert.DeserializeObject<ErrorCapillaryResponse>(response);
                        
                        if (checkError.Errors != null)
                        {
                            return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Errors.ToString(), JsonConvert.SerializeObject(checkError.Errors.FirstOrDefault())), responseTime, checkError);
                        }
                        
                        return new Tuple<bool, string, long, object>(false, @"Lỗi kết quả trả về từ WebApi Capillary " + response, responseTime, null);
                    }
                }
                else
                {
                    return new Tuple<bool, string, long, object>(false, @"Lỗi kết nối tới WebApi Capillary", responseTime, null);
                }

            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("PointReverse Exception response:" + response);
                FileHelper.WriteLogs("PointReverse Exception:" + JsonConvert.SerializeObject(ex));

                if (CheckContainsResponse(response, "Unauthorized"))
                {
                    return new Tuple<bool, string, long, object>(false, string.Format("(401) Unauthorized. Store {0} chưa được tạo trên Capillary", storeNo), responseTime, ex.InnerException);
                }

                if (responseErr != null && responseErr.StatusCode > 0) return new Tuple<bool, string, long, object>(false, response, responseTime, responseErr);

                if (!string.IsNullOrEmpty(response)) return new Tuple<bool, string, long, object>(false, response, responseTime, ex.InnerException);

                return new Tuple<bool, string, long, object>(false, ExceptionHelper.GetExceptionMessage(ex), responseTime, ex.InnerException);
            }
        }
        public Tuple<bool, string, long, object> PointsRedeem(SysWebApiDto sysWebApiDto, PointsRedeemCapRequest pointsRedeemCapRequest, string storeNo, string programId, List<NotifyConfigDto> notifyConfig = null)
        {
            long responseTime = 0;
            string response = "";
            string messageErr = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "RedeemPoints".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, long, object>(false, MessageConst.ApiNotFounDataConfig, 0, null);
               
                var param = router.Route + router.Notes ?? "" ;
                if (!string.IsNullOrEmpty(programId))
                {
                    param += $"&program_id={programId}";
                }

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "x-winx-loyalty-client", "POS" }
                };

                //create token
                string accessToken = Shared.EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + storeNo, sysWebApiDto.Password);
                StringHelper.Loggingz($"pointsRedeemCapRequest: {sysWebApiDto.Host + param} bodyData: {StringHelper.ObjectToStringLowercase(pointsRedeemCapRequest)}", true);
                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "POST",
                                                                StringHelper.ObjectToStringLowercase(pointsRedeemCapRequest),
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );

                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = JsonConvert.DeserializeObject<PointRedeemCapResponse>(response);
                    if (dataResponse != null && dataResponse.Response != null)
                    {
                        messageErr = dataResponse.Response.Status.Message;
                        if (dataResponse.Response.Responses != null && dataResponse.Response.Responses.Points != null)
                        {
                            if (dataResponse.Response.Responses.Points.Item_status != null) messageErr = dataResponse.Response.Responses.Points.Item_status.Message ?? "";
                        }

                        if (dataResponse.Response.Status.Code == 200 && dataResponse.Response.Status.Success.ToUpper() == "TRUE")
                        {
                            return new Tuple<bool, string, long, object>(true, messageErr, responseTime, dataResponse);
                        }

                        return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Item_status.ToString(), JsonConvert.SerializeObject(dataResponse.Response.Responses.Points.Item_status)), responseTime, dataResponse);
                    }
                    else
                    {
                        var checkError = JsonConvert.DeserializeObject<ErrorCapillaryResponse>(response);
                        if (checkError.Errors != null)
                        {
                            return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Errors.ToString(), JsonConvert.SerializeObject(checkError.Errors.FirstOrDefault())), responseTime, checkError);
                        }
                        return new Tuple<bool, string, long, object>(false, @"Lỗi kết quả trả về từ WebApi Capillary " + response, responseTime, checkError);
                    }
                }
                else
                {
                    return new Tuple<bool, string, long, object>(false, @"Lỗi kết nối tới WebApi Capillary", responseTime, null);
                }

            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("PointsRedeem Exception response:" + response);
                FileHelper.WriteLogs("PointsRedeem Exception:" + JsonConvert.SerializeObject(ex));

                if (CheckContainsResponse(response, ObjectErrorEnum.Unauthorized.ToString()))
                {
                    return new Tuple<bool, string, long, object>(false, string.Format("(401) Unauthorized. Store {0} chưa được tạo trên Capillary", storeNo), responseTime, ex.InnerException);
                }

                if (responseErr != null && responseErr.StatusCode > 0) return new Tuple<bool, string, long, object>(false, response, responseTime, responseErr);

                if (!string.IsNullOrEmpty(response)) return new Tuple<bool, string, long, object>(false, response, responseTime, ex.InnerException);

                return new Tuple<bool, string, long, object>(false, ExceptionHelper.GetExceptionMessage(ex), responseTime, ex.InnerException);
            }
        }
        public Tuple<bool, string, long, object> PointsRedeemable(SysWebApiDto sysWebApiDto, string memberCard, int point, string storeNo, List<NotifyConfigDto> notifyConfig = null)
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "PointsRedeemable".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, long, object>(false, MessageConst.ApiNotFounDataConfig, 0, null);

                string param = @"?mobile=" + memberCard + "&format=json&issue_otp=false&points=" + point;
                //create token
                string accessToken = Shared.EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + storeNo, sysWebApiDto.Password);
                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, 
                                                                router.Route + param,
                                                                null,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );

                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);

                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = JsonConvert.DeserializeObject<PointRedeemableCapResponse>(response);
                    if (dataResponse != null)
                    {
                        if(dataResponse.Response != null && dataResponse.Response.Status.Code == 200 && dataResponse.Response.Points.Redeemable.Is_redeemable == "true")
                        {
                            return new Tuple<bool, string, long, object>(true, "Success", responseTime, dataResponse);
                        }
                        else if (dataResponse.Response != null && dataResponse.Response.Status.Code == 500)
                        {
                            return new Tuple<bool, string, long, object>(false, string.Format("Số điểm hiện tại ít hơn số điểm yêu cầu quy đổi {0}", point), responseTime, dataResponse);
                        }
                        else
                        {
                            return new Tuple<bool, string, long, object>(false, response, responseTime, dataResponse);
                        }
                    }
                    else
                    {
                        var checkError = JsonConvert.DeserializeObject<ErrorCapillaryResponse>(response);
                        if (checkError.Errors != null)
                        {
                            return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Errors.ToString(), JsonConvert.SerializeObject(checkError.Errors.FirstOrDefault())), responseTime, checkError);
                        }

                        return new Tuple<bool, string, long, object>(false, @"Lỗi kết quả trả về từ WebApi Capillary " + response, responseTime, checkError);
                    }
                }
                else
                {
                    return new Tuple<bool, string, long, object>(false, @"Lỗi kết nối tới WebApi Capillary", responseTime, null);
                }

            }
            catch (Exception ex)
            {
                if (CheckContainsResponse(response, ObjectErrorEnum.Unauthorized.ToString()))
                {
                    return new Tuple<bool, string, long, object>(false, string.Format("(401) Unauthorized. Store {0} chưa được tạo trên Capillary", storeNo), responseTime, ex.InnerException);
                }

                if (responseErr != null && responseErr.StatusCode > 0) return new Tuple<bool, string, long, object>(false, response, responseTime, responseErr);

                if (!string.IsNullOrEmpty(response)) return new Tuple<bool, string, long, object>(false, response, responseTime, ex.InnerException);

                return new Tuple<bool, string, long, object>(false, ExceptionHelper.GetExceptionMessage(ex), responseTime, ex.InnerException);
            }
        }
        public Tuple<bool, string, long, object> RedemptionValdationData(SysWebApiDto sysWebApiDto, string store, string mobile, string code, bool details, ref int statusCode, List<NotifyConfigDto> notifyConfig = null)
        {
            long responseTime = 0;
            string response = string.Empty;
            ServerErrorResponses responseErr = new ServerErrorResponses();
            try
            {
                var router = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "CouponIsRedemption".ToUpper() && x.Blocked == false);
                if (router == null) return new Tuple<bool, string, long, object>(false, MessageConst.ApiNotFounDataConfig, 0, null);
                //create token
                string accessToken = Shared.EncryptionHelper.CreateTokenCapillary(sysWebApiDto.UserName + "." + store, sysWebApiDto.Password);

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-CAP-API-ATTRIBUTION-LOOKUP-TYPE", "MOBILE_TRIGGER" }
                };

                string param = @"?mobile=" + mobile + "&code=" + code + "&details=" + details;
                ApiCallHelper apiCallHelper = new ApiCallHelper(sysWebApiDto.Host, router.Route + param,
                                                                headers,
                                                                TokenTypeEnum.Basic.ToString(),
                                                                accessToken,
                                                                "GET",
                                                                null,
                                                                NumberHelper.GetTimeOutApi(sysWebApiDto.Version),
                                                                sysWebApiDto.HttpProxy,
                                                                sysWebApiDto.Bypasslist
                                                                );

                response = apiCallHelper.HttpClientWithApi(ref responseTime, ref responseErr);
                //FileHelper.WriteLogs("RedemptionValdationData response: " + response);
                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = JsonConvert.DeserializeObject<RedemptionResponse>(response);
                    if (dataResponse != null)
                    {
                        statusCode = dataResponse.RedemptionStatus.Code;
                        if (dataResponse.RedemptionStatus.Status == true && dataResponse.Redemption.FirstOrDefault().IsRedeemable)
                        {
                            return new Tuple<bool, string, long, object>(true, dataResponse.Redemption.FirstOrDefault().Code + " - " + dataResponse.RedemptionStatus.Message ?? "Success", responseTime, dataResponse);
                        }

                        return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Warnings.ToString(), response), responseTime, dataResponse);
                    }
                    else
                    {
                        return new Tuple<bool, string, long, object>(false, GetMessageCapillary(notifyConfig, ObjectErrorEnum.Warnings.ToString(), response) ?? response, responseTime, null);
                    }
                }
                else
                {
                    return new Tuple<bool, string, long, object>(false, @"Lỗi kết nối tới WebApi Capillary", responseTime, null);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("GetCustomerDetail Exception response:" + response);
                FileHelper.WriteLogs("GetCustomerDetail Exception:" + JsonConvert.SerializeObject(ex));

                if (CheckContainsResponse(response, "Unauthorized"))
                {
                    statusCode = 401;
                    return new Tuple<bool, string, long, object>(false, string.Format("(401) Unauthorized. Store {0} chưa được tạo trên Capillary", store), responseTime, ex.InnerException);
                }

                if (responseErr != null && responseErr.StatusCode > 0) return new Tuple<bool, string, long, object>(false, response, responseTime, responseErr);

                return new Tuple<bool, string, long, object>(false, ExceptionHelper.GetExceptionMessage(ex), responseTime, ex.InnerException);
            }
        }
        public bool CheckContainsResponse(string response, string contain)
        {
            if (!string.IsNullOrEmpty(response) && response.Contains(contain))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public string GetMessageCapillary(List<NotifyConfigDto> notifyConfigDto, string actionType, string response, string code = "")
        {
            string mess = response;
            try
            {
                if(notifyConfigDto == null) return mess;

                var messConfig = notifyConfigDto.Where(x => x.AppCode.ToUpper() == "CAP" && x.ActionType == actionType).ToList();
                if (messConfig != null && messConfig.Count > 0)
                {
                    foreach(var config in messConfig.OrderBy(x=>x.Status))
                    {
                        if (response.Contains(config.Status))
                        {
                            return $"{config.Status} - {config.Message} {code}";
                        }
                    }
                }
                else
                {
                    return mess;
                }
            }
            catch
            {

            }
            return mess;
        }
    }
}
