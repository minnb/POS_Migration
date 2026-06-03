using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VCM.POSBLUE.Model.SAP;
using TCX.API.Common.Shared;
using TCX.API.Common.Models;
using TCX.API.Common.Helpers;


namespace TCX.WebApiCore
{
    public class KibanaService
    {
        public KibanaService() 
        {
        }
        public void SendMessageSMS(string message, string notify = "Info", string subject = "", string messageId = "", string text = "",string appCode = "")
        {
            HttpClientService.SendMessageSMS(message, notify, subject, messageId, text, appCode);
        }
        public void LogRequest(string webApi, string posNo, string endPoint, string bodyJson)
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? "";
            var spanId = Activity.Current?.SpanId.ToString() ?? "";
            _ = Task.Run(() =>
            {
                try
                {
                    Serilog.Log.Logger.Warning(String.Format("Request {0} payload {1}", webApi + endPoint, bodyJson) + " {HttpContext} {WebApi} {PosNo} {trace_id} {span_id}", "Request", webApi + endPoint, posNo, traceId, spanId);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error in Fire-and-Forget log");
                }
            });
        }
        public void LogResponse(string webApi, string posNo, long timeResponse, string endPoint, string bodyJson)
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? "";
            var spanId = Activity.Current?.SpanId.ToString() ?? "";
            _ = Task.Run(() =>
            {
                try
                {
                    Serilog.Log.Logger.Warning(String.Format("{0} response: {1}", endPoint, bodyJson) + " {ResponseTime} {WebApi} {PosNo} {HttpContext} {trace_id} {span_id}", timeResponse, webApi + endPoint, posNo ?? ComputerHelper.GetHostName(), "Response", traceId, spanId);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error in Fire-and-Forget log");
                }
            });
        }
        public void LogException(string webApi, string posNo, long timeResponse, string endPoint, string bodyJson)
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? "";
            var spanId = Activity.Current?.SpanId.ToString() ?? "";
            _ = Task.Run(() =>
            {
                try
                {
                    Serilog.Log.Logger.Error(String.Format("{0} response: {1}", endPoint, bodyJson) + " {ResponseTime} {WebApi} {PosNo} {HttpContext} {trace_id} {span_id}", timeResponse, webApi + endPoint, posNo ?? ComputerHelper.GetHostName(), "Exception", traceId, spanId);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error in Fire-and-Forget log");
                }
            });
        }
        public void LoggingResponseWebApi(LoggingElastic loggingElastic)
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? "";
            var spanId = Activity.Current?.SpanId.ToString() ?? "";
            _ = Task.Run(() =>
            {
                try
                {
                    Serilog.Log.Logger.Information("{HttpContext} {PosNo} {WebApi} {ResponseTime} {TotalBill} {DeveloperMessage} {trace_id} {span_id}", loggingElastic.HttpContext, loggingElastic.PosNo, loggingElastic.WebApi, loggingElastic.ResponseTime, loggingElastic.TotalBill, loggingElastic.DeveloperMessage, traceId, spanId);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error in Fire-and-Forget log");
                }
            });
        }
        public void LoggingToLoyaltyDB(string posNo, string webApi, string actionType, long timeResponse, string orderNo = "", string requestPOS = "", string requestPartner = "", string response = "", string cardNumber = "")
        {
            try
            {
                if (string.IsNullOrEmpty(cardNumber))
                {
                    cardNumber = orderNo;
                }
                var loggingData = new CX_LogAPIModel()
                {
                    StoreNo = posNo.Substring(0, 4),
                    POSTerminal = posNo,
                    CardNumber = cardNumber,
                    InvoiceNo = orderNo,
                    ActionType = actionType,
                    PlainText = webApi,
                    Signature = "",
                    RequestPOS = requestPOS,
                    RequestXML = requestPartner,
                    ResponseXML = response,
                    ResponseTotal = timeResponse,
                    DateTime = DateTime.Now,
                    Source = ComputerHelper.GetHostName() ?? ""
                };
                Save_CX_LogAPI(loggingData);
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("LoggingToLoyaltyDB Exception",ex);
                throw;
            }
        }
        public void Save_CX_LogAPI(CX_LogAPIModel model)
        {
            //using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
            //{
            //    db.Open();
            //    try
            //    {
            //        string queryIns = @"INSERT INTO [dbo].[CX_LogAPI]
            //                                    ([StoreNo],[POSTerminal],[CardNumber],[InvoiceNo],[ActionType],[PlainText],[Signature],[RequestPOS]
            //                                    ,[RequestXML],[ResponseXML],[ResponseTotal],[Source],[DateTime])
            //                                VALUES(@StoreNo,@POSTerminal,@CardNumber,@InvoiceNo,@ActionType,@PlainText,@Signature,@RequestPOS
            //                                    ,@RequestXML,@ResponseXML,@ResponseTotal,@Source,@DateTime)";
            //        db.Execute(queryIns, model);
            //    }
            //    catch (Exception ex)
            //    {
            //        FileHelper.WriteExpLogs("Save_CX_LogAPI.Exception", ex);
            //    }
            //}
        }
        public void LoggingToLogSAP(string function, string POSTerminal, string voucherNo, string requestPOS, string requestPartner, string response)
        {
            //var modelSAPLog = new LogSAPModel
            //{
            //    StoreNo = StringHelper.Left(POSTerminal, 4),
            //    POSTerminal = POSTerminal,
            //    VoucherNumber = voucherNo,
            //    ActionType = function,
            //    RequestPOS = requestPOS,
            //    RequestSAP = requestPartner,
            //    ResponseSAP = response,
            //    CreatedDate = DateTime.Now
            //};
            //Task.Run(() => Save_LogSAP(modelSAPLog));
        }
        public void Save_LogSAP(LogSAPModel model)
        {
            //using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
            //{
            //    db.Open();
            //    try
            //    {
            //        //string queryIns = @"INSERT INTO [dbo].[LogSAP] ([StoreNo],[POSTerminal],[VoucherNumber],[RequestPOS],[RequestSAP],[ResponseSAP],[ActionType],[CreatedDate])
            //        //                    VALUES(@StoreNo,@POSTerminal,@VoucherNumber,@RequestPOS,@RequestSAP,@ResponseSAP,@ActionType,@CreatedDate)";
            //        //db.Execute(queryIns, model);
            //    }
            //    catch (Exception ex)
            //    {
            //        FileHelper.WriteExpLogs("Exception Save_LogSAP", ex);
            //    }
            //}
        }
        public void LoggingLoyalty(string appCode, string actionType, string orderNo, string voucherNo, string requestPOS, string requestPartner, string response)
        {

        }
    }
}