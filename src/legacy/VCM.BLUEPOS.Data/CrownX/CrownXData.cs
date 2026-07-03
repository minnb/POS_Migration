using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Data.SqlClient;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.PLHLog;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.Loyalty;
using VCM.BLUEPOS.Data.EF.DBRead.CentralSales;
using VCM.BLUEPOS.Model.CrownX;


namespace VCM.BLUEPOS.Data.CrownX
{
    public interface ICrownXData
    {
        List<TransactionDataCrownXPOSModel> LoadTransactionDataCrownXPOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string cardType, string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportExcelTransactionDataCrownXPOSModel> ExportExcelTransactionDataCrownXPOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string cardType, string textSearch, string orderNo);
        List<TransPointLineResponseModel> GetTransPointLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType,  string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportExcelTransPointLineResponseModel> ExportExcelGetTransPointLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, string orderNo);
        List<TransactionByMemberStampModel> GetTransactionByMemberStamp(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportExcelTransactionByMemberStampModel> ExportExcel_GetTransactionByMemberStamp(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch);
        List<DetailMemberEarnTransactionLineModel> GetDetailMemberEarnTransactionLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportExcel_DetailMemberEarnTransactionLineModel> ExportExcel_DeatilGetDetailMemberEarnTransactionLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch);
        List<POPupMemberCardModel> GetPopupMemberCard(string storeNo, string orderNo);
        List<StaffMemberRemnResponeModel> GetStaffMemberRemnList(string month, string compCode, string textSearch, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ReportStaffMemberRemnModel> GetReportStaffMemberRemnList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<DetailReportStaffMemberRemnModel> GetDetailOfferStaffTransactionList(string serverIP, string storeNo, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportReportStaffMemberRemnModel> ExportExcel_GetReportStaffMemberRemnList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch);
        List<GetOrderListByStaffRemnModel> GetOrderListByStaffRemn(string month, string staffCode, string phoneNumber, out int totalRecord, int pageIndex = 0, int pageSize = 100);


    }

    public class CrownXData : ICrownXData
    {
        public List<TransactionDataCrownXPOSModel> LoadTransactionDataCrownXPOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string cardType, string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new PLHLogContainer()) 
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<TransactionDataCrownXPOSModel>("[dbo].[PL_GetTransactionLoyaltyCrownXOnPOS] @FromDate, @ToDate, @StoreNo, @PosCode, @TransactionType, @CardType, @TextSearch, @OrderNo, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosCode", posTerminal ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                        new SqlParameter("@CardType", cardType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)
                        ).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<TransactionDataCrownXPOSModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return (default(List<TransactionDataCrownXPOSModel>));
            }
        }
        public List<ExportExcelTransactionDataCrownXPOSModel> ExportExcelTransactionDataCrownXPOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string cardType, string textSearch, string orderNo)
        {
            try
            {
                using (var db = new PLHLogContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ExportExcelTransactionDataCrownXPOSModel>("[dbo].[PL_GetTransactionLoyaltyCrownXOnPOS] @FromDate, @ToDate, @StoreNo, @PosCode, @TransactionType, @CardType, @TextSearch, @OrderNo, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosCode", posTerminal ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                        new SqlParameter("@CardType", cardType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelTransactionDataCrownXPOSModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelTransactionDataCrownXPOSModel>();
            }
        }

        public List<TransPointLineResponseModel> GetTransPointLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                //var ipServer = string.IsNullOrEmpty(storeNo) ? serverIP : ServerIPConnection.GetIPServerByStore(storeNo);
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new CentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<TransPointLineResponseModel>("[dbo].[GET_TRANS_POINT_LINE] @FromDate, @ToDate, @StoreNo, @PosTerminal, @OrderType, @TextSearch, @OrderNo, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosTerminal", posTerminal ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<TransPointLineResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<TransPointLineResponseModel>();
            }
        }

        // Show thông tin thẻ Phúc Long
        public List<POPupMemberCardModel> GetPopupMemberCard(string storeNo, string orderNo)
        {
            try
            {
                var ipServerRead = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var result = db.Database.SqlQuery<POPupMemberCardModel>("[dbo].[GetMemberCardInfoByCardPLH] @StoreNo, @OrderNo",
                            new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                            new SqlParameter("@OrderNo", orderNo ?? string.Empty)).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<POPupMemberCardModel>();
            }
        }

        public List<ExportExcelTransPointLineResponseModel> ExportExcelGetTransPointLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, string orderNo)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new CentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;

                    var data = db.Database.SqlQuery<ExportExcelTransPointLineResponseModel>("[dbo].[GET_TRANS_POINT_LINE] @FromDate, @ToDate, @StoreNo, @PosTerminal, @OrderType, @TextSearch, @OrderNo, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosTerminal", posTerminal ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelTransPointLineResponseModel>();
                    }   
                    
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelTransPointLineResponseModel>();
            }
        }
        public List<TransactionByMemberStampModel> GetTransactionByMemberStamp(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;

                    //var data = db.Database.SqlQuery<TransactionByMemberStampModel>("[dbo].[GetMemberTransactionByStamps] @FromDate, @ToDate, @StoreNo, @POSTerminal, @OrderType, @TextSearch, @Exp, @PageSize, @PageNumber",
                    //    new SqlParameter("@FromDate", fromDate),
                    //    new SqlParameter("@ToDate", toDate),
                    //    new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                    //    new SqlParameter("@POSTerminal", posTerminal ?? string.Empty),
                    //    new SqlParameter("@OrderType", orderType ?? string.Empty),
                    //    new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                    //    new SqlParameter("@Exp", 1),
                    //    new SqlParameter("@PageSize", pageSize),
                    //    new SqlParameter("@PageNumber", pageIndex)).ToList();

                    var data = db.Database.SqlQuery<TransactionByMemberStampModel>("[dbo].[GetMemberTransactionByStamps_V2] @FromDate, @ToDate, @StoreNo, @POSTerminal, @OrderType, @TextSearch, @Exp, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@POSTerminal", posTerminal ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Exp", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<TransactionByMemberStampModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<TransactionByMemberStampModel>();
            }
        }

        public List<ExportExcelTransactionByMemberStampModel> ExportExcel_GetTransactionByMemberStamp(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;

                    //var data = db.Database.SqlQuery<ExportExcelTransactionByMemberStampModel>("[dbo].[GetMemberTransactionByStamps] @FromDate, @ToDate, @StoreNo, @POSTerminal, @OrderType, @TextSearch, @Exp, @PageSize, @PageNumber",
                    //    new SqlParameter("@FromDate", fromDate),
                    //    new SqlParameter("@ToDate", toDate),
                    //    new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                    //    new SqlParameter("@POSTerminal", posTerminal ?? string.Empty),
                    //    new SqlParameter("@OrderType", orderType ?? string.Empty),
                    //    new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                    //    new SqlParameter("@Exp", 2),
                    //    new SqlParameter("@PageSize", string.Empty),
                    //    new SqlParameter("@PageNumber", string.Empty)).ToList();

                    var data = db.Database.SqlQuery<ExportExcelTransactionByMemberStampModel>("[dbo].[GetMemberTransactionByStamps_V2] @FromDate, @ToDate, @StoreNo, @POSTerminal, @OrderType, @TextSearch, @Exp, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@POSTerminal", posTerminal ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Exp", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelTransactionByMemberStampModel>();
                    }

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelTransactionByMemberStampModel>();
            }
        }
        public List<DetailMemberEarnTransactionLineModel> GetDetailMemberEarnTransactionLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<DetailMemberEarnTransactionLineModel>("[dbo].[GetDetailMemberEarnTransactionLine] @FromDate, @ToDate, @StoreNo, @POSTerminal, @OrderType, @TextSearch, @Exp, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@POSTerminal", posTerminal ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Exp", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailMemberEarnTransactionLineModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailMemberEarnTransactionLineModel>();
            }
        }
        public List<ExportExcel_DetailMemberEarnTransactionLineModel> ExportExcel_DeatilGetDetailMemberEarnTransactionLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Database.SqlQuery<ExportExcel_DetailMemberEarnTransactionLineModel>("[dbo].[GetDetailMemberEarnTransactionLine] @FromDate, @ToDate, @StoreNo, @POSTerminal, @OrderType, @TextSearch, @Exp, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@POSTerminal", posTerminal ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Exp", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcel_DetailMemberEarnTransactionLineModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcel_DetailMemberEarnTransactionLineModel>();
            }
        }
        public List<StaffMemberRemnResponeModel> GetStaffMemberRemnList(string month, string compCode, string textSearch, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<StaffMemberRemnResponeModel>("[dbo].[GetStaffMemberRemnList] @month, @compcode, @textsearch, @status, @export, @pageSize, @pageNumber",
                        new SqlParameter("@month", month ?? string .Empty),
                        new SqlParameter("@compcode", compCode ?? string .Empty),
                        new SqlParameter("@textsearch", textSearch ?? string.Empty),
                        new SqlParameter("@status", status ?? string.Empty),
                        new SqlParameter("export", '0'),
                        new SqlParameter("@pageSize", pageSize),
                        new SqlParameter("@pageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<StaffMemberRemnResponeModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<StaffMemberRemnResponeModel>();
            }
        }
        public List<ReportStaffMemberRemnModel> GetReportStaffMemberRemnList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<ReportStaffMemberRemnModel>("[dbo].[GetOfferStaffTransaction] @FromDate, @ToDate, @StoreNo, @PosNo, @OrderType, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosNo", posTerminal ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Export", '0'),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ReportStaffMemberRemnModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ReportStaffMemberRemnModel>();
            }
        }

        public List<DetailReportStaffMemberRemnModel> GetDetailOfferStaffTransactionList(string serverIP, string storeNo, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerReadByStore(storeNo);
                var ipServerRead = ipServer.Split('\\')[0];

                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<DetailReportStaffMemberRemnModel>("[dbo].[GetDetailOfferStaffTransaction] @OrderNo, @PageSize, @PageNumber",                     
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailReportStaffMemberRemnModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailReportStaffMemberRemnModel>();
            }
        }
        public List<ExportReportStaffMemberRemnModel> ExportExcel_GetReportStaffMemberRemnList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch)
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Database.SqlQuery<ExportReportStaffMemberRemnModel>("[dbo].[GetOfferStaffTransaction] @FromDate, @ToDate, @StoreNo, @PosNo, @OrderType, @TextSearch, @Export, @PageSize, @PageNumber",
                       new SqlParameter("@FromDate", fromDate),
                       new SqlParameter("@ToDate", toDate),
                       new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                       new SqlParameter("@PosNo", posTerminal ?? string.Empty),
                       new SqlParameter("@OrderType", orderType ?? string.Empty),
                       new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                       new SqlParameter("@Export", '1'),
                       new SqlParameter("@PageSize", string.Empty),
                       new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportReportStaffMemberRemnModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportReportStaffMemberRemnModel>();
            }
        }

        public List<GetOrderListByStaffRemnModel> GetOrderListByStaffRemn(string month, string staffCode, string phoneNumber, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<GetOrderListByStaffRemnModel>("[dbo].[GetOrderListByStaffRemn] @Month, @StaffCode, @PhoneNumber, @PageSize, @PageNumber",
                        new SqlParameter("@Month", month ?? string.Empty),
                        new SqlParameter("@StaffCode", staffCode ?? string.Empty),
                        new SqlParameter("@PhoneNumber", phoneNumber ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<GetOrderListByStaffRemnModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<GetOrderListByStaffRemnModel>();
            }
        }










    }
}
