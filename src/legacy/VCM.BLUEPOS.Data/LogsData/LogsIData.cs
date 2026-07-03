using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.Loyalty;
using VCM.BLUEPOS.Common.Helpers;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.LogsData;
using VCM.BLUEPOS.Model.LogsData.LoyaltyCXLogAPIModel;
using VCM.BLUEPOS.Data.EF.PLHLog;
using System.Data.Entity;
using Newtonsoft.Json;

namespace VCM.BLUEPOS.Data.LogsData
{
    public interface ILogsIData
    {
        List<APIErrorMappingResponseModel> GetAPIErrorMappingList(string eRRCode, out int totalRecord, int skip = 0, int take = 100);
        List<PublishLogResponseModel> GetPubLishLog(DateTime fromDate, DateTime toDate, string storeNo, string textSearch, out int totalRecord, int skip = 0, int take = 100);
        List<InvoiceErrorsResponseModel> GetInvoiceErrorsLog(DateTime fromDate, DateTime toDate, int exPort, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportInvoiceErrorsResponseModel> ExportExcelInvoiceErrorsLog(DateTime fromDate, DateTime toDate, int export);
        List<LoyaltyCXLogAPIResponseModel> GetLoyaltyCXLogAPI(string svProd, DateTime fromDate, DateTime toDate, string actionType, string storeNoOrPos, string keyWord, int pageSize, int pageNumber);
        List<DetailLoyaltyCXLogAPIRequestPOSModel> GetDetailCXLogAPIRequestPOS(int id);
        List<DetailLoyaltyCXLogAPIRequestXMLModel> GetDetailCXLogAPIRequestXML(int id);
        List<DetailLoyaltyCXLogAPIResponseXMLModel> GetDetailCXLogAPIResponseXML(int id);
        List<ExportExcelLoyaltyCXLogAPIModel> ExportExcelCXLogAPI(DateTime fromDate, DateTime toDate, string storeNo, string posID, string cardNumber, string invoiceNo);
        List<LogAPIVoucherModel> LoadLogAPIVoucher(string svProd, string partner, DateTime fromDate, DateTime toDate, string actionType, string storeNoOrPos, string keyWord, int pageSize, int pageNumber);
        List<LogMisSaleModel> LogMissOrderSale(string storeNo, DateTime fromDate, DateTime toDate, out int totalRecord, int skip, int take);
        List<LogCloseSalePOSModel> LogCloseSalePOSByBussinessDate(string IPServer, string storeNo, string posTerminal, string statusClose, DateTime bussinessDate, out int totalRecord, int skip, int take);
        Tuple<bool, string> SyncBussinessDateToCentral(string IPServer, List<SyncBussinessDateModel> listModel);
        List<LogFileSalePOSModel> LogFileSalePOS(string IPServer, string storeNo, string posTerminal, string file, DateTime fromDate, DateTime toDate, out int totalRecord, int skip, int take);
        List<SyncChangeLogStoreModel> SyncChangeLogStoreList(string storeNo, string tableName, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel UpdateLastCounter(UpdateSyncChangeLogStoreModel req);


    }

    public class LogsIData : ILogsIData
    {
        public List<APIErrorMappingResponseModel> GetAPIErrorMappingList(string eRRCode, out int totalRecord, int skip = 0, int take = 100)
        {
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<APIErrorMappingResponseModel>("[dbo].[PLG_GetAPIErrorMappingList] @ERRCode",
                        new SqlParameter("@ERRCode", eRRCode ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<APIErrorMappingResponseModel>();
                    }

                    totalRecord = data.Count();
                    var result = data.Skip(skip).Take(take).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<APIErrorMappingResponseModel>();
            }
        }

        public List<PublishLogResponseModel> GetPubLishLog(DateTime fromDate, DateTime toDate, string storeNo, string textSearch, out int totalRecord, int skip = 0, int take = 100)
        {
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<PublishLogResponseModel>("[dbo].[PLG_GetPublishLogList] @FromDate, @ToDate, @StoreNo, @TextSearch",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<PublishLogResponseModel>();
                    }
                    totalRecord = data.Count();
                    var result = data.Skip(skip).Take(take).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<PublishLogResponseModel>();
            }
        }

        public List<InvoiceErrorsResponseModel> GetInvoiceErrorsLog(DateTime fromDate, DateTime toDate, int exPort, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<InvoiceErrorsResponseModel>("[dbo].[PLG_GetInvoiceErrorsList] @FromDate, @ToDate, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@Export", exPort),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<InvoiceErrorsResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<InvoiceErrorsResponseModel>();
            }
        }

        public List<ExportInvoiceErrorsResponseModel> ExportExcelInvoiceErrorsLog(DateTime fromDate, DateTime toDate, int export)
        {
            try
            {
                var data = new List<ExportInvoiceErrorsResponseModel>();
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Database.SqlQuery<ExportInvoiceErrorsResponseModel>("[dbo].[PLG_GetInvoiceErrorsList] @FromDate, @ToDate, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@Export", export),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportInvoiceErrorsResponseModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ExportInvoiceErrorsResponseModel>));
            }
        }

        public List<LoyaltyCXLogAPIResponseModel> GetLoyaltyCXLogAPI(string svProd, DateTime fromDate, DateTime toDate, string actionType, string storeNoOrPos, string keyWord, int pageSize, int pageNumber)
        {
            try
            {
                if (svProd == "srvUAT")
                {
                    using (var db = new PLHLogContainer())
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var data = db.Database.SqlQuery<LoyaltyCXLogAPIResponseModel>("[dbo].[SP_PL_LOAD_LOGAPI] @FromDate, @ToDate,@ActionType, @StoreNoOrPOS, @Keyword,@PageSize,@PageNumber",
                            new SqlParameter("@FromDate", fromDate),
                            new SqlParameter("@ToDate", toDate),
                            new SqlParameter("@ActionType", actionType ?? string.Empty),
                            new SqlParameter("@StoreNoOrPOS", storeNoOrPos ?? string.Empty),
                            new SqlParameter("@Keyword", keyWord ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)
                            ).ToList();
                        return data;
                    }
                }
                else
                {
                    using (var db = new PLHLogProdContainer())
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var data = db.Database.SqlQuery<LoyaltyCXLogAPIResponseModel>("[dbo].[SP_PL_LOAD_LOGAPI] @FromDate, @ToDate,@ActionType, @StoreNoOrPOS, @Keyword,@PageSize,@PageNumber",
                            new SqlParameter("@FromDate", fromDate),
                            new SqlParameter("@ToDate", toDate),
                            new SqlParameter("@ActionType", actionType ?? string.Empty),
                            new SqlParameter("@StoreNoOrPOS", storeNoOrPos ?? string.Empty),
                            new SqlParameter("@Keyword", keyWord ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)
                            ).ToList();
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                return new List<LoyaltyCXLogAPIResponseModel>();
            }
        }

        public List<DetailLoyaltyCXLogAPIRequestPOSModel> GetDetailCXLogAPIRequestPOS(int id)
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.CX_LogAPI
                                where a.ID == id
                                select new DetailLoyaltyCXLogAPIRequestPOSModel
                                {
                                    ID = a.ID,
                                    CreateDate = a.DateTime,
                                    StoreNo = a.StoreNo,
                                    POSTerminal = a.POSTerminal,
                                    CardNumber = a.CardNumber,
                                    InvoiceNo = a.InvoiceNo,
                                    ActionType = a.ActionType,
                                    PlainText = a.PlainText,
                                    Signature = a.Signature,
                                    RequestPOS = a.RequestPOS,
                                    RequestXML = a.RequestXML,
                                    ResponseXML = a.ResponseXML,
                                    ResponseTotal = a.ResponseTotal,
                                    Source = a.Source
                                }).ToList();

                    if (data == null)
                    {
                        return new List<DetailLoyaltyCXLogAPIRequestPOSModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<DetailLoyaltyCXLogAPIRequestPOSModel>();
            }
        }

        public List<DetailLoyaltyCXLogAPIRequestXMLModel> GetDetailCXLogAPIRequestXML(int id)
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.CX_LogAPI
                                where a.ID == id
                                select new DetailLoyaltyCXLogAPIRequestXMLModel
                                {
                                    ID = a.ID,
                                    CreateDate = a.DateTime,
                                    StoreNo = a.StoreNo,
                                    POSTerminal = a.POSTerminal,
                                    CardNumber = a.CardNumber,
                                    InvoiceNo = a.InvoiceNo,
                                    ActionType = a.ActionType,
                                    PlainText = a.PlainText,
                                    Signature = a.Signature,
                                    RequestPOS = a.RequestPOS,
                                    RequestXML = a.RequestXML,
                                    ResponseXML = a.ResponseXML
                                }).ToList();

                    if (data == null)
                    {
                        return new List<DetailLoyaltyCXLogAPIRequestXMLModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<DetailLoyaltyCXLogAPIRequestXMLModel>();
            }
        }

        public List<DetailLoyaltyCXLogAPIResponseXMLModel> GetDetailCXLogAPIResponseXML(int id)
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.CX_LogAPI
                                where a.ID == id
                                select new DetailLoyaltyCXLogAPIResponseXMLModel
                                {
                                    ID = a.ID,
                                    CreateDate = a.DateTime,
                                    StoreNo = a.StoreNo,
                                    POSTerminal = a.POSTerminal,
                                    CardNumber = a.CardNumber,
                                    InvoiceNo = a.InvoiceNo,
                                    ActionType = a.ActionType,
                                    PlainText = a.PlainText,
                                    Signature = a.Signature,
                                    RequestPOS = a.RequestPOS,
                                    RequestXML = a.RequestXML,
                                    ResponseXML = a.ResponseXML
                                }).ToList();

                    if (data == null)
                    {
                        return new List<DetailLoyaltyCXLogAPIResponseXMLModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<DetailLoyaltyCXLogAPIResponseXMLModel>();
            }
        }

        public List<ExportExcelLoyaltyCXLogAPIModel> ExportExcelCXLogAPI(DateTime fromDate, DateTime toDate, string storeNo, string posID, string cardNumber, string invoiceNo)
        {
            try
            {
                var data = new List<ExportExcelLoyaltyCXLogAPIModel>();
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Database.SqlQuery<ExportExcelLoyaltyCXLogAPIModel>("[dbo].[Get_Loyalty_CX_LogAPI] @FromDate, @ToDate, @StoreNo, @PosID, @CardNumber, @InvoiceNo, @Export",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosID", posID ?? string.Empty),
                        new SqlParameter("@CardNumber", cardNumber ?? string.Empty),
                        new SqlParameter("@InvoiceNo", invoiceNo ?? string.Empty),
                        new SqlParameter("@Export", 2)).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ExportExcelLoyaltyCXLogAPIModel>));
            }
        }

        public List<LogAPIVoucherModel> LoadLogAPIVoucher(string svProd, string partner, DateTime fromDate, DateTime toDate, string actionType, string storeNoOrPos, string keyWord, int pageSize, int pageNumber)
        {
            try
            {
                if (svProd == "srvUAT")
                {
                    using (var db = new PLHLogContainer())
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var data = db.Database.SqlQuery<LogAPIVoucherModel>("[dbo].[SP_PL_LOAD_LOG_API_VOUCHER] @FromDate, @ToDate,@Partner,@ActionType, @StoreNoOrPOS, @Keyword,@PageSize,@PageNumber",
                            new SqlParameter("@FromDate", fromDate),
                            new SqlParameter("@ToDate", toDate),
                            new SqlParameter("@Partner", partner ?? string.Empty),
                            new SqlParameter("@ActionType", actionType ?? string.Empty),
                            new SqlParameter("@StoreNoOrPOS", storeNoOrPos ?? string.Empty),
                            new SqlParameter("@Keyword", keyWord ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)
                            ).ToList();
                        return data;
                    }
                }
                else
                {
                    using (var db = new PLHLogProdContainer())
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var data = db.Database.SqlQuery<LogAPIVoucherModel>("[dbo].[SP_PL_LOAD_LOG_API_VOUCHER] @FromDate, @ToDate,@Partner,@ActionType, @StoreNoOrPOS, @Keyword,@PageSize,@PageNumber",
                            new SqlParameter("@FromDate", fromDate),
                            new SqlParameter("@ToDate", toDate),
                            new SqlParameter("@Partner", partner ?? string.Empty),
                            new SqlParameter("@ActionType", actionType ?? string.Empty),
                            new SqlParameter("@StoreNoOrPOS", storeNoOrPos ?? string.Empty),
                            new SqlParameter("@Keyword", keyWord ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)
                            ).ToList();
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                return new List<LogAPIVoucherModel>();
            }
        }

        public List<LogMisSaleModel> LogMissOrderSale(string storeNo, DateTime fromDate, DateTime toDate, out int totalRecord, int skip, int take)
        {
            var result = new List<LogMisSaleModel>();
            try
            {
                var dateTime = DateTime.Now.Date;
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.LogMisSales.Where(d => DbFunctions.TruncateTime(d.BussinessDate) >= DbFunctions.TruncateTime(fromDate) && DbFunctions.TruncateTime(d.BussinessDate) <= DbFunctions.TruncateTime(toDate));

                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        data = data.Where(d => d.StoreNo == storeNo);
                    }
                    result = data.Select(a => new LogMisSaleModel
                    {
                        StoreNo = a.StoreNo,
                        BussinessDate = a.BussinessDate,
                        TotalSalePOS = a.TotalSalePOS,
                        TotalSaleServer = a.TotalSaleServer,
                        CreatedDate = a.CreatedDate,
                        CreatedUser = a.CreatedUser
                    }).ToList();

                    totalRecord = result.Count();
                    result = result.OrderByDescending(d => d.BussinessDate).Skip(skip).Take(take).ToList();
                }
            }
            catch (Exception Ex)
            {
                totalRecord = 0;
            }
            return result;
        }

        public List<LogCloseSalePOSModel> LogCloseSalePOSByBussinessDate(string IPServer, string storeNo, string posTerminal, string statusClose, DateTime bussinessDate, out int totalRecord, int skip, int take)
        {
            var result = new List<LogCloseSalePOSModel>();
            try
            {
                var dateTime = DateTime.Now.Date;
                using (var db = new CentralSalesStagingContainer(IPServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.POSBussinessDate_Log.Where(d => DbFunctions.TruncateTime(d.BussinessDate) == DbFunctions.TruncateTime(bussinessDate));

                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        data = data.Where(d => d.StoreNo == storeNo);
                    }
                    if (!string.IsNullOrEmpty(posTerminal))
                    {
                        data = data.Where(d => d.PosTerminal == posTerminal);
                    }
                    if (!string.IsNullOrEmpty(statusClose))
                    {
                        var status = statusClose == "1" ? true : false;
                        data = data.Where(d => d.IsClosed == status);
                    }
                    result = data.Select(a => new LogCloseSalePOSModel
                    {
                        StoreNo = a.StoreNo,
                        PosTerminal = a.PosTerminal,
                        BussinessDate = a.BussinessDate,
                        IsClosed = a.IsClosed,
                        IsOpened = a.IsOpened,
                        OpenDate = a.OpenDate,
                        CloseDate = a.CloseDate,
                        CreatedDate = a.CreatedDate,
                        CreatedUser = a.CreatedUser,
                        UpdatedUser = a.UpdatedUser,
                        UpdatedDate = a.UpdatedDate,
                        LastUpdated = a.LastUpdated
                    }).ToList();

                    totalRecord = result.Count();
                    result = result.OrderByDescending(d => d.LastUpdated).Skip(skip).Take(take).ToList();
                }
            }
            catch (Exception Ex)
            {
                totalRecord = 0;
            }
            return result;
        }

        public Tuple<bool, string> SyncBussinessDateToCentral(string IPServer, List<SyncBussinessDateModel> listModel)
        {
            var result = new Tuple<bool, string>(false, "");
            try
            {
                var dateTime = DateTime.Now.Date;
                using (var db = new CentralSalesContainer(IPServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    int countStatus = 0;
                    foreach (var item in listModel)
                    {
                        var checkItem = db.BussinessDates.FirstOrDefault(d => d.StoreNo == item.StoreNo
                        && d.PosTerminal == item.PosID
                        && d.BussinessDate1 == item.NgayKD
                        && d.IsClosed == false);
                        if (checkItem != null)
                        {
                            checkItem.IsClosed = item.IsClosed;
                            checkItem.CloseDate = item.CloseDate;
                            checkItem.UpdatedUser = item.CloseUser;
                            countStatus++;
                        }
                    }
                    db.SaveChanges();
                    result = new Tuple<bool, string>(true, $"Đồng bộ thành công {countStatus} POS");
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, $"{JsonConvert.SerializeObject(ex)}");
            }
            return result;
        }

        public List<LogFileSalePOSModel> LogFileSalePOS(string IPServer, string storeNo, string posTerminal, string file, DateTime fromDate, DateTime toDate, out int totalRecord, int skip, int take)
        {
            var result = new List<LogFileSalePOSModel>();
            try
            {
                var dateTime = DateTime.Now.Date;
                using (var db = new CentralSalesStagingContainer(IPServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.UploadSaleLogs.Where(d => DbFunctions.TruncateTime(d.CreatedDate) >= DbFunctions.TruncateTime(fromDate)
                    && DbFunctions.TruncateTime(d.CreatedDate) <= DbFunctions.TruncateTime(toDate));

                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        data = data.Where(d => d.TableName.Contains(storeNo));
                    }
                    if (!string.IsNullOrEmpty(posTerminal))
                    {
                        data = data.Where(d => d.TableName.Contains(posTerminal));
                    }
                    if (!string.IsNullOrEmpty(file))
                    {
                        data = data.Where(d => d.TableName.Contains(file));
                    }
                    result = data.Select(a => new LogFileSalePOSModel
                    {
                        OrderNo = a.OrderNo,
                        FileName = a.FileName,
                        TableName = a.TableName,
                        IsError = a.IsError,
                        MsgError = a.MsgError,
                        CreatedDate = a.CreatedDate,
                        IpServer = a.IpServer,
                        ProcessID = a.ProcessID
                    }).ToList();
                    totalRecord = result.Count();
                    result = result.OrderByDescending(d => d.CreatedDate).Skip(skip).Take(take).ToList();
                }
            }
            catch (Exception Ex)
            {
                totalRecord = 0;
            }
            return result;
        }

        public List<SyncChangeLogStoreModel> SyncChangeLogStoreList(string storeNo, string tableName, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<SyncChangeLogStoreModel>("[dbo].[PLG_SYNC_CHANGE_LOG_LIST] @StoreNo, @TableName, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TableName", tableName ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<SyncChangeLogStoreModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<SyncChangeLogStoreModel>();
            }
        }


        public ResultResponseModel UpdateLastCounter(UpdateSyncChangeLogStoreModel req)
        {
            try
            {
                /*
                      - Chỉ cập nhật : LastCounter
                      - Không cần cập nhật: SyncTime, LastedDate
                */

                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;                    
                    var data = db.SyncChangeLogStores.FirstOrDefault(x => x.ID == req.ID && x.SiteCode == req.SiteCode && x.TableName == req.TableName);
                    if (data == null)
                    {
                        return new ResultResponseModel {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }
                    data.LastCounter = req.LastCounter;
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật Last Counter thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }













    }
}
