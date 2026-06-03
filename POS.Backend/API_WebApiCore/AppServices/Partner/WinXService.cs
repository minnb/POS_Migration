using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TCX.API.Common.Const;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Capillary.Enosta;
using TCX.API.Common.Dtos.Capillary.Point;
using TCX.API.Common.Dtos.Capillary.Transaction;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Ops;
using TCX.API.Common.Dtos.WinX;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore.Shared;

namespace TCX.WebApiCore
{
    public class WinXService
    {
        private readonly KibanaService _kibanaService;
        private readonly string _subjectSMS = $"{AppGlobals.WebApiSystem} - sử dụng dịch vụ webApi WINX";
        public WinXService()
        {
            _kibanaService = new KibanaService();
        }
        public async Task<(PosPostTransactionsResponse, string)> PosPostTransactions(MemoryCacheService memoryCacheService, MMLSchemeRequest transaction)
        {
            try
            {
                var result = await CallApiTheWinX(memoryCacheService, "PosPostTransactions", transaction.PosNo, transaction, timeout: 10);
                if (result.Item3 == (int)HttpStatusCode.OK)
                {
                    var responseData = StringHelper.StringToObject<WinxResponse>(result.Item1);
                    if (responseData != null && responseData.Status == 200 && responseData.Data != null)
                    {
                        var dataQrCode = StringHelper.StringToObject<PosPostTransactionsResponse>(JsonConvert.SerializeObject(responseData.Data));
                        return (dataQrCode, "OK");
                    }
                    else
                    {
                        return (null, JsonConvert.SerializeObject(result));
                    }
                }
                else
                {
                    return (null, result.Item1);
                }
            }
            catch (Exception ex)
            {
                return (null, JsonConvert.SerializeObject(ex));
            }
        }

        public async Task<(string, string, string)> ResolveWinXUserIdCapillary(MemoryCacheService memoryCacheService, string posNo, string barcode)
        {
            try
            {
                var request = new TCX.API.Common.Dtos.WinX.ResolveWinxUserIdRequest()
                {
                    Winx_user_ids = new List<string>() { $"WIN-{barcode}" }
                };
                var result = await CallApiTheWinX(memoryCacheService, "ResolveWinxUserId", posNo, request);
                if (result.Item3 == (int)HttpStatusCode.OK)
                {
                    var responseData = StringHelper.StringToObject<TCX.API.Common.Dtos.WinX.ResolveWinxUserIdCapillary>(result.Item1);
                    if (responseData != null && responseData.Status == 200 && responseData.Data != null && responseData.Data.Data != null && responseData.Data.Data.Count > 0)
                    {
                        return (responseData.Data.Data.FirstOrDefault().Capillary_user_id,  "OK", barcode);
                    }
                    else if (responseData != null && responseData.Status == 200 && responseData.Data != null && responseData.Data.Errors != null && responseData.Data.Errors.Count > 0)
                    {
                        string mess = NotifyConfigHelper.GetNotifyConfigByStatus(memoryCacheService,
                                                                            PartnerEnum.WINX.ToString(),
                                                                            responseData.Data.Errors.FirstOrDefault().Error,
                                                                            barcode,
                                                                            "ResolveWinxUserId");

                        return (null, mess, JsonConvert.SerializeObject(responseData));
                    }
                    else
                    {
                        return (null, $"Barcode {barcode} không tồn tại", result.Item1);
                    }
                }
                else
                {
                    return (null, $"Lỗi kết nối The WinX", result.Item1);
                }
            }
            catch (Exception ex)
            {
                return (null, ex.Message, JsonConvert.SerializeObject(ex));
            }
        }

        public async Task<(string, string, string)> ResolveDynamicVouchers(MemoryCacheService memoryCacheService, string posNo, string voucher, string phoneNumber = "")
        {
            try
            {
                var RequestDynamicVouchersWinX = new TCX.API.Common.Dtos.WinX.RequestDynamicVouchersWinX()
                {
                    Dynamic_codes = new List<string>() { voucher }
                };
                var result = await CallApiTheWinX(memoryCacheService, "GetVoucherCapillary", posNo, RequestDynamicVouchersWinX);
                if(result.Item3 == (int)HttpStatusCode.OK)
                {
                    var responseData = StringHelper.StringToObject<TCX.API.Common.Dtos.WinX.ResponseDynamicVouchersWinX>(result.Item1);
                    if(responseData != null && responseData.Status == 200 && responseData.Data != null && responseData.Data.Data != null && responseData.Data.Data.Count > 0)
                    {
                        //if(responseData.Data.Data.FirstOrDefault().Uuser_phone_number != FormatHelper.PhoneNumberWithCountryCode(phoneNumber))
                        //{
                        //    string mess = NotifyConfigHelper.GetNotifyConfigByStatus(memoryCacheService,
                        //                                                    PartnerEnum.WINX.ToString(),
                        //                                                    "NOT_OWNED",
                        //                                                    voucher,
                        //                                                    "DynamicVoucher");
                        //    return (null, $"{mess} {responseData.Data.Data.FirstOrDefault().Uuser_phone_number}");
                        //}
                        return (responseData.Data.Data.FirstOrDefault().Capillary_voucher_code, voucher, null);
                    }
                    else if(responseData != null && responseData.Status == 200 && responseData.Data != null && responseData.Data.Errors != null && responseData.Data.Errors.Count > 0)
                    {
                        string mess = NotifyConfigHelper.GetNotifyConfigByStatus(memoryCacheService, 
                                                                            PartnerEnum.WINX.ToString(),
                                                                            responseData.Data.Errors.FirstOrDefault().Error,
                                                                            voucher,
                                                                            "DynamicVoucher");

                        return (null, mess, JsonConvert.SerializeObject(responseData));
                    }
                    else
                    {
                        return (null, $"Không tìm thấy voucher {voucher}", result.Item1);
                    }
                }
                else
                {
                    return (null, $"Lỗi kết nối The WinX", result.Item1);
                }
            }
            catch(Exception ex)
            {
                return (null, ex.Message, JsonConvert.SerializeObject(ex));
            }
        }
        private async Task<(string, long, int)> CallApiTheWinX(MemoryCacheService memoryCacheService, string function, string posNo, object bodyJson, string orderNo = "", int timeout = 0)
        {
            string url = string.Empty;
            try
            {
                var sysWebApiDto = memoryCacheService.GetSysWebApi(memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.WINX.ToString()).FirstOrDefault();
                url = sysWebApiDto?.Host;
                if (sysWebApiDto == null) return (MessConst.NotFounDataConfig, 0, (int)HttpStatusCode.BadRequest);
                
                var sysWebApiRoute = sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == function.ToUpper());
                if (sysWebApiRoute == null) return (MessConst.NotFounDataConfig, 0, (int)HttpStatusCode.BadRequest);

                var bodyJsonStr = StringHelper.ObjectToStringLowercase(bodyJson);
                if (timeout == 0) 
                { 
                    timeout = NumberHelper.TryParseInt(sysWebApiDto.Version, 30);
                }

                Dictionary<string, string> Headers = new Dictionary<string, string>
                {
                    { sysWebApiDto.UserName.Trim(), sysWebApiDto.Password.Trim() }
                };

                StringHelper.Loggingz($"CallApiTheWinX request:" + bodyJsonStr);
                ApiHttpClientHelper apiHttpClientHelper = new ApiHttpClientHelper(
                    sysWebApiDto.Host,
                    sysWebApiRoute.Route,
                    Headers,
                    null,
                    null,
                    MethodApiEnum.POST.ToString(),
                    bodyJsonStr,
                    timeout,
                    sysWebApiDto.HttpProxy
                    );
                _kibanaService.LogRequest(sysWebApiDto.Host + sysWebApiRoute.Route, posNo, "", bodyJsonStr);

                var response = await apiHttpClientHelper.HttpClientWithApiAsync();
                StringHelper.Loggingz($"CallApiTheWinX response:" + response.Item1);
                _kibanaService.LogResponse(sysWebApiDto.Host + sysWebApiRoute.Route, posNo, response.Item2, "", bodyJsonStr + $"{orderNo} response: " + response.Item1);

                if (HttpStatusConst.HttpSatusServerError.Contains(response.Item3))
                {
                    await Task.Run(() => HttpClientService.SendMessageSMS(
                                            $"{function} lỗi httpStatus: {response.Item3} kết nối tới WebApi THE-WINX. MsgError: {response.Item1}",
                                            MessageTypeEnum.Warning.ToString(),
                                            _subjectSMS, appCode: AppCodeMessageEnum.BLUEPOS_WINX_VOUCHERS.ToString()));
                }

                if (response.Item3 != (int)HttpStatusCode.OK) 
                {
                    Ops_Logging_Data ops_Logging_Data = new Ops_Logging_Data
                    {
                        App_module = sysWebApiDto.AppCode,
                        Endpoint = sysWebApiDto.Host + sysWebApiRoute.Route,
                        Error_code = ((HttpStatusCode)response.Item3).ToString(),
                        Error_message = response.Item1,
                        Error_type = OpsStatusEnum.warning.ToString(),
                        Server_code = HostHelper.GetIpAddress(),
                        Severity = response.Item3 == (int)HttpStatusCode.BadRequest ? "warning" : "error",
                        Request_id = orderNo,
                        Context = bodyJson,
                        Http_status = response.Item3
                    };
                    await Task.Run(() => OpsMonitoringHelper.OpsLogging(ops_Logging_Data));
                }

                return response;
            }
            catch (Exception ex)
            {
                _kibanaService.LogException(url, posNo, 0, "", JsonConvert.SerializeObject(ex));
                if (ex.InnerException != null)
                {
                    FileHelper.WriteExpLogs("CallApiOneU InnerException:", ex);
                }
                await Task.Run(() => HttpClientService.SendMessageSMS(
                                        $"{function} lỗi kết nối tới WebApi OneU. MsgError: {ex.Message}",
                                        MessageTypeEnum.Warning.ToString(),
                                        _subjectSMS, appCode: AppCodeMessageEnum.BLUEPOS_WINX_VOUCHERS.ToString()));
                return (ex.Message, 0, (int)HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<(HttpStatusCode, string, List<EarnLineItems>)> UpdateTransactionReturn(MemoryCacheService memoryCacheService, AddTransactionCapPOSRequest request, VinIDRefundRequest POSrequest, string trans_id)
        {
            try
            {
                (HttpStatusCode, string, TransactionReturnResponse) resultRespone;
                if (request.TransactionType == 2 && request.Type == TransactionTypeCapEnum.RETURN.ToString())
                {
                    var transactionReturnFullRequest = new TransactionReturnFullRequest()
                    {
                        Type = request.Type,
                        ReturnType = ReturnTypeCapEnum.FULL.ToString(),
                        BillNumber = request.BillNumber,
                        Mobile = request.MemberCard,
                        Return_trans_id = trans_id,
                    };
                    resultRespone = await CallUpdateTransactionReturn(request.MemberCard, memoryCacheService, transactionReturnFullRequest, trans_id, POSrequest.OrderNo);
                }
                else
                {
                    var transactionReturnPartialRequest = new TransactionReturnPartialRequest()
                    {
                        Type = request.Type,
                        ReturnType = ReturnTypeCapEnum.LINE_ITEM.ToString(),
                        BillNumber = request.BillNumber,
                        Mobile = request.MemberCard,
                        BillAmount = request.BillAmount,
                        GrossAmount = request.GrossAmount,
                        BillingDate = request.BillingDate,
                        Return_trans_id = trans_id
                    };
                    List<EnostaLineItemV2> lineItemsV2 = new List<EnostaLineItemV2>();
                    foreach (var item in request.LineItemsV2)
                    {
                        lineItemsV2.Add(new EnostaLineItemV2()
                        {
                            Discount = (int)item.Discount,
                            Item_code = item.ItemCode,
                            Description = item.Description,
                            Rate = item.Rate,
                            Amount = item.Amount,
                            Qty = item.Qty,
                            Serial = item.Serial,
                            ExtendedFields = item.ExtendedFields
                        });
                    }
                    transactionReturnPartialRequest.LineItemsV2 = lineItemsV2;
                    resultRespone = await CallUpdateTransactionReturn(request.MemberCard, memoryCacheService, StringHelper.ObjectToStringLowercase(transactionReturnPartialRequest), trans_id, POSrequest.OrderNo);
                }
                if(resultRespone.Item1 == HttpStatusCode.OK && resultRespone.Item3 != null)
                {
                    if(resultRespone.Item3.Data != null && resultRespone.Item3.Data.Response != null 
                        && resultRespone.Item3.Data.Response.Status != null 
                        && resultRespone.Item3.Data.Response.Status.Success == "true"
                        && resultRespone.Item3.Data.Response.Customer != null
                        && resultRespone.Item3.Data.Response.Customer.Transactions != null
                        && resultRespone.Item3.Data.Response.Customer.Transactions.Transaction != null
                        && resultRespone.Item3.Data.Response.Customer.Transactions.Transaction.FirstOrDefault() != null
                        && resultRespone.Item3.Data.Response.Customer.Transactions.Transaction.FirstOrDefault().Line_items != null
                        && resultRespone.Item3.Data.Response.Customer.Transactions.Transaction.FirstOrDefault().Line_items.Line_item != null)
                    {
                        var earnLineItems = new List<EarnLineItems>();
                        foreach (var item in resultRespone.Item3.Data.Response.Customer.Transactions.Transaction.FirstOrDefault().Line_items.Line_item)
                        {
                            var checkLine = POSrequest.TransLine.FirstOrDefault(x => x.LineNo == item.Serial);
                            if(checkLine != null)
                            {
                                earnLineItems.Add(new EarnLineItems()
                                {
                                    LineNo = (int)item.Serial,
                                    ItemCode = item.Item_code,
                                    UnitOfMeasure = checkLine.UnitOfMeasure,
                                    Quantity = item.Qty,
                                    LineAmountIncVAT = checkLine.LineAmountIncVAT,
                                    DiscountAmount = item.Discount,
                                    Description = item.Description,
                                    UnitPrice = item.Rate,
                                    DiscountType = 0,
                                    VatAmount = item.Amount,
                                    Size = checkLine.Size,
                                    CardLevel = checkLine.CardLevel,
                                    EarnPoints = (int)decimal.Parse(item.Base_points_returned??"0"),
                                });
                            }
                        }
                        return (HttpStatusCode.OK, "OK", earnLineItems);
                    }
                    else
                    {
                        return (HttpStatusCode.BadRequest, resultRespone.Item3.Data.Response.Status.Message, null);
                    }
                }
                else
                {
                    return (resultRespone.Item1, resultRespone.Item2, null);
                }
            }
            catch(Exception ex)
            {
                return (HttpStatusCode.ExpectationFailed, ex.Message, null);
            }
        }
        private async Task<(HttpStatusCode, string, TransactionReturnResponse)> CallUpdateTransactionReturn(string phoneNumber, MemoryCacheService memoryCacheService, object body, string return_trans_id = "", string orderNo = "")
        {
            long responseTime = 0;
            string response = "";
            ServerErrorResponses responseErr = new ServerErrorResponses();
            var lstSysWebApiDto = memoryCacheService.GetSysWebApi(memoryCacheService.GetConnectStringCentralMD());
            var webApiDto = lstSysWebApiDto.FirstOrDefault(x => x.AppCode.ToUpper() == "Enosta".ToUpper());
            if (webApiDto == null) return (HttpStatusCode.NotFound, MessageConst.ApiNotFounDataConfig, null);

            var router = webApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "UpdateTransactionReturn".ToUpper() && x.Blocked == false);
            if (router == null) return (HttpStatusCode.NotFound, MessageConst.ApiNotFounDataConfig, null);

            try
            {
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "client-id", webApiDto.UserName },
                    { "client-secret-key", webApiDto.Password }
                };

                RestSharpApiHelper apiCallHelper = new RestSharpApiHelper(
                                                                "WINX",        
                                                                webApiDto.Host, 
                                                                router.Route + router.Notes ?? "",
                                                                "POST",
                                                                headers,
                                                                null,
                                                                body,
                                                                NumberHelper.GetTimeOutApi(webApiDto.Version),
                                                                webApiDto.HttpProxy,
                                                                9090
                                                                );
                _kibanaService.LogRequest(webApiDto.Host + router.Route, return_trans_id, "", $"{phoneNumber}_{return_trans_id}_{body}");
                var result = await apiCallHelper.InteractWithApiAsync();
                response = result.Item1;
                _kibanaService.LogResponse(webApiDto.Host + router.Route, return_trans_id, responseTime, "", $"{orderNo}_{phoneNumber}_{return_trans_id}_response:" + response);

                if (!string.IsNullOrEmpty(response))
                {
                    var dataResponse = JsonConvert.DeserializeObject<TransactionReturnResponse>(response);
                    if (dataResponse != null && dataResponse.Status == 200)
                    {
                        _kibanaService.LogResponse(webApiDto.Host + router.Route, "", responseTime, "", JsonConvert.SerializeObject(dataResponse));
                        return (HttpStatusCode.OK, "OK", dataResponse);
                    }
                    else
                    {
                        var checkError = JsonConvert.DeserializeObject<ErrorCapillaryResponse>(response);
                        if (checkError.Errors != null)
                        {
                            return (HttpStatusCode.BadRequest, response, null);
                        }
                        return (HttpStatusCode.NoContent, response, null);
                    }
                }
                else
                {
                    await Task.Run(() => HttpClientService.SendMessageSMS($"Lỗi kết nối tới WebApi:{webApiDto.Host + router.Route}", "Error", $"HV WIN: {phoneNumber} - lỗi kết nối tới TheWinX", appCode: AppCodeMessageEnum.BLUEPOS_WINX_RETURN.ToString()));
                    return (HttpStatusCode.NoContent, "NoContent", null);
                }
            }
            catch (Exception ex)
            {
                string messError = ex.Message;
                if (ex.InnerException != null)
                {
                    messError += "_" + ex.InnerException.Message;
                }
                _kibanaService.LogException(webApiDto.Host + router, phoneNumber, 0,"", $"{phoneNumber} Exception:" + JsonConvert.SerializeObject(ex));
                await Task.Run(() => HttpClientService.SendMessageSMS($"{messError}_HV_WIN: {phoneNumber}_Request: {body}", "Error", $"HV WIN: {phoneNumber} - lỗi kết nối tới The WinX {webApiDto.Host + router.Route}", appCode: AppCodeMessageEnum.BLUEPOS_WINX_RETURN.ToString()));
                return (HttpStatusCode.ExpectationFailed, $"{response} ExpectationFailed: {ex.Message}", null);
            }
        }
    }
}