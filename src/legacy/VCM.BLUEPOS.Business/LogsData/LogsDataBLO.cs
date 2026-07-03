using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.LogsData;
using VCM.BLUEPOS.Model.LogsData.LoyaltyCXLogAPIModel;
using VCM.BLUEPOS.Data.LogsData;
using VCM.BLUEPOS.Model;

namespace VCM.BLUEPOS.Business.LogsData
{
    public interface ILogsDataBLO
    {
        // Logs Data E-Invoice
        List<APIErrorMappingResponseModel> GetAPIErrorMappingList(string eRRCode, out int totalRecord, int skip = 0, int take = 100);
        List<PublishLogResponseModel> GetPubLishLog(DateTime fromDate, DateTime toDate, string storeNo, string textSearch, out int totalRecord, int skip = 0, int take = 100);
        List<InvoiceErrorsResponseModel> GetInvoiceErrorsLog(DateTime fromDate, DateTime toDate, int exPort, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportInvoiceErrorsResponseModel> ExportExcelInvoiceErrorsLog(DateTime fromDate, DateTime toDate, int export);

        // Loyalty
        List<LoyaltyCXLogAPIResponseModel> GetLoyaltyCXLogAPI(string svProd,DateTime fromDate, DateTime toDate, string actionType, string storeNoOrPos, string keyWord, int pageSize, int pageNumber);
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


    public class LogsDataBLO : ILogsDataBLO
    {
        private LogsIData _data { get; set; }
        public LogsDataBLO()
        {
            _data = new LogsIData();
        }
        public List<APIErrorMappingResponseModel> GetAPIErrorMappingList(string eRRCode, out int totalRecord, int skip = 0, int take = 100)
        {
            return _data.GetAPIErrorMappingList(eRRCode, out totalRecord, skip, take);
        }
        public List<PublishLogResponseModel> GetPubLishLog(DateTime fromDate, DateTime toDate, string storeNo, string textSearch, out int totalRecord, int skip = 0, int take = 100)
        {
            return _data.GetPubLishLog(fromDate, toDate, storeNo, textSearch, out totalRecord, skip, take);
        }
        public List<InvoiceErrorsResponseModel> GetInvoiceErrorsLog(DateTime fromDate, DateTime toDate, int exPort, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetInvoiceErrorsLog(fromDate, toDate, exPort, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportInvoiceErrorsResponseModel> ExportExcelInvoiceErrorsLog(DateTime fromDate, DateTime toDate, int export)
        {
            return _data.ExportExcelInvoiceErrorsLog(fromDate, toDate, export);
        }
        public List<LoyaltyCXLogAPIResponseModel> GetLoyaltyCXLogAPI(string svProd, DateTime fromDate, DateTime toDate, string actionType, string storeNoOrPos, string keyWord, int pageSize, int pageNumber)
        {
            return _data.GetLoyaltyCXLogAPI(svProd, fromDate, toDate, actionType, storeNoOrPos, keyWord, pageSize, pageNumber);
        }
        public List<DetailLoyaltyCXLogAPIRequestPOSModel> GetDetailCXLogAPIRequestPOS(int id)
        {
            return _data.GetDetailCXLogAPIRequestPOS(id);
        }
        public List<DetailLoyaltyCXLogAPIRequestXMLModel> GetDetailCXLogAPIRequestXML(int id)
        {
            return _data.GetDetailCXLogAPIRequestXML(id);
        }
        public List<DetailLoyaltyCXLogAPIResponseXMLModel> GetDetailCXLogAPIResponseXML(int id)
        {
            return _data.GetDetailCXLogAPIResponseXML(id);
        }
        public List<ExportExcelLoyaltyCXLogAPIModel> ExportExcelCXLogAPI(DateTime fromDate, DateTime toDate, string storeNo, string posID, string cardNumber, string invoiceNo)
        {
            return _data.ExportExcelCXLogAPI(fromDate, toDate, storeNo, posID, cardNumber, invoiceNo);
        }
        public List<LogAPIVoucherModel> LoadLogAPIVoucher(string svProd, string partner, DateTime fromDate, DateTime toDate, string actionType, string storeNoOrPos, string keyWord, int pageSize, int pageNumber)
        {
            return _data.LoadLogAPIVoucher(svProd, partner, fromDate, toDate, actionType, storeNoOrPos, keyWord, pageSize, pageNumber);
        }
        public List<LogMisSaleModel> LogMissOrderSale(string storeNo, DateTime fromDate, DateTime toDate, out int totalRecord, int skip, int take)
        {
            return _data.LogMissOrderSale(storeNo, fromDate, toDate, out totalRecord, skip, take);
        }
        public List<LogCloseSalePOSModel> LogCloseSalePOSByBussinessDate(string IPServer, string storeNo, string posTerminal, string statusClose, DateTime bussinessDate, out int totalRecord, int skip, int take)
        {
            return _data.LogCloseSalePOSByBussinessDate(IPServer,storeNo,posTerminal,statusClose,bussinessDate, out totalRecord,skip,take);
        }
        public Tuple<bool, string> SyncBussinessDateToCentral(string IPServer, List<SyncBussinessDateModel> listModel)
        {
            return _data.SyncBussinessDateToCentral(IPServer, listModel);
        }
        public List<LogFileSalePOSModel> LogFileSalePOS(string IPServer, string storeNo, string posTerminal, string file, DateTime fromDate, DateTime toDate, out int totalRecord, int skip, int take)
        {
            return _data.LogFileSalePOS(IPServer, storeNo, posTerminal, file, fromDate, toDate, out totalRecord, skip, take);
        }
        public List<SyncChangeLogStoreModel> SyncChangeLogStoreList(string storeNo, string tableName, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.SyncChangeLogStoreList(storeNo, tableName, out totalRecord, pageIndex, pageSize);
        }
        public ResultResponseModel UpdateLastCounter(UpdateSyncChangeLogStoreModel req)
        {
            return _data.UpdateLastCounter(req);
        }















    }
}
