using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.CrownX;
using VCM.BLUEPOS.Model.CrownX;

namespace VCM.BLUEPOS.Business.CrownX
{
    public interface ICrownXBLO
    {
        List<TransactionDataCrownXPOSModel> LoadTransactionDataCrownXPOS(DateTime fromDate, DateTime toDate, string storeNo, string posID, string transactionType, string cardType, string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportExcelTransactionDataCrownXPOSModel> ExportExcelTransactionDataCrownXPOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string cardType, string textSearch, string orderNo);
        List<TransPointLineResponseModel> GetTransPointLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
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

    public class CrownXBLO : ICrownXBLO
    {
        private CrownXData _data { get; set; }
        public CrownXBLO()
        {
            _data = new CrownXData();
        }
        public List<TransactionDataCrownXPOSModel> LoadTransactionDataCrownXPOS(DateTime fromDate, DateTime toDate, string storeNo, string posID, string transactionType, string cardType, string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.LoadTransactionDataCrownXPOS(fromDate, toDate, storeNo, posID, transactionType, cardType, textSearch, orderNo, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcelTransactionDataCrownXPOSModel> ExportExcelTransactionDataCrownXPOS(DateTime fromDate, DateTime toDate, string storeNo, string posTerminal, string transactionType, string cardType, string textSearch, string orderNo)
        {
            return _data.ExportExcelTransactionDataCrownXPOS(fromDate, toDate, storeNo, posTerminal, transactionType, cardType, textSearch, orderNo);
        }
        public List<TransPointLineResponseModel> GetTransPointLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetTransPointLine(fromDate, toDate, serverIP, storeNo, posTerminal, orderType, textSearch, orderNo, out totalRecord, pageIndex, pageSize);
        }
        public List<POPupMemberCardModel> GetPopupMemberCard(string storeNo, string orderNo)
        {
            return _data.GetPopupMemberCard(storeNo, orderNo);
        }
        public List<ExportExcelTransPointLineResponseModel> ExportExcelGetTransPointLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, string orderNo)
        {
            return _data.ExportExcelGetTransPointLine(fromDate, toDate, serverIP, storeNo, posTerminal, orderType, textSearch, orderNo);
        }
        public List<TransactionByMemberStampModel> GetTransactionByMemberStamp(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetTransactionByMemberStamp(fromDate, toDate, serverIP, storeNo, posTerminal, orderType, textSearch, out  totalRecord,  pageIndex, pageSize);
        }
        public List<ExportExcelTransactionByMemberStampModel> ExportExcel_GetTransactionByMemberStamp(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch)
        {
            return _data.ExportExcel_GetTransactionByMemberStamp(fromDate, toDate, serverIP, storeNo, posTerminal, orderType, textSearch);
        }
        public List<DetailMemberEarnTransactionLineModel> GetDetailMemberEarnTransactionLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetDetailMemberEarnTransactionLine(fromDate, toDate, serverIP, storeNo, posTerminal, orderType, textSearch, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcel_DetailMemberEarnTransactionLineModel> ExportExcel_DeatilGetDetailMemberEarnTransactionLine(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch)
        {
            return _data.ExportExcel_DeatilGetDetailMemberEarnTransactionLine(fromDate, toDate, serverIP, storeNo, posTerminal, orderType, textSearch);
        }
        public List<StaffMemberRemnResponeModel> GetStaffMemberRemnList(string month, string compCode, string textSearch, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetStaffMemberRemnList(month, compCode, textSearch, status, out totalRecord, pageIndex, pageSize);
        }
        public List<ReportStaffMemberRemnModel> GetReportStaffMemberRemnList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetReportStaffMemberRemnList(fromDate, toDate, serverIP, storeNo, posTerminal, orderType, textSearch, out totalRecord, pageIndex, pageSize);
        }
        public List<DetailReportStaffMemberRemnModel> GetDetailOfferStaffTransactionList(string serverIP, string storeNo, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetDetailOfferStaffTransactionList(serverIP, storeNo, orderNo, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportReportStaffMemberRemnModel> ExportExcel_GetReportStaffMemberRemnList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posTerminal, string orderType, string textSearch)
        {
            return _data.ExportExcel_GetReportStaffMemberRemnList(fromDate, toDate, serverIP, storeNo, posTerminal, orderType, textSearch);
        }

        public List<GetOrderListByStaffRemnModel> GetOrderListByStaffRemn(string month, string staffCode, string phoneNumber, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetOrderListByStaffRemn(month, staffCode, phoneNumber, out totalRecord, pageIndex, pageSize);
        }


    }
}
