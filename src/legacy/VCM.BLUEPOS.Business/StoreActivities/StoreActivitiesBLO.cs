using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.StoreActivities;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.StoreActivities;
using VCM.BLUEPOS.Model.Invoice;
using VCM.BLUEPOS.Model.Common;

namespace VCM.BLUEPOS.Business.StoreActivities
{
    public interface IStoreActivitiesBLO
    {
        Task<StaffCodeModel> StaffInforByAD(string userName);
        List<StaffCodeModel> GetStaffCodeList(string storeNo);
        List<ShiftHeaderModel> ListShiftHeader(string staffCode, string storeNo, DateTime bussinessDate, string source);
        BussinessDateOpenModel GetEndDateBusiness(string storeNo);
        Task<Tuple<List<ShiftHeaderModel>, int>> GetShiftHeaderList(string storeNo, string staffCode, string typeTrans, DateTime dateTime, int Skip, int Take);
        List<SaleBusinessStoreModel> SaleBusinessStoreStaging(string storeNo, DateTime businessDate);
        List<CheckSaleStaffCodeVMModel> ListShiftHeaderByStore(string storeNo, DateTime bussinessDate);
        ViewPopupNotEnd ListTerminalNotEndShiftHeader(string storeNo, DateTime businessDate);
        List<ViewPopupNotEnd> ListTerminalNotEndShift(string storeNo, DateTime businessDate);
        List<EODCreatedInvoiceModel> ListNotCreatedInvoice(string StoreNo);
        List<TransHeaderPOSModel> ListOrderPOS(string connectStringPOS, string storeNo, string posTerminal, DateTime businessDate);
        Tuple<bool, int, int> CheckTotalSale(string storeNo, DateTime businessDate);
        List<PosTerminalMissOrderModel> ListPosTerminalMissOrderNo(string storeNo, DateTime businessDate);
        bool InsertUpdatePOSEOD(List<POSEODModel> model);
        List<BusinessDateModel> ListCheckBusinessDate(string storeNo, DateTime businessDate);
        List<SaleBusinessStoreModel> SaleBusinessStore(string storeNo, DateTime businessDate);
        List<ViewPopupNotEnd> CheckFinishSale(string storeNo, DateTime businessDate);
        void ConfirmRetryBusinessDate(List<BussinessDateConfirmModel> listmodel, int RetryConfirm);
        MessageCheckEndShiftResponse ConfirmBusinessDate(List<BussinessDateConfirmModel> listmodel);
        bool UpdateTotalSaleToPOSEOD(string storeNo, DateTime businessDate);
        List<HistoryEndShiftModel> ListHistoryEndShift(string staffCode, string storeNo, DateTime bussinessDate, string status);
        Tuple<int, string> DeleteShiftEnd(string shiftCode, string shiftNumber, string remark, string userName);
        List<StaffCodeModel> ListTypeUser(string storeNo);
        ShiftHeaderDesciptionModel GetDescription(string storeNo, string shiftCode);
        Tuple<bool, string> UpdateDescription(ShiftHeaderDesciptionModel model);
        Tuple<bool, string> DeleteDescription(ShiftHeaderDesciptionModel model);
        List<HistoryConfirmEndDatePOSModel> HistoryConfirmDatePOS(string setDB, string listStore, string PosID, DateTime fromDate, string status, int pageSize, int pageNumber);
        List<HistoryEndDateModel> ListHistoryConfirmEndDate(string setDB, string siteNo, DateTime businessDate, string isConfirm, int pageSize, int pageNumber);
        List<DataTable> ViewDetailEndDate(string storeNo, DateTime businessDate);
        MessageCheckEndShiftResponse DateBusinessUpdate(BussinessDateOpenModel model);
        Tuple<bool, string> ConfirmBusinessDateCancel(BussinessDateConfirmModel model);
        bool UpdateMisSaleLog(List<LogMisSaleModel> model);
        List<TransHeader> ListTransPOS(DateTime businessDate, string storeNo, string staffCode);
        MessageCheckEndShiftResponse FinishShift(ShiftHeaderModel headerModel, List<ShiftLineModel> lineModel);
        MessageCheckEndShiftResponse FinishShiftTH(ShiftHeaderModel headerModel, List<ShiftLineModel> lineModel);
        List<DataTable> ViewDetailEndShift(string storeNo, string staffCode, DateTime businessDate, string shifCode);
        List<DataTable> ViewDetailEndShiftTH(string storeNo, string staffCode, DateTime businessDate, string shifCode);
        List<DataTable> ViewDetailEndShiftPLG(string storeNo, string staffCode, DateTime businessDate, string shiftCode);


    }

    public class StoreActivitiesBLO : IStoreActivitiesBLO
    {
        private StoreActivitiesData _data { get; set; }
        public StoreActivitiesBLO()
        {
            _data = new StoreActivitiesData();
        }
        public Task<StaffCodeModel> StaffInforByAD(string userName)
        {
            return _data.StaffInforByAD(userName);
        }
        public List<StaffCodeModel> GetStaffCodeList(string storeNo)
        {
            return _data.GetStaffCodeList(storeNo);
        }
        public List<ShiftHeaderModel> ListShiftHeader(string staffCode, string storeNo, DateTime bussinessDate, string source)
        {
            return _data.ListShiftHeader(staffCode, storeNo, bussinessDate, source);
        }
        public BussinessDateOpenModel GetEndDateBusiness(string storeNo)
        {
            return _data.GetEndDateBusiness(storeNo);
        }
        public Task<Tuple<List<ShiftHeaderModel>, int>> GetShiftHeaderList(string storeNo, string staffCode, string typeTrans, DateTime dateTime, int Skip, int Take)
        {
            return _data.GetShiftHeaderList(storeNo, staffCode, typeTrans, dateTime, Skip, Take);
        }
        public List<SaleBusinessStoreModel> SaleBusinessStoreStaging(string storeNo, DateTime businessDate)
        {
            return _data.SaleBusinessStoreStaging(storeNo, businessDate);
        }
        public List<CheckSaleStaffCodeVMModel> ListShiftHeaderByStore(string storeNo, DateTime bussinessDate)
        {
            return _data.ListShiftHeaderByStore(storeNo, bussinessDate);
        }
        public ViewPopupNotEnd ListTerminalNotEndShiftHeader(string storeNo, DateTime businessDate)
        {
            return _data.ListTerminalNotEndShiftHeader(storeNo, businessDate);
        }
        public List<ViewPopupNotEnd> ListTerminalNotEndShift(string storeNo, DateTime businessDate)
        {
            return _data.ListTerminalNotEndShift(storeNo, businessDate);
        }
        public List<EODCreatedInvoiceModel> ListNotCreatedInvoice(string StoreNo)
        {
            return _data.ListNotCreatedInvoice(StoreNo);
        }
        public List<TransHeaderPOSModel> ListOrderPOS(string connectStringPOS, string storeNo, string posTerminal, DateTime businessDate)
        {
            return _data.ListOrderPOS(connectStringPOS, storeNo, posTerminal, businessDate);
        }
        public Tuple<bool, int, int> CheckTotalSale(string storeNo, DateTime businessDate)
        {
            return _data.CheckTotalSale(storeNo, businessDate);
        }
        public List<PosTerminalMissOrderModel> ListPosTerminalMissOrderNo(string storeNo, DateTime businessDate)
        {
            return _data.ListPosTerminalMissOrderNo(storeNo, businessDate);
        }
        public bool InsertUpdatePOSEOD(List<POSEODModel> model)
        {
            return _data.InsertUpdatePOSEOD(model);
        }
        public List<BusinessDateModel> ListCheckBusinessDate(string storeNo, DateTime businessDate)
        {
            return _data.ListCheckBusinessDate(storeNo, businessDate);
        }
        public List<SaleBusinessStoreModel> SaleBusinessStore(string storeNo, DateTime businessDate)
        {
            return _data.SaleBusinessStore(storeNo, businessDate);
        }
        public List<ViewPopupNotEnd> CheckFinishSale(string storeNo, DateTime businessDate)
        {
            return _data.CheckFinishSale(storeNo, businessDate);
        }
        public void ConfirmRetryBusinessDate(List<BussinessDateConfirmModel> listmodel, int RetryConfirm)
        {
            _data.ConfirmRetryBusinessDate(listmodel, RetryConfirm);
        }
        public MessageCheckEndShiftResponse ConfirmBusinessDate(List<BussinessDateConfirmModel> listmodel)
        {
            return _data.ConfirmBusinessDate(listmodel);
        }
        public bool UpdateTotalSaleToPOSEOD(string storeNo, DateTime businessDate)
        {
            return _data.UpdateTotalSaleToPOSEOD(storeNo, businessDate);
        }
        public List<HistoryEndShiftModel> ListHistoryEndShift(string staffCode, string storeNo, DateTime bussinessDate, string status)
        {
            return _data.ListHistoryEndShift(staffCode, storeNo, bussinessDate, status);
        }
        public Tuple<int, string> DeleteShiftEnd(string shiftCode, string shiftNumber, string remark, string userName)
        {
            return _data.DeleteShiftEnd(shiftCode, shiftNumber, remark, userName);
        }
        public List<StaffCodeModel> ListTypeUser(string storeNo)
        {
            return _data.ListTypeUser(storeNo);
        }
        public ShiftHeaderDesciptionModel GetDescription(string storeNo, string shiftCode)
        {
            return _data.GetDescription(storeNo, shiftCode);
        }
        public Tuple<bool, string> UpdateDescription(ShiftHeaderDesciptionModel model)
        {
            return _data.UpdateDescription(model);
        }
        public Tuple<bool, string> DeleteDescription(ShiftHeaderDesciptionModel model)
        {
            return _data.DeleteDescription(model);
        }   
        public List<HistoryConfirmEndDatePOSModel> HistoryConfirmDatePOS(string setDB, string listStore, string PosID, DateTime fromDate, string status, int pageSize, int pageNumber)
        {
            return _data.HistoryConfirmDatePOS(setDB, listStore, PosID, fromDate, status, pageSize, pageNumber);
        }
        public List<HistoryEndDateModel> ListHistoryConfirmEndDate(string setDB, string siteNo, DateTime businessDate, string isConfirm, int pageSize, int pageNumber)
        {
            return _data.ListHistoryConfirmEndDate(setDB, siteNo, businessDate, isConfirm, pageSize, pageNumber);
        }
        public List<DataTable> ViewDetailEndDate(string storeNo, DateTime businessDate)
        {
            return _data.ViewDetailEndDate(storeNo, businessDate);
        }
        public MessageCheckEndShiftResponse DateBusinessUpdate(BussinessDateOpenModel model)
        {
            return _data.DateBusinessUpdate(model);
        }
        public Tuple<bool, string> ConfirmBusinessDateCancel(BussinessDateConfirmModel model)
        {
            return _data.ConfirmBusinessDateCancel(model);
        }
        public bool UpdateMisSaleLog(List<LogMisSaleModel> model)
        {
            return _data.UpdateMisSaleLog(model);
        }

        public List<TransHeader> ListTransPOS(DateTime businessDate, string storeNo, string staffCode)
        {
            return _data.ListTransPOS(businessDate, storeNo, staffCode);
        }
        public MessageCheckEndShiftResponse FinishShift(ShiftHeaderModel headerModel, List<ShiftLineModel> lineModel)
        {
            return _data.FinishShift(headerModel, lineModel);
        }

        public MessageCheckEndShiftResponse FinishShiftTH(ShiftHeaderModel headerModel, List<ShiftLineModel> lineModel)
        {
            return _data.FinishShiftTH(headerModel, lineModel);
        }

        public List<DataTable> ViewDetailEndShift(string storeNo, string staffCode, DateTime businessDate, string shifCode)
        {
            return _data.ViewDetailEndShift(storeNo, staffCode, businessDate, shifCode);
        }
        public List<DataTable> ViewDetailEndShiftTH(string storeNo, string staffCode, DateTime businessDate, string shifCode)
        {
            return _data.ViewDetailEndShiftTH(storeNo, staffCode, businessDate, shifCode);
        }

        public List<DataTable> ViewDetailEndShiftPLG(string storeNo, string staffCode, DateTime businessDate, string shiftCode)
        {
            return _data.ViewDetailEndShiftPLG(storeNo, staffCode, businessDate, shiftCode);
        }











    }
}
