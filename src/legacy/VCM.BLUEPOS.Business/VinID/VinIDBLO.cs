using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.VinID;
using VCM.BLUEPOS.Model.VinID;

namespace VCM.BLUEPOS.Business.VinID
{
    public interface IVinIDBLO
    {
        List<TransactionHistoryBluePOSResponseModel> GetTransactionHistoryBluePOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportTransactionLoyaltyModel> ExportTransactionHistoryBluePOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string textSearch, string orderNo);
        List<HeaderTransactionBluePointsModel> GetTransactionBluePointHeaderList(DateTime fromDate, DateTime toDate, string storeNo, string posCode, string transactionType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<DetailTransactionBluePointsModel> GetTransactionBluePointDetailList(string receiptNumber, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportTransactionBluePointsModel> ExportToExcelTransactionBluePointList(DateTime fromDate, DateTime toDate, string storeNo, string posCode, string transactionType, string textSearch);



    }
    public class VinIDBLO : IVinIDBLO
    {
        private VinIDData _data { get; set; }
        public VinIDBLO()
        {
            _data = new VinIDData();
        }
        public List<TransactionHistoryBluePOSResponseModel> GetTransactionHistoryBluePOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetTransactionHistoryBluePOS(fromDate, toDate, storeNo, posTerminal, transactionType, textSearch, orderNo, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportTransactionLoyaltyModel> ExportTransactionHistoryBluePOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string textSearch, string orderNo)
        {
            return _data.ExportTransactionHistoryBluePOS(fromDate, toDate, storeNo, posTerminal, transactionType, textSearch, orderNo);
        }
        public List<HeaderTransactionBluePointsModel> GetTransactionBluePointHeaderList(DateTime fromDate, DateTime toDate, string storeNo, string posCode, string transactionType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetTransactionBluePointHeaderList(fromDate, toDate, storeNo, posCode, transactionType, textSearch, out totalRecord, pageIndex, pageSize);
        }
        public List<DetailTransactionBluePointsModel> GetTransactionBluePointDetailList(string receiptNumber, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetTransactionBluePointDetailList(receiptNumber, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportTransactionBluePointsModel> ExportToExcelTransactionBluePointList(DateTime fromDate, DateTime toDate, string storeNo, string posCode, string transactionType, string textSearch)
        {
            return _data.ExportToExcelTransactionBluePointList(fromDate, toDate, storeNo, posCode, transactionType, textSearch);
        }





    }
}
