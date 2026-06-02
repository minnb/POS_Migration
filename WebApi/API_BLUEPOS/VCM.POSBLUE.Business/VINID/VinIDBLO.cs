using System.Collections.Generic;
using System.Threading.Tasks;
using TCX.API.Common.Models;
using VCM.POSBLUE.Data.VINID;
using VCM.POSBLUE.Model.VINID;

namespace VCM.POSBLUE.Business.VINID
{
    public interface IVinIDBLO
    {
        void WriteLogAPI(LogAPIModel model);
        List<LogAPIModel> GetListInvoiceOffline(string orderOrig);

        void WriteLogTransaction(LogTransactionLoyaltyModel model);
        string OrigInvoiceNo(string OrigOrderNo);
        StoreMappingModel StoreMapping(string siteCode, string posTerminal);
        VinIDOfflinePercentModel CalAwardOffline(double amount);
        VinIDOfflinePercentModel CalAwardOfflineCX(double amount);
        bool InsertExtral(ExtraHeaderModel header, List<ExtratItemBillModel> item);
        List<ExtraHeaderModel> CheckAmountOrderOrigin(string orderOrigin);

        void WriteLogAPI_CX(CX_LogAPIModel model);
        void CXTransaction(CX_TransactionLoyaltyModel model);
        OrigOrderNoModel CXOrigInvoiceNo(string OrigOrderNo);
        bool CXInsertExtral(CXExtralHeaderModel header, List<CXExtralItemBillModel> item);

        bool UpdateOrderOffline(string storeNo, string orderOrig);
        List<GetOrderOfflineModel> ListOrderOffline(string storeNo, string orderOrig);
        
    }
    public class VinIDBLO : IVinIDBLO
    {
        private VinIDData VinData { get; set; }
        public VinIDBLO()
        {
            VinData = new VinIDData();
        }
        public void WriteLogAPI_CX(CX_LogAPIModel model)
        {
            Task.Run(() => VinData.CXWriteLogAPI(model));
        }
        public void WriteLogAPI(LogAPIModel model)
        {
            Task.Run(() => VinData.WriteLogAPI(model));
        }
        public List<LogAPIModel> GetListInvoiceOffline(string orderOrig)
        {
            return VinData.GetListInvoiceOffline(orderOrig);
        }
        public void WriteLogTransaction(LogTransactionLoyaltyModel model)
        {
            Task.Run(() => VinData.WriteLogTransaction(model));
        }

        public string OrigInvoiceNo(string OrigOrderNo)
        {
            return VinData.OrigInvoiceNo(OrigOrderNo);
        }
        public StoreMappingModel StoreMapping(string siteCode, string posTerminal)
        {
            return VinData.StoreMapping(siteCode, posTerminal);
        }

        public VinIDOfflinePercentModel CalAwardOffline(double amount)
        {
            return VinData.CalAwardOffline(amount);
        }

        public bool InsertExtral(ExtraHeaderModel header, List<ExtratItemBillModel> item)
        {
            return VinData.InsertExtral(header, item);
        }

        public List<ExtraHeaderModel> CheckAmountOrderOrigin(string orderOrigin)
        {
            return VinData.CheckAmountOrderOrigin(orderOrigin);
        }
        public void CXTransaction(CX_TransactionLoyaltyModel model)
        {
            VinData.CXTransaction(model);
        }

        public OrigOrderNoModel CXOrigInvoiceNo(string OrigOrderNo)
        {
            return VinData.CXOrigInvoiceNo(OrigOrderNo);
        }

        public bool CXInsertExtral(CXExtralHeaderModel header, List<CXExtralItemBillModel> item)
        {
            return VinData.CXInsertExtral(header, item);
        }

        public bool UpdateOrderOffline(string storeNo, string orderOrig)
        {
            return VinData.UpdateOrderOffline(storeNo, orderOrig);
        }

        public List<GetOrderOfflineModel> ListOrderOffline(string storeNo, string orderOrig)
        {
            return VinData.ListOrderOffline(storeNo, orderOrig);
        }

        public VinIDOfflinePercentModel CalAwardOfflineCX(double amount)
        {
            return VinData.CalAwardOfflineCX(amount);
        }
    }
}
