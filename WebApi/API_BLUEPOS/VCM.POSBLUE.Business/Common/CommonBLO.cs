using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.POSBLUE.Data.Common;
using VCM.POSBLUE.Data.DataBaseContext;
using VCM.POSBLUE.Model.Common;

namespace VCM.POSBLUE.Business.Common
{
    public interface ICommonBLO
    {
        TransCpnVchIssueModel TransactionQtyUse(string articleNo, string siteCode);
        BusinessDateResponse GetBusinessDate(string siteCode);
        ShiftHeaderModel GET_SHIFT_HEADER(string siteCode, string posTerminal, DateTime businessDate);
        POSMonitorInsertResponse POSMonitorInsert(POSMonitorInsertRequest model);
        PosTerminalModel CheckIPaddressPos(string IPAddress);
        List<POSDataSetupModel> GetDataSetup();
        List<SaleTableModel> GetOrderInfo(string orderNo);
        List<POSVersionModel> GetPOSVersion();
        bool CheckSaleReturn(string orderNo);
        bool InsertSignalStore(SignalStoreModel model);

        void InsertBussinessDateOpen(BussinessDateOpenModel model);
        List<POSDocumentNoModel> ListPOSDocumentNo(string storeNo, string posTerminal);

        ResponseUpdateTransModel InsertLineOrig_Update_OrderInfo(UpdateOrderInfoModel listModel);
        HealthCheckModel HealthCheck();
        bool CheckCouponLine(string itemNo, string barCode);
        InsuranceModel GetInsurance(string ReceiptNo, string POSNo, string StaffCode);

        bool UpdatePOSEOD(POSEOD_APIModel model);
        CheckTotalBillResponse CheckTotalBill(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal);
        bool IsCheckAwardSpend(string storeNo);
        StoreNoMappingModel MappingStoreWinlife(string storeBlue);
        Tuple<bool, string> KiosInsertSale(KiosInsertSaleRequest model);
        Tuple<bool, string, KiosCheckOrderResponse> KiosCheckOrder(string storeNo, string posNo, string orderNo);
        void LogSaleKios(LogSaleKiosModel model);
        Tuple<bool, string, string> CheckWinMember(string storeNo);

        List<Store> GetAllStores();
    }
    public class CommonBLO : ICommonBLO
    {
        private CommonData data { get; set; }
        public CommonBLO()
        {
            data = new CommonData();
        }
        public TransCpnVchIssueModel TransactionQtyUse(string articleNo, string siteCode)
        {
            return data.TransactionQtyUse(articleNo, siteCode);
        }

        public BusinessDateResponse GetBusinessDate(string siteCode)
        {
            return data.GetBusinessDate(siteCode);
        }

        public ShiftHeaderModel GET_SHIFT_HEADER(string siteCode, string posTerminal, DateTime businessDate)
        {
            return data.GET_SHIFT_HEADER(siteCode, posTerminal, businessDate);
        }

        public POSMonitorInsertResponse POSMonitorInsert(POSMonitorInsertRequest model)
        {
            return data.POSMonitorInsert(model);
        }

        public PosTerminalModel CheckIPaddressPos(string IPAddress)
        {
            return data.CheckIPaddressPos(IPAddress);
        }

        public List<POSDataSetupModel> GetDataSetup()
        {
            return data.GetDataSetup();
        }

        public List<SaleTableModel> GetOrderInfo(string orderNo)
        {
            return data.GetOrderInfo(orderNo);
        }

        public List<POSVersionModel> GetPOSVersion()
        {
            return data.GetPOSVersion();
        }

        public bool CheckSaleReturn(string orderNo)
        {
            return data.CheckSaleReturn(orderNo);
        }

        public bool InsertSignalStore(SignalStoreModel model)
        {
            return data.InsertSignalStore(model);
        }

        public void InsertBussinessDateOpen(BussinessDateOpenModel model)
        {
            data.InsertBussinessDateOpen(model);
        }

        public List<POSDocumentNoModel> ListPOSDocumentNo(string storeNo, string posTerminal)
        {
            return data.ListPOSDocumentNo(storeNo, posTerminal);
        }

        public ResponseUpdateTransModel InsertLineOrig_Update_OrderInfo(UpdateOrderInfoModel listModel)
        {
            return data.InsertLineOrig_Update_OrderInfo(listModel);
        }

        public HealthCheckModel HealthCheck()
        {
            return data.HealthCheck();
        }

        public bool CheckCouponLine(string itemNo, string barCode)
        {
            return data.CheckCouponLine(itemNo, barCode);
        }

        public InsuranceModel GetInsurance(string ReceiptNo, string POSNo, string StaffCode)
        {
            return data.GetInsurance(ReceiptNo, POSNo, StaffCode);
        }

        public bool UpdatePOSEOD(POSEOD_APIModel model)
        {
            return data.UpdatePOSEOD(model);
        }

        public CheckTotalBillResponse CheckTotalBill(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal)
        {
            return data.CheckTotalBill(storeNo, posTerminal, bussinessDate, posTotal);
        }

        public bool IsCheckAwardSpend(string storeNo)
        {
            return data.IsCheckAwardSpend(storeNo);
        }

        public StoreNoMappingModel MappingStoreWinlife(string storeBlue)
        {
            return data.MappingStoreWinlife(storeBlue);
        }

        public Tuple<bool, string> KiosInsertSale(KiosInsertSaleRequest model)
        {
            return data.KiosInsertSale(model);
        }

        public Tuple<bool, string, KiosCheckOrderResponse> KiosCheckOrder(string storeNo, string posNo, string orderNo)
        {
            return data.KiosCheckOrder(storeNo, posNo, orderNo);
        }

        public void LogSaleKios(LogSaleKiosModel model)
        {
            data.LogSaleKios(model);
        }

        public Tuple<bool, string, string> CheckWinMember(string storeNo)
        {
            return data.CheckWinMember(storeNo);
        }

        public List<Store> GetAllStores()
        {
            return data.GetStoreAll();
        }
    }
}
