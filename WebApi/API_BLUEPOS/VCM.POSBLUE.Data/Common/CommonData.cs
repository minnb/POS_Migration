using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using TCX.API.Common.Helpers;
using VCM.POSBLUE.Data.DataBaseContext;
using VCM.POSBLUE.Data.DataBaseContext.CentralSale;
using VCM.POSBLUE.Data.DataBaseContext.SaleStaging;
using VCM.POSBLUE.Model.Common;
using VCM.POSBLUE.Model.POS.Gift;

namespace VCM.POSBLUE.Data.Common
{
    public class CommonData
    {
        public CommonData()
        {

        }
        public List<StoreSetServer> GetStoreSetServers()
        {
            return ServerIPConnection.GetStoreSetServers();
        }
        public TransCpnVchIssueModel TransactionQtyUse(string articleNo, string siteCode)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(siteCode);
                using (var db = new CentralSaleContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.TransCpnVchIssues.AsNoTracking()
                        .Where(d => d.Site == siteCode && d.Article_No == articleNo)
                        .GroupBy(a => new { a.Article_No, a.Voucher_Type, a.MaxQtyUse })
                        .Select(b => new TransCpnVchIssueModel
                        {
                            ArticleNo = b.Key.Article_No,
                            VoucherType = b.Key.Voucher_Type,
                            MaxQtyUse = b.Key.MaxQtyUse,
                            QtyUse = b.Count()
                        }).FirstOrDefault();

                    if (data == null)
                        return new TransCpnVchIssueModel { ArticleNo = articleNo, MaxQtyUse = 9999, QtyUse = 0, VoucherType = "V" };

                    return data;
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("TransactionQtyUse", ex);
                return default;
            }
        }
        public BusinessDateResponse GetBusinessDate(string siteCode)
        {
            var data = new BusinessDateResponse();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(siteCode);
                var date = DateTime.Now;

                using (var db = new CentralSaleContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = (from a in db.BussinessDateOpens
                            where a.StoreNo == siteCode
                            select new BusinessDateResponse
                            {
                                BussinessDate = a.BussinessDate,
                                CurrentDate = date

                            }).FirstOrDefault();
                    return data;
                }
            }
            catch
            {
                return default;
            }
        }
        public void InsertBussinessDateOpen(BussinessDateOpenModel model)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(model.StoreNo);

                using (var db = new CentralSaleContainer(ipServer))
                {
                    var item = new BussinessDateOpen
                    {
                        Code = model.Code,
                        StoreNo = model.StoreNo,
                        BussinessDate = model.BussinessDate,
                        CreatedUser = model.CreatedUser,
                        CreatedDate = model.CreatedDate
                    };
                    var checkStore = db.BussinessDateOpens.Any(d => d.StoreNo == model.StoreNo);
                    if (!checkStore)
                    {
                        db.BussinessDateOpens.Add(item);
                        db.SaveChanges();
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public ShiftHeaderModel GET_SHIFT_HEADER(string siteCode, string posTerminal, DateTime businessDate)
        {
            var result = new ShiftHeaderModel();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(siteCode);

                using (var db = new CentralSaleContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Database.SqlQuery<ShiftHeaderModel>("[dbo].[API_POS_CHECK_SHIFT_HEADER] @SiteCode,@PosTerminal,@BusinessDate ",
                        new SqlParameter("@SiteCode", siteCode),
                         new SqlParameter("@PosTerminal", posTerminal),
                          new SqlParameter("@BusinessDate", businessDate)
                        ).FirstOrDefault();


                    return result;
                }
            }
            catch
            {
                return result;
            }
        }
        public POSMonitorInsertResponse POSMonitorInsert(POSMonitorInsertRequest model)
        {
            var result = new POSMonitorInsertResponse();
            try
            {

                using (var db = new CentralMDContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Database.SqlQuery<POSMonitorInsertResponse>("[dbo].[POSMonitorInsert] @StoreNo,@IpAddress,@PosTerminalID,@BluePosVersion,@BluePosVersionUpdate,@BluePosDatabaseStatus,@IsOpenBluePos,@DateTimePos,@IntervalJob,@LastTimeInsertAll,@LastTimeInsertChange,@JobVersion,@ScriptVersion,@ComputerName ",
                         new SqlParameter("@StoreNo", model.StoreNo),
                         new SqlParameter("@IpAddress", model.IpAddress),
                         new SqlParameter("@PosTerminalID", model.PosTerminalID),
                         new SqlParameter("@BluePosVersion", string.IsNullOrEmpty(model.BluePosVersion) ? "" : model.BluePosVersion),
                         new SqlParameter("@BluePosVersionUpdate", model.BluePosVersionUpdate),
                         new SqlParameter("@BluePosDatabaseStatus", model.BluePosDatabaseStatus),
                         new SqlParameter("@IsOpenBluePos", model.IsOpenBluePos),
                         new SqlParameter("@DateTimePos", model.DateTimePos),
                         new SqlParameter("@IntervalJob", model.IntervalJob),
                         new SqlParameter("@LastTimeInsertAll", model.LastTimeInsertAll),
                         new SqlParameter("@LastTimeInsertChange", model.LastTimeInsertChange),
                         new SqlParameter("@JobVersion", string.IsNullOrEmpty(model.JobVersion) ? "" : model.JobVersion),
                         new SqlParameter("@ScriptVersion", string.IsNullOrEmpty(model.ScriptVersion) ? "" : model.ScriptVersion),
                          new SqlParameter("@ComputerName", model.ComputerName)
                        ).FirstOrDefault();
                    return result;
                }
            }
            catch 
            {
                return result;
            }
        }
        public PosTerminalModel CheckIPaddressPos(string IPAddress)
        {
            var data = new PosTerminalModel();
            try
            {
                using (var db = new CentralMDContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.POSTerminals.Where(d => d.IPAddress == IPAddress).Select(a => new PosTerminalModel
                    {
                        IPAddress = a.IPAddress,
                        StoreNo = a.StoreNo,
                        TerminalPOS = a.No,
                        TerminalNetworkID = a.TerminalNetworkID,
                        StyleProfile = a.StyleProfile,
                        DefaultSalesType = a.DefaultSalesType,
                        SalesTypeFilter = a.SalesTypeFilter,
                        Pkey = a.Pkey,
                        DualDisHost = a.DualDisHost,
                        BillNoseri = a.BillNoseri,
                        Placement = a.Placement,
                        StatementMethod = a.StatementMethod??0,
                        TerminalStatement = a.TerminalStatement,
                        TerminalConnection = a.TerminalConnection ?? 0,
                        PrintReceiptLogo = a.PrintReceiptLogo,
                        CustomerDisplayText1 = a.CustomerDisplayText1,
                        CustomerDisplayText2 = a.CustomerDisplayText2,
                        PrintReceiptBCType = a.PrintReceiptBCType ?? 0,
                        InterfaceProfile = a.InterfaceProfile,
                    }).FirstOrDefault();
                    return data;
                }
            }
            catch
            {
                return default;
            }
        }

        public List<POSDataSetupModel> GetDataSetup()
        {
            var data = new List<POSDataSetupModel>();
            try
            {
                var date = DateTime.Now;
                using (var db = new CentralMDContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = (from a in db.POSDataSetups
                            select new POSDataSetupModel
                            {
                                Code = a.Code,
                                Value = a.Value
                            }).ToList();
                    return data;
                }
            }
            catch
            {
                return default;
            }
        }
        public List<POSVersionModel> GetPOSVersion()
        {
            var data = new List<POSVersionModel>();
            try
            {
                var date = DateTime.Now;
                using (var db = new CentralMDContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = (from a in db.POSVersions
                            select new POSVersionModel
                            {
                                LastVersion = a.LastVersion,
                                CurVersion = a.CurVersion,
                                Source = a.Source,
                                IsUpdate = a.IsUpdate,
                                Counter = a.Counter,
                                Folder = a.Folder,
                                Pkey = a.Pkey,
                                UpdateTime = a.UpdateTime
                            }).ToList();
                    return data;
                }
            }
            catch(Exception ex)
            {
                FileHelper.WriteExpLogs("GetPOSVersion.Exception", ex);
                return default;
            }
        }

        public bool CheckSaleReturn(string orderNo)
        {
            try
            {
                var siteCode = orderNo.Substring(0, 4);
                var ipServer = ServerIPConnection.GetIPServerByStore(siteCode);

                using (var db = new CentralSaleContainer(ipServer))
                {
                    var checkSaleIsReturn = db.TransHeaders.Any(d => d.OrderNo == orderNo && d.SalesIsReturn == 1);
                    return checkSaleIsReturn;
                }
            }
            catch
            {
                return false;
            }
        }

        public List<SaleTableModel> GetOrderInfo(string orderNo)
        {
            var result = new List<SaleTableModel>();
            try
            {
                var siteCode = orderNo.Substring(0, 4);
                var ipServer = ServerIPConnection.GetIPServerByStore(siteCode);

                using (var db = new CentralSaleContainer(ipServer))
                {
                    DataSet ds = new DataSet();
                    using (var con = new SqlConnection(db.Database.Connection.ConnectionString))
                    using (var cmd = new SqlCommand("API_SALE_INFO_ORDERNO", con))
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        cmd.Parameters.AddWithValue("@OrderNo", orderNo);
                        cmd.CommandType = CommandType.StoredProcedure;
                        da.Fill(ds);
                        var countTable = ds.Tables.Count;
                        for (int i = 0; i < countTable; i++)
                        {
                            var tableModel = new SaleTableModel();
                            var dataTable = ds.Tables[i];
                            if (dataTable.Rows.Count > 0)
                            {
                               
                                tableModel.TableName = dataTable.Rows[0]["TableName"].ToString();
                                if (dataTable.Columns["TableName"].ColumnName == "TableName")
                                {
                                    dataTable.Columns.Remove("TableName");
                                }
                                if (dataTable.Columns["timestamp"].ColumnName == "timestamp")
                                {
                                    dataTable.Columns.Remove("timestamp");
                                }

                                tableModel.TableData = JsonConvert.SerializeObject(dataTable);

                                result.Add(tableModel);
                            }
                        }
                        return result;
                    }
                }
            }
            catch(Exception  ex)
            {
                FileHelper.WriteLogs($"GetOrderInfo.Exception: {JsonConvert.SerializeObject(ex)}");
                return result;
            }
        }
        public bool InsertSignalStore(SignalStoreModel model)
        {
            try
            {
                using (var db = new CentralMDContainer())
                {
                    var item = new SignalStore
                    {
                        StoreNO = model.StoreNO,
                        POSTerminalID = model.POSTerminalID,
                        BusinessDate = model.BusinessDate,
                        CreatedDate = model.CreatedDate
                    };
                    db.SignalStores.Add(item);
                    db.SaveChanges();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        public List<POSDocumentNoModel> ListPOSDocumentNo(string storeNo, string posTerminal)
        {
            var data = new List<POSDocumentNoModel>();
            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
            using (var db = new CentralSaleContainer(ipServer))
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    //data = db.Database.SqlQuery<POSDocumentNoModel>("[dbo].[API_GET_LAST_DOCUMENTNO] @StoreNo, @PosTerminal ",
                    //    new SqlParameter("@StoreNo", storeNo),
                    //    new SqlParameter("@PosTerminal", posTerminal)
                    //    ).ToList();

                    var orderLast = db.TransHeaders.AsNoTracking()
                        .Where(d => d.StoreNo == storeNo && d.POSTerminalNo == posTerminal)
                        .OrderByDescending(d => d.OrderTime)
                        .Select(a => new POSDocumentNoModel
                        {
                            StoreNo = a.StoreNo,
                            POSTerminal = a.POSTerminalNo,
                            LastNumber = a.OrderNo,
                            DocumentType = "ORDER",
                            LastDateTime = a.OrderTime
                        }).FirstOrDefault();

                    var voucherLast = db.TransCpnVchIssues.AsNoTracking()
                        .Where(d => d.Voucher_Type == "V" && d.Site == storeNo && d.Voucher_Type != "R"
                         && d.Site + d.POSNo.Substring(1, 2) == posTerminal)
                        .OrderByDescending(d => d.CreatedDate)
                        .Select(a => new POSDocumentNoModel
                        {
                            StoreNo = a.Site,
                            POSTerminal = posTerminal,
                            LastNumber = a.SerialNo,
                            DocumentType = "VOUCHER",
                            LastDateTime = a.CreatedDate
                        }).FirstOrDefault();
                    if (orderLast != null)
                    {
                        data.Add(orderLast);
                    }
                    if (voucherLast != null)
                    {
                        data.Add(voucherLast);
                    }
                }
                catch 
                {
                }
                return data;
            }
        }

        public bool CheckCouponLine(string itemNo, string barCode)
        {
            var result = true;
            using (var db = new CentralMDContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.CpnVchBOMLines.Any(d => d.ItemNo == itemNo && d.Barcode == barCode);
                }
                catch
                {
                    result = false;
                }
            }
            return result;
        }

        public ResponseUpdateTransModel InsertLineOrig_Update_OrderInfo(UpdateOrderInfoModel model)
        {
            var result = new ResponseUpdateTransModel();
            try
            {
                var listModel = model.ListLine;
                var listBluePoint = model.ListLineBluePoint;
                var dateTime = DateTime.Now;
                var orderNo = model.Header.OrderNo;
                var siteCode = orderNo.Substring(0, 4);
                var ipServer = ServerIPConnection.GetIPServerByStore(siteCode);

                using (var db = new DataBaseContext.SaleStaging.CentralSaleStagingContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var listInsert = new List<DataBaseContext.SaleStaging.TransLineOrig>(listModel.Select(a => new DataBaseContext.SaleStaging.TransLineOrig
                    {
                        DocumentNo = a.DocumentNo,
                        LineNo = a.LineNo,
                        QtyDiscountReturned = a.QtyDiscountReturned,
                        ReturnedQuantity = a.ReturnedQuantity,
                        DiscountQuantity = a.DiscountQuantity

                    }));
                    if (listBluePoint != null && listBluePoint.Count > 0)
                    {
                        var listInsertBlueOrig = new List<DataBaseContext.SaleStaging.TransBluePointOrig>(listBluePoint.Select(a => new DataBaseContext.SaleStaging.TransBluePointOrig
                        {
                            OrderNo = a.OrderNo,
                            LineNo = a.LineNo,
                            ExtraQuantityNotEarnReturned = a.ExtraQuantityNotEarnReturned

                        }));
                        db.TransBluePointOrigs.AddRange(listInsertBlueOrig);
                    }

                    db.TransLineOrigs.AddRange(listInsert);
                    db.TransHeaderOrigs.Add(new DataBaseContext.SaleStaging.TransHeaderOrig 
                                                {   OrderNo = model.Header.OrderNo, 
                                                    MemberPointsRedeemReturned = model.Header.MemberPointsRedeemReturned, 
                                                    OrderTime = dateTime,
                                                    PaymentAtPOSAmount = model.Header.WinPayAmountReturned,
                                                });

                    if (model.ListLineDiscountEntry != null && model.ListLineDiscountEntry.Count > 0)
                    {
                        foreach (var item in model.ListLineDiscountEntry)
                        {
                            var checkDiscount = db.TransDiscountEntries.FirstOrDefault(x => x.OrderNo == item.OrderNo
                            && x.LineNo == item.LineNo);

                            if (checkDiscount != null)
                            {
                                checkDiscount.Ref1 = "" + item.ReturnedQuantity;
                            }
                        }
                    }
                    if (model.ListTransPaymentEntry != null && model.ListTransPaymentEntry.Count > 0)
                    {
                        var listTransPayment = db.TransPaymentEntries.Where(d => d.OrderNo == orderNo).ToList();
                        if (listTransPayment.Count > 0)
                        {
                            foreach (var item in model.ListTransPaymentEntry)
                            {
                                var checkLine = listTransPayment.FirstOrDefault(d => d.LineNo == item.LineNo);
                                if (checkLine != null)
                                {
                                    checkLine.TransactionNo = item.TransactionNo;
                                }
                            }
                        }
                    }
                    db.SaveChanges();
                    var callStore = db.Database.SqlQuery<int>("[dbo].[InsertTransOrig] @OrderNo,@StoreNo ",
                            new SqlParameter("@OrderNo", listModel.FirstOrDefault().DocumentNo),
                            new SqlParameter("@StoreNo", "")
                           ).FirstOrDefault();

                    result = new ResponseUpdateTransModel { Status = true, Message = $"Cập nhật thành công {callStore} LineNo" };
                }
            }
            catch (Exception ex)
            {
                result = new ResponseUpdateTransModel { Status = false, Message = ex.Message };
            }
            return result;
        }

        public HealthCheckModel HealthCheck()
        {
            var result = new HealthCheckModel { DbConnection = true };
            try
            {
                using (var db = new CentralMDContainer())
                {
                    var checkSaleIsReturn = db.POSDataSetups.Take(1).FirstOrDefault();
                }
            }
            catch
            {
                result = new HealthCheckModel { DbConnection = false };
            }
            return result;
        }
        public InsuranceModel GetInsurance(string ReceiptNo, string POSNo, string StaffCode)
        {
            var result = new InsuranceModel();
            try
            {

                using (var db = new DCMStarContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Database.SqlQuery<InsuranceModel>("[dbo].[BAOHIEM5] @ReceiptNo, @POSNo, @Staff ",
                        new SqlParameter("@ReceiptNo", ReceiptNo),
                         new SqlParameter("@POSNo", POSNo),
                          new SqlParameter("@Staff", StaffCode)
                        ).FirstOrDefault();


                    return result;
                }
            }
            catch
            {
                return result;
            }
        }
        public bool UpdatePOSEOD(POSEOD_APIModel model)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(model.StoreNo);
                using (var db = new CentralSaleContainer(ipServer))
                {
                    db.Database.CommandTimeout = 120;
                    var check = db.POSEOD_API.Where(d => d.StoreNo == model.StoreNo && d.POSTerminal == model.POSTerminal && DbFunctions.TruncateTime(d.BussinessDate) == model.BussinessDate).FirstOrDefault();
                    if (check != null)
                    {
                        check.TotalSale = model.TotalSale;
                    }
                    else
                    {
                        var createdDate = DateTime.Now;
                        var item = new POSEOD_API
                        {
                            StoreNo = model.StoreNo,
                            POSTerminal = model.POSTerminal,
                            BussinessDate = model.BussinessDate,
                            TotalSale = model.TotalSale,
                            CreatedDate = createdDate
                        };
                        db.POSEOD_API.Add(item);
                    }
                    db.SaveChanges();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        public CheckTotalBillResponse CheckTotalBill(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal)
        {
            var result = new CheckTotalBillResponse();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new DataBaseContext.SaleStaging.CentralSaleStagingContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var totalCentral = db.TransHeaderStagings.Count(d => d.StoreNo == storeNo && d.POSTerminalNo == posTerminal && DbFunctions.TruncateTime(d.OrderDate) == bussinessDate);
                    if (totalCentral == posTotal)
                        result = new CheckTotalBillResponse { Status = true, Description = "", TotalBillPOS = posTotal, TotalBillCentral = totalCentral };
                    else
                        result = new CheckTotalBillResponse { Status = false, Description = "Pos và central lệch sale", TotalBillPOS = posTotal, TotalBillCentral = totalCentral };
                }
            }
            catch (Exception ex)
            {
                result = new CheckTotalBillResponse { Status = false, Description = ex.Message, TotalBillPOS = posTotal, TotalBillCentral = 0 };
            }
            return result;
        }

        public bool IsCheckAwardSpend(string storeNo)
        {
            var result = true;
            try
            {
                using (var db = new CentralMDContainer())
                {
                    db.Database.CommandTimeout = 60;
                    var check = db.Stores.Any(d => d.No == storeNo && d.BusinessAreaNo == "HF");
                    if (check)
                    {
                        result = false;
                    }
                }
            }
            catch
            {

            }
            return result;
        }

        public List<Store> GetStoreAll()
        {
            try
            {
                using (var db = new CentralMDContainer())
                {
                    db.Database.CommandTimeout = 60;
                   return db.Stores.ToList();
                }
            }
            catch
            {
                return null;
            }
        }

        public StoreNoMappingModel MappingStoreWinlife(string storeBlue)
        {
            try
            {
                using (var db = new CentralMDContainer())
                {
                    var data = db.StoreNoMappings.Where(d => d.StoreNoBlue == storeBlue).Select(a => new StoreNoMappingModel
                    {
                        StoreNoBlue = a.StoreNoBlue,
                        StoreNoPLH = a.StoreNoPLH,
                        StoreNoWPH = a.StoreNoWPH

                    }).FirstOrDefault();
                    return data;
                }
            }
            catch
            {
                return null;
            }
        }

        public Tuple<bool, string> KiosInsertSale(KiosInsertSaleRequest model)
        {
            var result = new Tuple<bool, string>(false, $"");
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(model.StoreNo);
                using (var db = new KIOSContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    try
                    {
                        var data = db.Database.SqlQuery<int>("[dbo].[Sale_InsertDataByOrderV2] @Type, @Json ",
                             new SqlParameter("@Type", model.Type),
                             new SqlParameter("@Json", model.Json)

                             ).FirstOrDefault();

                        if (data == 1)
                        {
                            result = new Tuple<bool, string>(true, "OK");
                        }
                        else
                        {
                            result = new Tuple<bool, string>(false, "Thất bại");
                        }
                    }
                    catch (Exception ex)
                    {
                        result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));

                        return result;
                    }
                    return result;

                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                return result;
            }
        }
        public Tuple<bool, string, KiosCheckOrderResponse> KiosCheckOrder(string storeNo, string posNo, string orderNo)
        {
            var data = new KiosCheckOrderResponse();
            var result = new Tuple<bool, string, KiosCheckOrderResponse>(false, $"", data);
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new KIOSContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    try
                    {
                        data = db.Database.SqlQuery<KiosCheckOrderResponse>("[dbo].[SP_API_KIOS_ORDER_CHECK] @OrderNo ",
                            new SqlParameter("@OrderNo", orderNo)
                            ).FirstOrDefault();

                        if (data != null)
                        {
                            result = new Tuple<bool, string, KiosCheckOrderResponse>(true, "OK", data);
                        }
                        else
                        {
                            result = new Tuple<bool, string, KiosCheckOrderResponse>(false, $"Không tìm thấy đơn hàng {orderNo} trong hệ thống", data);
                        }
                    }
                    catch (Exception ex)
                    {
                        result = new Tuple<bool, string, KiosCheckOrderResponse>(false, JsonConvert.SerializeObject(ex), data);

                        return result;
                    }
                    return result;

                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string, KiosCheckOrderResponse>(false, JsonConvert.SerializeObject(ex), data);
                return result;
            }
        }

        public void LogSaleKios(LogSaleKiosModel model)
        {
            var ipServer = ServerIPConnection.GetIPServerByStore(model.StoreNo);
            using (var db = new KIOSContainer(ipServer))
            {
                try
                {
                    db.Database.CommandTimeout = 60;
                    if (model != null)
                    {
                        var itemSave = new LogSaleKio
                        {
                            StoreNo = model.StoreNo,
                            PosID = model.PosID,
                            RequestPOS = model.RequestPOS,
                            ResponseAPI = model.ResponseAPI,
                            RequestAPI = model.RequestAPI,
                            ResponsePOS = model.ResponsePOS,
                            CreatedDate = model.CreatedDate
                        };
                        db.LogSaleKios.Add(itemSave);
                        db.SaveChanges();
                    }
                }
                catch 
                {
                    throw;
                }
            }
        }

        public Tuple<bool, string, string> CheckWinMember(string storeNo)
        {
            var result = new Tuple<bool, string, string>(true, "CVL", $"");
            try
            {
                using (var db = new CentralMDContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    try
                    {
                        var checkStore = db.Stores.FirstOrDefault(d => d.No == storeNo);

                        if (checkStore == null)
                        {
                            result = new Tuple<bool, string, string>(false, "CVL", $"Không tìm thấy CH/ST {storeNo} trên hệ thống");
                            return result;
                        }
                        else
                        {
                            if (checkStore.InterfaceProfile.ToUpper() == "WINMEMBER")
                            {
                                result = new Tuple<bool, string, string>(true, "WIN", "OK");
                                return result;
                            }
                            else
                            {
                                result = new Tuple<bool, string, string>(true, "CVL", "OK");
                                return result;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result = new Tuple<bool, string, string>(false, "CVL", JsonConvert.SerializeObject(ex));

                        return result;
                    }

                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string, string>(false, "CVL", JsonConvert.SerializeObject(ex));
                return result;
            }
        }



    }
}
