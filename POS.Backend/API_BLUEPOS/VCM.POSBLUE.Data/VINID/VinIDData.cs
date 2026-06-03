using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using VCM.POSBLUE.Data.DataBaseContext;
using VCM.POSBLUE.Model.VINID;

namespace VCM.POSBLUE.Data.VINID
{
    public class VinIDData
    {
        public void WriteLogAPI(LogAPIModel model)
        {
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 60;
                    if (model != null)
                    {
                        var itemSave = new LogAPI
                        {
                            StoreNo = model.StoreNo,
                            POSTerminal = model.POSTerminal,
                            CardNumber = model.CardNumber,
                            InvoiceNo = model.InvoiceNo,
                            ActionType = model.ActionType,
                            PlainText = model.PlainText,
                            //Signature = model.Signature,
                            RequestPOS = model.RequestPOS,
                            RequestXML = model.RequestXML,
                            ResponseXML = model.ResponseXML,
                            ResponseTotal = (int)model.ResponseTotal,
                            StatusVINID = model.StatusVINID,
                            CallOffline = model.CallOffline,
                            DateTime = model.DateTime
                        };
                        db.LogAPIs.Add(itemSave);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    LogCommonModel logModel = new LogCommonModel
                    {
                        StoreNo = model.StoreNo,
                        POSTerminal = model.POSTerminal,
                        OrderNo = model.InvoiceNo,
                        CardNumber = model.CardNumber,
                        ActionType = model.ActionType,
                        JSONModel = JsonConvert.SerializeObject(model),
                        CreatedDate = DateTime.Now,
                        MessageError = JsonConvert.SerializeObject(ex)
                    };
                    LogCommon(logModel);
                }
            }
        }
        public bool WriteLogAPIs(List<LogAPIModel> models)
        {
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 60;
                    if (models != null && models.Count > 0)
                    {
                        List<LogAPI> logAPIs = new List<LogAPI>();
                        foreach (var model in models) 
                        {
                            logAPIs.Add(new LogAPI
                            {
                                StoreNo = model.StoreNo,
                                POSTerminal = model.POSTerminal,
                                CardNumber = model.CardNumber,
                                InvoiceNo = model.InvoiceNo,
                                ActionType = model.ActionType,
                                PlainText = model.PlainText,
                                //Signature = model.Signature,
                                RequestPOS = model.RequestPOS,
                                RequestXML = model.RequestXML,
                                ResponseXML = model.ResponseXML,
                                ResponseTotal = (int)model.ResponseTotal,
                                StatusVINID = model.StatusVINID,
                                CallOffline = model.CallOffline,
                                DateTime = model.DateTime
                            });
                        }
                        logAPIs.ForEach(x=>db.LogAPIs.Add(x));
                        db.SaveChanges();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    var model = models.FirstOrDefault();
                    LogCommonModel logModel = new LogCommonModel
                    {
                        StoreNo = model.StoreNo,
                        POSTerminal = model.POSTerminal,
                        OrderNo = model.InvoiceNo,
                        CardNumber = model.CardNumber,
                        ActionType = model.ActionType,
                        JSONModel = JsonConvert.SerializeObject(model),
                        CreatedDate = DateTime.Now,
                        MessageError = JsonConvert.SerializeObject(ex)

                    };
                    LogCommon(logModel);
                    return false;
                }
            }
        }
        public List<LogAPIModel> GetListInvoiceOffline(string orderOrig)
        {
            try
            {
                using (var db = new LoyaltyEntityContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.LogAPIs.Where(d => d.InvoiceNo == orderOrig && d.ResponseXML.Contains("request_id")).Select(a => new LogAPIModel
                    {
                        StoreNo = a.StoreNo,
                        POSTerminal = a.POSTerminal,
                        InvoiceNo = a.InvoiceNo,
                        CardNumber = a.CardNumber,
                        ActionType = a.ActionType,
                        RequestPOS = a.RequestPOS,
                        RequestXML = a.RequestXML,
                        ResponseXML = a.ResponseXML

                    }).ToList();
                    return data;
                }
            }
            catch
            {
                return new List<LogAPIModel>();
            }
        }
        public bool CXWriteLogAPI(CX_LogAPIModel model)
        {
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 90;
                    db.CX_LogAPI.Add(new CX_LogAPI
                    {
                        StoreNo = model.StoreNo,
                        POSTerminal = model.POSTerminal,
                        CardNumber = model.CardNumber,
                        InvoiceNo = model.InvoiceNo,
                        ActionType = model.ActionType,
                        PlainText = model.PlainText,
                        Signature = model.Signature,
                        RequestPOS = model.RequestPOS,
                        RequestXML = model.RequestXML,
                        ResponseXML = model.ResponseXML,
                        ResponseTotal = (int)model.ResponseTotal,
                        Source = model.Source,
                        DateTime = model.DateTime
                    });
                    db.SaveChanges();
                    return true;
                }
                catch (Exception ex)
                {
                    FileHelper.WriteExpLogs("CXWriteLogAPI",ex);
                    return false;
                }
            }
        }
        public bool CXWriteLogAPIs(List<CX_LogAPIModel> models)
        {
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 60;
                    if (models != null && models.Count > 0)
                    {
                        List<CX_LogAPI> cX_LogAPIs = new List<CX_LogAPI>();
                        foreach(var model in models)
                        {
                            cX_LogAPIs.Add(new CX_LogAPI
                            {
                                StoreNo = model.StoreNo,
                                POSTerminal = model.POSTerminal,
                                CardNumber = model.CardNumber,
                                InvoiceNo = model.InvoiceNo,
                                ActionType = model.ActionType,
                                RequestPOS = model.RequestPOS,
                                RequestXML = model.RequestXML,
                                ResponseXML = model.ResponseXML,
                                ResponseTotal = (int)model.ResponseTotal,
                                Source = model.Source,
                                DateTime = model.DateTime
                            });
                        }

                        cX_LogAPIs.ForEach(x => db.CX_LogAPI.Add(x));
                        db.SaveChanges();
                    }
                    return true;
                }
                catch 
                {
                    return false;
                }
            }
        }
        public void WriteLogTransaction(LogTransactionLoyaltyModel model)
        {
            using (var db = new LoyaltyEntityContainer())
            {

                try
                {
                    db.Database.CommandTimeout = 60;
                    if (model != null)
                    {
                        var check = db.TransactionLoyalties.FirstOrDefault(d => d.OrderNo == model.OrderNo && d.TransactionType == "SALE");
                        if (check == null)
                        {
                            var itemSave = new TransactionLoyalty
                            {
                                CardNumber = model.CardNumber,
                                QRCode = model.QRCode,
                                VirtualCard = model.VirtualCard,
                                InvoiceNo = model.InvoiceNo,
                                ScanAndGoOrderNo = model.ScanAndGoOrderNo,
                                ScanAndGoReferenceNumber = model.ScanAndGoReferenceNumber,
                                AccessToken = model.AccessToken,
                                TerminalId = model.TerminalId,
                                StoreID = model.StoreID,
                                ChannelCode = model.ChannelCode,
                                DateTime = model.DateTime,
                                TransactionType = model.TransactionType,
                                TransactionName = model.TransactionName,
                                AmountOnOrderPOS = model.AmountOnOrderPOS,
                                BillAmount = model.BillAmount,
                                SpendPoints = model.SpendPoints,
                                RefundAmount = model.RefundAmount,
                                Operator = model.Operator,
                                OrderNo = model.OrderNo,
                                OrigInvoiceNo = model.OrigInvoiceNo,
                                OrigOrderNo = model.OrigOrderNo,
                                OrigBillAmount = model.OrigBillAmount,
                                Source = model.Source,
                                NettAmtOE = model.NettAmtOE,
                                GrossTransactionAmountOE = model.GrossTransactionAmountOE,
                                AwardPointOE = model.AwardPointOE,
                                AwardAmountOE = model.AwardAmountOE,
                                RedeemPointOE = model.RedeemPointOE,
                                RedeemAmountOE = model.RedeemAmountOE,
                                PointPoolIDToRedeem = model.PointPoolIDToRedeem,
                                IsOffline = model.IsOffline,
                                ErrorCode = model.ErrorCode,
                                ErrorMessage = model.ErrorMessage,
                                ResponseCode = model.ResponseCode
                            };
                            db.TransactionLoyalties.Add(itemSave);

                        }
                        else
                        {
                            check.ScanAndGoReferenceNumber = model.ScanAndGoReferenceNumber;
                            if (model.SpendPoints == 0)//Tiêu điểm
                            {
                                check.InvoiceNo = model.InvoiceNo;//ĐH ScanAndGO có tiêu điểm thì k update
                            }

                            check.OrigOrderNo = model.OrigOrderNo;
                            check.OrigInvoiceNo = model.OrigInvoiceNo;
                            check.AwardPointOE = model.AwardPointOE;
                            check.AwardAmountOE = model.AwardAmountOE;
                            check.GrossTransactionAmountOE = model.GrossTransactionAmountOE;
                            check.ScanAndGoOrderNo = model.ScanAndGoOrderNo;
                            check.SpendPoints = model.SpendPoints + check.SpendPoints;
                            check.RedeemPointOE = model.RedeemPointOE + check.RedeemPointOE;
                            check.RedeemAmountOE = check.RedeemPointOE * 10;
                        }
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    LogCommonModel logModel = new LogCommonModel
                    {
                        StoreNo = model.StoreID,
                        POSTerminal = model.TerminalId,
                        OrderNo = model.OrderNo,
                        CardNumber = model.CardNumber,
                        ActionType = "TransactionLoyalty_" + model.TransactionType,
                        JSONModel = JsonConvert.SerializeObject(model),
                        CreatedDate = DateTime.Now,
                        MessageError = JsonConvert.SerializeObject(ex)

                    };
                    LogCommon(logModel);
                }
            }
        }
        public void LogCommon(LogCommonModel model)
        {
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 60;

                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                    var ipServer = addr.LastOrDefault()?.ToString();

                    var insert = new LogCommon
                    {
                        StoreNo = model.StoreNo,
                        POSTerminal = model.POSTerminal,
                        OrderNo = model.OrderNo,
                        CardNumber = model.CardNumber,
                        ActionType = model.ActionType,
                        JSONModel = model.JSONModel,
                        MessageError = model.MessageError,
                        IPServer = ipServer,
                        CreatedDate = model.CreatedDate
                    };
                    db.LogCommons.Add(insert);
                    db.SaveChanges();

                }
                catch (Exception ex)
                {
                    WriteFileLog($"LogCommon", model.OrderNo, $"{JsonConvert.SerializeObject(ex)}", model.JSONModel);
                }
            }
        }
        public void WriteFileLog(string typeFile, string orderNo, string message, string jsonModel)
        {

            var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff");
            var content = $"{now} {typeFile}    {orderNo}       {message}       {jsonModel}";
            var pathLog = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(pathLog))
            {
                Directory.CreateDirectory(pathLog);
            }

            var filepath = pathLog + "\\" + typeFile + "_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".txt";

            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(content);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(content);
                }
            }

            if (Environment.UserInteractive)
            {
                Console.WriteLine(content);
            }
        }
        public void CXTransaction(CX_TransactionLoyaltyModel model)
        {
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 60;
                    if (model != null)
                    {
                        var check = db.CX_TransactionLoyalty.FirstOrDefault(d => d.OrderNo == model.OrderNo && d.TransactionType == "SALE");
                        if (check == null)
                        {
                            var itemSave = new CX_TransactionLoyalty
                            {
                                CardNumber = model.CardNumber,
                                QRCode = model.QRCode,
                                PhoneNumber = model.PhoneNumber,
                                InvoiceNo = model.InvoiceNo,
                                ReferenceNumberCX = model.ReferenceNumberCX,
                                TerminalId = model.TerminalId,
                                StoreID = model.StoreID,
                                ChannelCode = model.ChannelCode,
                                DateTime = model.DateTime,
                                TransactionType = model.TransactionType,
                                TransactionName = model.TransactionName,
                                AmountOnOrderPOS = model.AmountOnOrderPOS,
                                BillAmount = model.BillAmount,
                                SpendPoints = model.SpendPoints,
                                RefundAmount = model.RefundAmount,
                                Operator = model.Operator,
                                OrderNo = model.OrderNo,
                                OrigInvoiceNo = model.OrigInvoiceNo,
                                OrigOrderNo = model.OrigOrderNo,
                                OrigBillAmount = model.OrigBillAmount,
                                Source = model.Source,
                                Rate = model.Rate,
                                NettAmtCX = model.NettAmtCX,
                                GrossTransactionAmountCX = model.GrossTransactionAmountCX,
                                AwardPointCX = model.AwardPointCX,
                                AwardAmountCX = model.AwardAmountCX,
                                RedeemPointCX = model.RedeemPointCX,
                                RedeemAmountCX = model.RedeemAmountCX,
                                IsOffline = model.IsOffline,

                            };
                            db.CX_TransactionLoyalty.Add(itemSave);

                        }
                        else
                        {
                            check.Rate = model.Rate;
                            check.InvoiceNo = model.InvoiceNo;
                            check.ReferenceNumberCX = model.ReferenceNumberCX;
                            check.OrigOrderNo = model.OrigOrderNo;
                            check.OrigInvoiceNo = model.OrigInvoiceNo;
                            check.AwardPointCX = model.AwardPointCX;
                            check.AwardAmountCX = model.AwardAmountCX;
                            check.GrossTransactionAmountCX = model.GrossTransactionAmountCX;
                            check.SpendPoints = model.SpendPoints + check.SpendPoints;
                            check.RedeemPointCX = model.RedeemPointCX + check.RedeemPointCX;
                            check.RedeemAmountCX = check.RedeemPointCX * model.Rate;
                        }
                        db.SaveChanges();
                    }
                }
                catch
                {
                }
            }
        }
        public string OrigInvoiceNo(string OrigOrderNo)
        {
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 60;
                    if (!string.IsNullOrEmpty(OrigOrderNo))
                    {
                        var data = db.TransactionLoyalties.OrderByDescending(d => d.ID).FirstOrDefault(d => d.OrigOrderNo == OrigOrderNo.Trim());
                        if (data == null)
                            return OrigOrderNo + "|0|0|0|S|0";
                        //if (!string.IsNullOrEmpty(data.ScanAndGoOrderNo))
                        //    return data.ScanAndGoReferenceNumber + "|" + data.GrossTransactionAmountOE + "|" + data.BillAmount + "|" + data.AwardPointOE;

                        return data.InvoiceNo + "|" + data.GrossTransactionAmountOE + "|" + data.BillAmount + "|" + data.AwardPointOE + "|" + data.Source + "|" + data.SpendPoints;
                    }
                }
                catch
                {
                }
            }
            return OrigOrderNo + "|0|0|0|S|0";
        }
        public OrigOrderNoModel CXOrigInvoiceNo(string OrigOrderNo)
        {
            var result = new OrigOrderNoModel
            {
                InvoiceNo = OrigOrderNo,
                BillAmount = 0,
                SpendPoints = 0,
                AwardPointCX = 0
            };
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    if (!string.IsNullOrEmpty(OrigOrderNo))
                    {
                        var data = db.CX_TransactionLoyalty.OrderByDescending(d => d.ID).FirstOrDefault(d => d.OrigOrderNo == OrigOrderNo.Trim());
                        if (data != null)
                        {
                            result = new OrigOrderNoModel
                            {
                                InvoiceNo = data.InvoiceNo,
                                BillAmount = data.BillAmount,
                                SpendPoints = data.SpendPoints,
                                AwardPointCX = data.AwardPointCX
                            };
                        }
                    }
                }
                catch
                {
                }
            }
            return result;
        }
        public bool CXInsertExtral(CXExtralHeaderModel header, List<CXExtralItemBillModel> item)
        {
            var data = new StoreMappingModel();
            var date = DateTime.Now;
            using (var db = new LoyaltyEntityContainer())
            {
                db.Database.CommandTimeout = 60;
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var insertH = new CX_ExtraHeader
                        {
                            StoreNo = header.StoreNo,
                            POSCode = header.POSCode,
                            OrderNo = header.OrderNo,
                            TransactionTime = header.TransactionTime,
                            TransactionType = header.TransactionType,
                            CustomerName = header.CustomerName,
                            PhoneNUmber = header.PhoneNUmber,
                            TotalBillAmount = header.TotalBillAmount,                           
                            ReferenceNumber = header.ReferenceNumber,
                            OverQuota = header.OverQuota,
                            ExtraEarnByItems = header.ExtraEarnByItems,
                            ExtraEarnByCampaign = header.ExtraEarnByCampaign,
                            EarnByDefault = header.EarnByDefault,
                            ExtraRefundByItems = header.ExtraRefundByItems,
                            ExtraRefundByCampaign = header.ExtraRefundByCampaign,
                            OriginalPosNumber = header.OriginalPosNumber,
                            OriginalReceiptNumber = header.OriginalReceiptNumber,
                            OriginalReferenceNumber = header.OriginalReferenceNumber,
                            RefundAmount = header.RefundAmount,
                            RefundPointDefault = header.RefundPointDefault,
                            Rate = header.Rate,
                            EmployeeCode = header.EmployeeCode,
                            CompanyCode = header.CompanyCode,
                            ActionAPI = header.ActionAPI,
                            CreatedDate = DateTime.Now,

                        };
                        db.CX_ExtraHeader.Add(insertH);

                        if (item != null && item.Count > 0)
                        {
                            var insertItem = item.Select(a => new CX_ExtraItemBill
                            {
                                ReceiptNumber = a.ReceiptNumber,
                                RecordNo = a.RecordNo,
                                Barcode = a.Barcode,
                                Article = a.Article,
                                ArticleName = a.ArticleName,
                                UOM = a.UOM,
                                Quantity = a.Quantity,
                                SalePrice = a.SalePrice,
                                Amount = a.Amount,
                                DiscountAmount = a.DiscountAmount,
                                LineAmount = a.LineAmount,
                                ExtraAmountEarn = a.ExtraAmountEarn,
                                ExtraQuantityEarn = a.ExtraQuantityEarn,
                                RefundQuantity = a.RefundQuantity,
                                ExtraAmountRefund = a.ExtraAmountRefund,
                                ExtraQuantityRefund = a.ExtraQuantityRefund,
                                CreatedDate = DateTime.Now
                            });
                            db.CX_ExtraItemBill.AddRange(insertItem);
                        }
                        db.SaveChanges();
                        transaction.Commit();
                    }

                    catch 
                    {
                        transaction.Rollback();
                        return false;
                    }
                    return true;
                }
            }
        }
       
        public StoreMappingModel StoreMapping(string siteCode, string posTerminal)
        {
            var data = new StoreMappingModel();
            try
            {
                var date = DateTime.Now;
                using (var db = new LoyaltyEntityContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = (from a in db.StoreMappings
                            where a.NewStoreID == siteCode && a.NewTerminalID == posTerminal
                            select new StoreMappingModel
                            {
                                OldStoreID = a.OldStoreID,
                                OldTerminalID = a.OldTerminalID,
                                OldVinPayStoreID = a.OldVinPayStoreID,
                                OldVinPayTerminalID = a.OldVinPayTerminalID
                            }).FirstOrDefault();
                    return data;
                }
            }
            catch
            {
                return default;
            }
        }
        public VinIDOfflinePercentModel CalAwardOffline(double amount)
        {
            var date = DateTime.Now.Date;
            var data = new VinIDOfflinePercentModel();
            try
            {
                using (var db = new LoyaltyEntityContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = (from a in db.VinIDOfflinePercents
                            where a.Enabled == true && DbFunctions.TruncateTime(a.FromDate) < date && DbFunctions.TruncateTime(a.ToDate) > date
                            && a.FromAmount <= amount && a.ToAmount >= amount
                            select new VinIDOfflinePercentModel
                            {
                                FromAmount = a.FromAmount,
                                ToAmount = a.ToAmount,
                                Percent = a.Percent,
                                FromDate = a.FromDate,
                                ToDate = a.ToDate,
                                CreatedDate = a.CreatedDate
                            }).OrderByDescending(d => d.CreatedDate).FirstOrDefault();
                    return data;
                }
            }
            catch
            {
                return default;
            }
        }
        public VinIDOfflinePercentModel CalAwardOfflineCX(double amount)
        {
            var date = DateTime.Now.Date;
            var data = new VinIDOfflinePercentModel();
            try
            {
                using (var db = new CentralMDContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.CXOfflinePercents.Where(d => d.Enabled == true && DbFunctions.TruncateTime(d.FromDate) < date && DbFunctions.TruncateTime(d.ToDate) > date
                            && d.FromAmount <= amount && d.ToAmount >= amount)
                            .Select(a => new VinIDOfflinePercentModel
                            {
                                FromAmount = a.FromAmount,
                                ToAmount = a.ToAmount,
                                Percent = a.Percent,
                                FromDate = a.FromDate,
                                ToDate = a.ToDate,
                                CreatedDate = a.CreatedDate
                            }).OrderByDescending(d => d.CreatedDate).FirstOrDefault();
                    return data;
                }
            }
            catch
            {
                return default;
            }
        }
        public bool InsertExtral(ExtraHeaderModel header, List<ExtratItemBillModel> item)
        {
            var data = new StoreMappingModel();
            try
            {
                var date = DateTime.Now;
                using (var db = new LoyaltyEntityContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var insertH = new ExtraHeader
                    {
                        StoreNo = header.StoreNo,
                        POSCode = header.POSCode,
                        ReceiptNumber = header.ReceiptNumber,
                        OrderNo = header.OrderNo,
                        MerchantId = header.MerchantId,
                        TerminalId = header.TerminalId,
                        TransactionTime = header.TransactionTime,
                        TransactionType = header.TransactionType,
                        CashierCode = header.CashierCode,
                        BusinessTime = header.BusinessTime,
                        CustomerName = header.CustomerName,
                        VinidCardNumber = header.VinidCardNumber,
                        VinidCSN = header.VinidCSN,
                        TotalBillAmount = header.TotalBillAmount,
                        ReferenceNumber = header.ReferenceNumber,
                        OverQuota = header.OverQuota,
                        ExtraEarnByItems = header.ExtraEarnByItems,
                        ExtraEarnByCampaign = header.ExtraEarnByCampaign,
                        EarnByDefault = header.EarnByDefault,
                        ExtraFefundByItems = header.ExtraRefundByItems,
                        ExtraRefundByCampaign = header.ExtraRefundByCampaign,
                        OriginalPosNumber = header.OriginalPosNumber,
                        OriginalReceiptNumber = header.OriginalReceiptNumber,
                        OriginalReferenceNumber = header.OriginalReferenceNumber,
                        RefundAmount = header.RefundAmount,
                        RefundPointDefault = header.RefundPointDefault,
                        EmployeeCode = header.EmployeeCode,
                        CompanyCode = header.CompanyCode,
                        ActionAPI = header.ActionAPI,
                        CreatedDate = DateTime.Now,
                        CreatedUser = header.CashierCode

                    };
                    db.ExtraHeaders.Add(insertH);

                    if (item != null && item.Count > 0)
                    {
                        var insertItem = item.Select(a => new ExtraItemBill
                        {
                            ReceiptNumber = a.ReceiptNumber,
                            RecordNo = a.RecordNo,
                            Barcode = a.Barcode,
                            Article = a.Article,
                            ArticleName = a.ArticleName,
                            UOM = a.UOM,
                            Quantity = a.Quantity,
                            SalePrice = a.SalePrice,
                            Amount = a.Amount,
                            DiscountAmount = a.DiscountAmount,
                            LineAmount = a.LineAmount,
                            ExtraAmountEarn = a.ExtraAmountEarn,
                            ExtraQuantityEarn = a.ExtraQuantityEarn,
                            RefundQuantity = a.RefundQuantity,
                            ExtraAmountRefund = a.ExtraAmountRefund,
                            ExtraQuantityRefund = a.ExtraQuantityRefund,
                            CreatedDate = DateTime.Now,
                            CreatedUser = header.CashierCode
                        });
                        db.ExtraItemBills.AddRange(insertItem);
                    }
                    db.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                var model = new LogExtralSaleModel
                {
                    saleHeader = header,
                    saleLine = item
                };

                LogCommonModel logModel = new LogCommonModel
                {
                    StoreNo = header.StoreNo,
                    POSTerminal = header.POSCode,
                    OrderNo = header.OrderNo,
                    CardNumber = header.VinidCardNumber,
                    ActionType = "Blue_" + header.ActionAPI,
                    JSONModel = JsonConvert.SerializeObject(model),
                    CreatedDate = DateTime.Now,
                    MessageError = JsonConvert.SerializeObject(ex)

                };
                LogCommon(logModel);

                return false;
            }
        }
        public List<ExtraHeaderModel> CheckAmountOrderOrigin(string orderOrigin)
        {
            try
            {
                using (var db = new LoyaltyEntityContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.ExtraHeaders.Where(d => d.OriginalReceiptNumber == orderOrigin).Select(a => new ExtraHeaderModel
                    {
                        OriginalReceiptNumber = a.OriginalReceiptNumber,
                        RefundAmount = a.RefundAmount
                    }).ToList();
                    return data;
                }
            }
            catch
            {
                return new List<ExtraHeaderModel>();
            }
        }
        public bool UpdateOrderOffline(string storeNo, string orderOrig)
        {
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var call = db.Database.ExecuteSqlCommand("[dbo].[UpdateOrderOffline] @StoreNo, @OrderNo ",
                        new SqlParameter("@StoreNo", storeNo),
                        new SqlParameter("@OrderNo", orderOrig)
                        );
                }
                catch
                {
                }
                return true;
            }
        }
        public List<GetOrderOfflineModel> ListOrderOffline(string storeNo, string orderOrig)
        {
            var data = new List<GetOrderOfflineModel>();
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Database.SqlQuery<GetOrderOfflineModel>("[dbo].[GetOrderOffline_CallAPI] @StoreNo, @OrderNo ",
                        new SqlParameter("@StoreNo", storeNo),
                        new SqlParameter("@OrderNo", orderOrig)
                        ).ToList();
                }
                catch
                {
                    return null;
                }
            }
            return data;
        }
    }
}
