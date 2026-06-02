using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using TCX.API.Common.Dtos.StagingDB;
using Dapper;
using System.Linq;
using TCX.API.Common.Helpers;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.POS;
using TCX.API.Common.Dtos.Tax;
using System.Data.SqlClient;
using TCX.API.Common.Shared;
using TCX.API.Common.Enums;
using System.Threading.Tasks;
using System.Configuration;
using System.Net;
using TCX.WebApiCore.DbContext;
using StackExchange.Redis;
using RestSharp;

namespace TCX.WebApiCore
{
    public class DataRawJsonRepository
    {
        private readonly KibanaService _kibanaService;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly string _connectStringDb;
        private readonly IDatabase dbRedis;
        private readonly RedisManager redisManager;
        public DataRawJsonRepository()
        {
            _connectStringDb = ConfigurationManager.ConnectionStrings["DAPPER_CENTRAL_MD"].ConnectionString;
            _kibanaService = new KibanaService();
            dbRedis = RedisManager.GetDatabase();
            _memoryCacheService = new MemoryCacheService();
            redisManager = new RedisManager();
        }
        private string GetKeyRedis(string key, bool isCheck = false)
        {
            if(isCheck)
            {
                return $"InvoiceCreated:CHECK_" + key;
            }
            else
            {
                return $"InvoiceCreated:PRIMARY_" + key;
            }
        }
        public (bool, string, string) InsertInvoiceCreated(List<InvoiceCreated> invoiceCreated, string connectStringDb)
        {
            string message = "OK";
            try
            {
                var sanitizedInvoices = new List<InvoiceCreated>();
                foreach (var item in invoiceCreated)
                {
                    if (!RegexHelper.IsCharacters(item.OrderNo))
                    {
                        return (false, $"Phiếu tính tiền {item.OrderNo} sai định dạng", null);
                    }
                    var checkKey = dbRedis.StringGet(GetKeyRedis(item.OrderNo, true));
                    if (checkKey.IsNullOrEmpty)
                    {
                        return (false, $"Phiếu tính tiền {item.OrderNo} không hợp lệ", null);
                    }

                    var checkRedisExists = dbRedis.StringGet(GetKeyRedis(item.OrderNo));
                    if (!checkRedisExists.IsNullOrEmpty)
                    {
                        return (false, $"Phiếu tính tiền {item.OrderNo} đã được nhập thông tin HD GTGT", null);
                    }

                    sanitizedInvoices.Add(item);
                }
                var dapper = new DapperContext(connectStringDb);
                using (IDbConnection db = dapper.CreateConnDB)
                {
                    db.Open();
                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            string queryIns = @"INSERT INTO EInvoice.[dbo].[InvoiceCreated_Temp] ([SiteNo],[OrderNo],[CustomerName],[CompanyName],[TaxCode],[PhoneNumber],[Email],[Address],[IsNotVAT],[IsNotVATPartner],[CreatedUser],[CreatedDate], [Id], Passport, CCCD, DVQHNS) " +
                                               " VALUES (@SiteNo,@OrderNo,@CustomerName,@CompanyName,@TaxCode,@PhoneNumber,@Email,@Address,@IsNotVAT,@IsNotVATPartner,'WEB', getdate(), @Id, @Passport, @CCCD, @DVQHNS)";
                            db.Execute(queryIns, sanitizedInvoices, transaction, commandTimeout: 3600);
                            transaction.Commit();
                            Task.Run(() =>
                            {
                                foreach(var item in sanitizedInvoices)
                                {
                                    dbRedis.KeyDelete(GetKeyRedis(item.OrderNo, true));
                                }
                            });
                            _kibanaService.LogResponse("invoice/create", sanitizedInvoices.FirstOrDefault().SiteNo, 0, "", $"InsertInvoiceCreated {sanitizedInvoices.FirstOrDefault().OrderNo} OK");
                            return (true, message, "OK");
                        }
                        catch (SqlException ex)
                        {
                            transaction.Rollback();
                            if (ex.Number == 2627) // Error số 2627: Vi phạm PRIMARY KEY
                            {
                                string duplicateKey = ExceptionHelper.ExtractDuplicateKeyValue(ex.Message);
                                dbRedis.StringSet(GetKeyRedis(duplicateKey), JsonConvert.SerializeObject(sanitizedInvoices), DateTimeHelper.GetTimeUntilMidnight());
                                return (false, $"Phiếu tính tiền {duplicateKey} đã được nhập thông tin HD GTGT", null);
                            }

                            string messE = $"{sanitizedInvoices.FirstOrDefault().OrderNo} SqlException {JsonConvert.SerializeObject(ex)}";
                            _kibanaService.LogException($"InsertInvoiceCreated.SqlException", sanitizedInvoices.FirstOrDefault().TaxCode, 0, "", messE);
                            Task.Run(() => HttpClientService.SendMessageSMS(messE, "Error", "InsertInvoiceCreated SqlException - Rollback"));
                            return (false, ex.Message, JsonConvert.SerializeObject(ex));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException($"InsertInvoiceCreated.Exception", invoiceCreated.FirstOrDefault().TaxCode, 0, "", $"{invoiceCreated.FirstOrDefault().OrderNo} Exception: {JsonConvert.SerializeObject(ex)} ");
                return (false, ex.Message, JsonConvert.SerializeObject(ex));
            }
        }
        public (bool, string, object, HttpStatusCode) ValidateTransaction(StoreDto storeInfo, string connectStringDB, string orderNo, string appCode =  "WCM")
        {
            try
            {
                string message = "OK";
                var dapper = new DapperContext(connectStringDB);
                using (IDbConnection db = dapper.CreateConnDB)
                {
                    db.Open();
                    try
                    {
                        string queryTransHeader = "";
                        if (appCode == "WCM")
                        {
                            queryTransHeader = @"SELECT OrderNo, OrderDate, OrderTime, TransactionType, DeliveringMethod, SalesIsReturn, ReturnedOrderNo, CreatedDate, RefKey1, Ref4 AS CQT, CustomerName, MemberCardNo, ISNULL(MemberPointsEarn,0) MemberPointsEarn " +
                                                                            " FROM TransHeader (NOLOCK) WHERE OrderNo = @orderNo;";
                        }
                        else if (appCode == "PLH")
                        {
                            queryTransHeader = @"SELECT OrderNo, OrderDate, OrderTime, TransactionType, DeliveringMethod, SalesIsReturn, ReturnedOrderNo, CreatedDate, RefKey1, RefKey5 AS CQT, CustomerName, MemberCardNo " +
                                                                            " FROM TransHeader (NOLOCK) WHERE OrderNo = @orderNo;";
                        }

                        //1. check exists temp
                        var checkTransaction = db.Query<InvoiceCreated>(@"SELECT OrderNo, TaxCode, CustomerName FROM EInvoice.[dbo].[InvoiceCreated_Temp] (NOLOCK) WHERE OrderNo = @orderNo ;", new { orderNo }).FirstOrDefault();
                        if (checkTransaction != null)
                        {
                            string mes = GetMessageWarningsVATCheck(VATCheckEnum.ExistsTemp.ToString());
                            if (!string.IsNullOrEmpty(checkTransaction.TaxCode))
                            {
                                mes += $" cho MST {checkTransaction.TaxCode}";
                            }
                            else
                            {
                                mes += $" cho khách hàng {checkTransaction.CustomerName}";
                            }

                            //2. check exists Cash_OrderNoSentToEInvoice
                            var cashOrderNoSentToEInvoice1 = db.Query<Cash_OrderNoSentToEInvoice>("SELECT * FROM EInvoice.[dbo].Cash_OrderNoSentToEInvoice (NOLOCK) WHERE OrderNo = '" + orderNo + "';").FirstOrDefault();
                            if (cashOrderNoSentToEInvoice1 != null)
                            {
                                ValidateTransactionDto vatInfo = new ValidateTransactionDto
                                {
                                    OrderNo = orderNo,
                                    IsEOD = cashOrderNoSentToEInvoice1.IsEOD,
                                    Key = cashOrderNoSentToEInvoice1.Key,
                                    CompTaxCode = cashOrderNoSentToEInvoice1.CompTaxCode
                                };
                                return (false, GetMessageWarningsVATCheck(VATCheckEnum.ExistsEOD.ToString()), vatInfo, HttpStatusCode.Created);
                            }
                            return (false, mes, null, HttpStatusCode.BadRequest);
                        }

                        //3.check exists transHeader
                        var transHeader = db.Query<ValidateTransactionDto>(queryTransHeader, new {orderNo}).FirstOrDefault();
                        if (transHeader == null)
                        {
                            return (false, GetMessageWarningsVATCheck(VATCheckEnum.NotExistsTrans.ToString()), null, HttpStatusCode.BadRequest);
                        }

                        //6. check not return
                        if (transHeader != null && transHeader.SalesIsReturn > 0)
                        {
                            return (false, GetMessageWarningsVATCheck(VATCheckEnum.OrderReturned.ToString()), null, HttpStatusCode.BadRequest);
                        }

                        var transLines = db.Query<ValidateTransactionLine>(@"SELECT DISTINCT [LineNo], ItemNo, [Description] ItemName, UnitOfMeasure, Quantity, UnitPrice, DiscountAmount, VATPercent, VATAmount, LineAmountIncVAT, DivisionCode, Barcode " +
                                                                            " FROM TransLine (NOLOCK) WHERE [LineType] = 0 AND DocumentNo = @orderNo;", new { orderNo }).ToList();
                        //check PLH
                        //if (appCode == "WCM" && transLines!= null && transLines.Count > 0)
                        //{
                        //    var check = transLines.FirstOrDefault(x => x.DivisionCode.ToUpper() == "PLG");
                        //    if (check != null)
                        //    {
                        //        return (false, GetMessageWarningsVATCheck(VATCheckEnum.OrderInPLH.ToString()), null, HttpStatusCode.BadRequest);
                        //    }
                        //}
                        transHeader.TransLine = transLines.ToList();
                        transHeader.StoreNo = storeInfo.StoreNo;
                        transHeader.StoreName = storeInfo.Name;
                        transHeader.Address = storeInfo.Address;
                        transHeader.TotalAmount = transLines.Sum(x => x.LineAmountIncVAT);
                        transHeader.IsEOD = false;
                        transHeader.Key = "";
                        transHeader.CompTaxCode = "";

                        //2. check exists Cash_OrderNoSentToEInvoice
                        var cashOrderNoSentToEInvoice = db.Query<Cash_OrderNoSentToEInvoice>("SELECT * FROM EInvoice.[dbo].Cash_OrderNoSentToEInvoice (NOLOCK) WHERE OrderNo = @orderNo;", new { orderNo }).FirstOrDefault();
                        if (cashOrderNoSentToEInvoice != null)
                        {
                            transHeader.IsEOD = cashOrderNoSentToEInvoice.IsEOD;
                            transHeader.Key = cashOrderNoSentToEInvoice.Key;
                            transHeader.CompTaxCode = cashOrderNoSentToEInvoice.CompTaxCode;
                            return (false, GetMessageWarningsVATCheck(VATCheckEnum.ExistsEOD.ToString()), transHeader, HttpStatusCode.Created);
                        }

                        //4. check thơi gian hóa đơn trên CentralSalesString
                        int minuteInvoice = ConvertHelper.ConvertStringToInt(_memoryCacheService.GetPOSDataSetup("WWW_INVOICE_MINUTE") != null ? _memoryCacheService.GetPOSDataSetup("WWW_INVOICE_MINUTE").Value : null, 60);
                        if (DateTimeHelper.GetMinuteElapsed(transHeader.CreatedDate, DateTime.Now) > minuteInvoice)
                        {
                            return (false, GetMessageWarningsVATCheck(VATCheckEnum.OverTime.ToString()), null, HttpStatusCode.BadRequest);
                        }

                        //5. Check DATE
                        if (transHeader.OrderDate.Date < DateTime.Now.Date)
                        {
                            return (false, GetMessageWarningsVATCheck(VATCheckEnum.OverDate.ToString()), null, HttpStatusCode.BadRequest);
                        }

                        //set redis check
                        if (!redisManager.CheckKeyExists(GetKeyRedis(orderNo, true)))
                        {
                            redisManager.StringSet(GetKeyRedis(orderNo, true), JsonConvert.SerializeObject(transHeader), 3660);
                        }

                        //return
                        return (true, message, transHeader, HttpStatusCode.OK);
                    }
                    catch (Exception ex)
                    {
                        FileHelper.WriteExpLogs($"{orderNo} DataRawJsonRepository.ValidateTransaction.Exception", ex);
                        return (false, ex.Message, ex, HttpStatusCode.BadRequest);
                    }
                } 
            }
            catch (Exception e) 
            {
                _kibanaService.SendMessageSMS(e.Message, MessageTypeEnum.Warning.ToString(),$"Khách hàng {appCode} kiểm tra hóa đơn {orderNo} để xuất Hóa đơn GTGT", appCode: AppCodeMessageEnum.BLUEPOS_VAT_VALIDATE.ToString());
                return (false, e.Message, e, HttpStatusCode.BadRequest);
            }
        }
        public (bool, string) InsertDataRawJson(string connectStringDb, List<DataRawJsonDto> request)
        {
            string message = "OK";
            var dapper = new DapperContext(connectStringDb);
            using (IDbConnection db = dapper.CreateConnDB)
            {
                db.Open();
                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        var dataIns = new List<DataRawJsonDto>();
                        foreach (var item in request)
                        {
                            dataIns.Add(new DataRawJsonDto()
                            {
                                Id = Guid.NewGuid(),
                                StoreNo = item.StoreNo,
                                PosNo = item.PosNo,
                                Type = item.Type,
                                BatchFile = item.BatchFile,
                                FileName = item.FileName,
                                DataJson = item.DataJson,
                                CrtDate = DateTime.Now,
                                ChgDate = DateTime.Now,
                                IsRead = false,
                                OrderDate = DateTime.Now.Date,
                                OrderNo = "",
                                Srv = item.Srv
                            });
                        }
                        string queryIns = @"INSERT INTO dbo.DataRawJson
                                            (StoreNo,PosNo,BatchFile,[FileName],DataJson,CrtDate,IsRead,Srv,Id, Type, OrderNo, OrderDate, ChgDate)
                                            VALUES (@StoreNo,@PosNo,@BatchFile,@FileName,@DataJson,@CrtDate,@IsRead,@Srv,@Id, @Type,@OrderNo,@OrderDate,@ChgDate)";
                        db.Execute(queryIns, dataIns, transaction, commandTimeout: 3600);
                        transaction.Commit();
                        return (true, message);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        FileHelper.WriteExpLogs("Rollback InsertDataRawJson", ex);
                        _kibanaService.LogException("ExceptionInsertDataRawJson", request.FirstOrDefault().PosNo, 0, "", JsonConvert.SerializeObject(ex));
                        return (false, "Exception");
                    }
                }
            }
        }
        public string GetMessageWarningsVATCheck(string actionType)
        {
            var messConfig = _memoryCacheService.GetNotifyConfig(_connectStringDb).Where(x => x.AppCode == "VAT").ToList();
            if(messConfig == null)
            {
                return null;
            }

            return messConfig.FirstOrDefault(x => x.Status.ToUpper() == actionType.ToUpper()).Message ?? "Phiếu tính tiền không hợp lệ";
        }
        public List<string> GetSyncTableList()
        {
            try
            {
                var queryData = @"SELECT TableName FROM SyncTableList (NOLOCK) WHERE IsAll = 1 GROUP BY TableName ORDER BY TableName;";
                using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                {
                    db.Open();
                    return db.Query<string>(queryData).ToList();
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CentralMDRepository.GetSyncTableList.Exception", ex);
                return null;
            }
        }
    }
}