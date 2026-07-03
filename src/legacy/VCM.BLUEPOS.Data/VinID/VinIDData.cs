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
using VCM.BLUEPOS.Data.EF.Loyalty;
using VCM.BLUEPOS.Model.VinID;


namespace VCM.BLUEPOS.Data.VinID
{
    public interface IVinIDData
    {
        List<TransactionHistoryBluePOSResponseModel> GetTransactionHistoryBluePOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportTransactionLoyaltyModel> ExportTransactionHistoryBluePOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string textSearch, string orderNo);
        List<HeaderTransactionBluePointsModel> GetTransactionBluePointHeaderList(DateTime fromDate, DateTime toDate, string storeNo, string posCode, string transactionType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<DetailTransactionBluePointsModel> GetTransactionBluePointDetailList(string receiptNumber, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportTransactionBluePointsModel> ExportToExcelTransactionBluePointList(DateTime fromDate, DateTime toDate, string storeNo, string posCode, string transactionType, string textSearch);


    }
    public class VinIDData : IVinIDData
    {
        // Dữ liệu giao dịch VinID tại POS
        public List<TransactionHistoryBluePOSResponseModel> GetTransactionHistoryBluePOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<TransactionHistoryBluePOSResponseModel>("[dbo].[GetTransactionHistoryBluePOS] @FromDate, @ToDate, @StoreNo, @PosCode, @TransactionType, @TextSearch, @OrderNo, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosCode", posTerminal ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<TransactionHistoryBluePOSResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return (default(List<TransactionHistoryBluePOSResponseModel>));
            }
        }

        // Export: Dữ liệu giao dịch VinID tại POS
        public List<ExportTransactionLoyaltyModel> ExportTransactionHistoryBluePOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string textSearch, string orderNo)
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ExportTransactionLoyaltyModel>("[dbo].[GetTransactionHistoryBluePOS_Export] @FromDate, @ToDate, @StoreNo, @PosCode, @TransactionType, @TextSearch, @OrderNo",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosCode", posTerminal ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportTransactionLoyaltyModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ExportTransactionLoyaltyModel>));
            }
        }

        // Giao dịch điểm Blue
        public List<HeaderTransactionBluePointsModel> GetTransactionBluePointHeaderList(DateTime fromDate, DateTime toDate, string storeNo, string posCode, string transactionType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100 )
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<HeaderTransactionBluePointsModel>("[dbo].[GetTransactionBluePointHeaderList] @FromDate, @ToDate, @StoreNo, @PosCode, @TransactionType, @TextSearch, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosCode", posCode ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<HeaderTransactionBluePointsModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<HeaderTransactionBluePointsModel>();
            }
        }

        // Chi tiết: Giao dịch điểm Blue
        public List<DetailTransactionBluePointsModel> GetTransactionBluePointDetailList(string receiptNumber, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<DetailTransactionBluePointsModel>("[dbo].[GetTransactionBluePointDetailList] @ReceiptNumber, @PageSize, @PageNumber",
                        new SqlParameter("@ReceiptNumber", receiptNumber ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();
                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailTransactionBluePointsModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }

            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailTransactionBluePointsModel>();
            }
        }

        // Export : Giao dịch điểm Blue
        public List<ExportTransactionBluePointsModel> ExportToExcelTransactionBluePointList(DateTime fromDate, DateTime toDate, string storeNo, string posCode, string transactionType, string textSearch)
        {
            try
            {
                var data = new List<ExportTransactionBluePointsModel>();
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Database.SqlQuery<ExportTransactionBluePointsModel>("[dbo].[GetTransactionBluePoint_Export] @FromDate, @ToDate, @StoreNo, @PosCode, @TransactionType, @TextSearch",
                         new SqlParameter("@FromDate", fromDate),
                         new SqlParameter("@ToDate", toDate),
                         new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                         new SqlParameter("@PosCode", posCode ?? string.Empty),
                         new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                         new SqlParameter("@TextSearch", textSearch ?? string.Empty)).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ExportTransactionBluePointsModel>));
            }
        }







    }
}
