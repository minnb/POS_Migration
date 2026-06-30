using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Report;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Report;
using VCM.BLUEPOS.Model.Report.PromotionOfferTypeComboModel;
using VCM.BLUEPOS.Model.Report.WinLifeModel;
using VCM.BLUEPOS.Model.MCH;
using VCM.BLUEPOS.Model.Order;

namespace VCM.BLUEPOS.Business.Report
{
    public interface IReportBLO
    {
        OfferTypeListModel LoadOfferTypeCombo();
        OfferTypeListModel LoadOfferTypeDiscountValue();
        List<ReportDeleteTotalRowOrderModel> GetDeleteRowTotalOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, out int totalRecord, int skip = 0, int take = 100);
        List<ExportReportDeleteTotalRowOrderModel> ExportExcelGetDeleteRowTotalOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo);
        List<ReportDetailDeleteRowOrderModel> GetDetailDeleteRowOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffID, string orderNo, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportDetailDeleteRowOrderModel> ExportExcelGetDetailDeleteRowOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffCode, string orderNo);
        List<ReportDeleteOrderListModel> GetDeleteOrderSalesList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string orderType, string orderNo, string staffCode, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportReportDeleteOrderListModel> ExportExcelGetDeleteOrderSalesList(DateTime FromDate, DateTime ToDate, string serverIP, string StoreNo, string PosID, string OrderType, string OrderNo, string StaffCode);
        List<DetailRevenueReportResponseModel> GetDetailRevenueReport(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderType, string salesType, string vatCode, string returnOrder, string userID, string textSearch, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportExcelDetailRevenueReportModel> ExportExcelDetailRevenueReport(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderType, string salesType, string vatCode, string returnOrder, string userID, string textSearch);
        List<PaymentOrderSalesResponseModel> GetPaymentOrderSalesList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderNo, string orderType, string tenderType, string userID, string staffCode, int searchBy, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportExcelPaymentOrderSalesModel> ExportExcelPaymentOrderSalesList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderNo, string orderType, string tenderType, string userID, string staffCode, int searchBy);
        List<RevenueOrderSalesByStaffModel> GetRevenueOrderSalesByStaff(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffCode);
        List<ExportExcelRevenueOrderSalesByStaffModel> ExportExcel_RevenueOrderSalesByStaff(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffCode);
        List<RevenueOrderSalesByStoreModel> GetRevenueOrderSalesByStore(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, int pageSize, int pageNumber);
        List<RevenueOrderSalesByStoreModel> GetRevenueOrderSalesByStoreAll(string userName, string storeNo, DateTime fromDate, DateTime toDate);
        List<RevenueOrderSalesByMCHModel> GetRevenueOrderSalesByMCH(string serverIP, string storeNo, DateTime fromDate, DateTime toDate);
        List<ExportExcel_RevenueOrderSalesByMCHModel> ExportExcel_RevenueOrderSalesByMCH(string serverIP, string storeNo, DateTime fromDate, DateTime toDate);
        List<SalesDetailListByMCHModel> GetDetailRevenueSalesByStoreMCH(string storeNo, string MCH2, string MCH5, DateTime fromDate, DateTime toDate);
        List<ExportExcelSalesDetailListByMCHModel> ExportExcel_GetDetailRevenueSalesByStoreMCH(string FromDate, string ToDate, string StoreNo, string MCH2, string MCH5);
        List<VoucherReceiptResponseModel> GetVoucherReceiptList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string voucherType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportVoucherReceiptModel> ExportGetVoucherReceiptList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string voucherType, string textSearch);
        List<ReportShiftEndModel> Report_ShiftEnd_VM(string serverIP, string listStore, DateTime businessDate, string staffCode, string isShiftClosed, int pageSize, int pageNumber);
        List<ReportShiftEndModel> Report_ShiftEnd_VMP(string serverIP, string listStore, DateTime businessDate, string posID, string isShiftClosed, int pageSize, int pageNumber);
        List<SalesFailDateBusByStoreModel> Sales_By_Store_Fail_BussinessDate(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, int pageSize, int pageNumber);
        List<ShiftEndReportPLGResponseModel> GetShiftEndReportPLG(string serverIP, string listStore, DateTime businessDate, string posID, string isShiftClosed, int pageSize, int pageNumber);
        List<SaleOdooModel> ListSaleOdoo(string storeNo, string orderNo, out int totalRecord, int skip, int take);
        List<OrderSalesDiscountValueResponseModel> GetPromotionDiscountValue(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber);
        List<DetailOrderSalesDiscountValueResponseModel> GetDetailPromotionDiscountValue(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber);
        List<SaleOdooModel> ListDetailOrder(string orderNo);
        List<PromotionOfferTypeComboResponseModel> GetPromotionOfferTypeByComboList(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber);
        List<DetailPromotionOfferTypeComboResponseModel> GetDetailPromotionOfferTypeByComboList(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber);
        List<SaleUsedCupModel> SaleReportUsedCup(string storeNo, DateTime businessDate, string shiftCode);
        List<RevenueOrderSalesByProductModel> GetList_Revenue_OrderSales_ByProduct(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string category, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportExcelRevenueOrderSalesByProductModel> ExportExcel_Revenue_OrderSales_ByProduct(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string category);
        List<Sale_Report_Detail_Cup_Model> ListDetailCup(string StoreNo, string BusinessDate, string ShiftCode, string ItemCup, string Size, string OrderNo, string SaleType, string ItemNo, string SaleIsReturn, out int recordsTotal, int skip, int take);
        List<GeneralPaymentOrderSalesModel> PaymentOrderSalesGeneralList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string tenderType, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportExcel_GeneralPaymentOrderSalesModel> ExportExcel_PaymentOrderSalesGeneralList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string tenderType);
        List<SalesTypeResponseModel> ReportSalesType(SalesTypeRequestModel model);

        /* --- Win Life ---*/
        List<DetailRevenueReportWinLifeResponseModel> GetDetailRevenueReportWinLife(DateTime fromDate, DateTime toDate, string storeNo, string transactionType, string returnOrder, string textSearch, string chanelSales, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportExcelDetailWinLifeRevenueReportModel> ExportExcelDetailRevenueReportWinLife(DateTime fromDate, DateTime toDate, string storeNo, string transactionType, string returnOrder, string textSearch, string chanelSales);
        List<ExportExcelPaymentOrderSalesWinLifeModel> ExportExcelPaymentOrderSalesListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string orderNo, string transactionType, string tenderType, string chanelSales);
        List<PaymentOrderSalesWinLifeResponseModel> GetPaymentOrderSalesListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string orderNo, string transactionType, string tenderType, string chanelSales, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<RevenueOrderSalesByStoreWinLifeModel> GetRevenueOrderSalesByStoreWinLife(string listStore, DateTime fromDate, DateTime toDate, out int totalRecord, int pageSize, int pageNumber);
        List<RevenueOrderSalesByStoreWinLifeModel> ExportExcel_GetRevenueOrderSalesByStoreWinLife(string listStore, DateTime fromDate, DateTime toDate);

        /* --- Bao cao doanh thu theo gio ---*/
        List<RevenueSalesByHourlyModel> GetRevenueSalesByHourly(string fromDate, string toDate, string ipServer, string storeNo, string orderType, string salesType, out int totalRecord, int pageIndex = 1, int pageSize = 100);     
        List<ExportExcelRevenueSalesByHourlyModel> ExportExcel_GetRevenueSalesByHourly(string fromDate, string toDate, string ipServer, List<string> storeNo, string orderType, List<string> salesType);
        
        /*--- Bao cao Cumulative Sales ---*/
        List<CumulativeSalesResponseModel> GetCumulativeSalesList(string fromDateSelected, string toDateSelected, string fromDateComparison, string toDateComparison, string ipServer, string channel, string storeNo);
        List<ExportExcel_CumulativeSalesResponseModel> ExportExcel_CumulativeSalesList(string fromDateSelected, string toDateSelected, string fromDateComparison, string toDateComparison, string ipServer, string channel, List<string> storeNo);

        // tungnt8,11/02/2025, báo cáo chi tiet khuyen mai
        List<SalesDetailPromotionModel> GetSalesDetailPromotion(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderType, string salesType, string textSearch, out int totalRecord, int pageIndex = 1, int pageSize = 100);



    }

    public class ReportBLO : IReportBLO
    {
        private ReportData _data { get; set; }
        public ReportBLO()
        {
            _data = new ReportData();
        }
        public OfferTypeListModel LoadOfferTypeCombo()
        {
            return _data.LoadOfferTypeCombo();
        }
        public OfferTypeListModel LoadOfferTypeDiscountValue()
        {
            return _data.LoadOfferTypeDiscountValue();
        }
        public List<ReportDeleteTotalRowOrderModel> GetDeleteRowTotalOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, out int totalRecord, int skip = 0, int take = 100)
        {
            return _data.GetDeleteRowTotalOrderList(fromDate, toDate, serverIP, storeNo, out totalRecord, skip, take);
        }
        public List<ExportReportDeleteTotalRowOrderModel> ExportExcelGetDeleteRowTotalOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo)
        {
            return _data.ExportExcelGetDeleteRowTotalOrderList(fromDate, toDate, serverIP, storeNo);
        }
        public List<ReportDetailDeleteRowOrderModel> GetDetailDeleteRowOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffID, string orderNo, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetDetailDeleteRowOrderList(fromDate, toDate, serverIP, storeNo, staffID, orderNo, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportDetailDeleteRowOrderModel> ExportExcelGetDetailDeleteRowOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffCode, string orderNo)
        {
            return _data.ExportExcelGetDetailDeleteRowOrderList(fromDate, toDate, serverIP, storeNo, staffCode, orderNo);
        }
        public List<ReportDeleteOrderListModel> GetDeleteOrderSalesList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string orderType, string orderNo, string staffCode, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetDeleteOrderSalesList(fromDate, toDate, serverIP, storeNo, posID, orderType, orderNo, staffCode, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportReportDeleteOrderListModel> ExportExcelGetDeleteOrderSalesList(DateTime FromDate, DateTime ToDate, string serverIP, string StoreNo, string PosID, string OrderType, string OrderNo, string StaffCode)
        {
            return _data.ExportExcelGetDeleteOrderSalesList(FromDate, ToDate, serverIP, StoreNo, PosID, OrderType, OrderNo, StaffCode);
        }
        public List<DetailRevenueReportResponseModel> GetDetailRevenueReport(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderType, string salesType, string vatCode, string returnOrder, string userID, string textSearch, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetDetailRevenueReport(fromDate, toDate, serverIP, storeNo, orderType, salesType, vatCode, returnOrder, userID, textSearch, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcelDetailRevenueReportModel> ExportExcelDetailRevenueReport(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderType, string salesType, string vatCode, string returnOrder, string userID, string textSearch)
        {
            return _data.ExportExcelDetailRevenueReport(fromDate, toDate, serverIP, storeNo, orderType, salesType, vatCode, returnOrder, userID, textSearch);
        }
        public List<PaymentOrderSalesResponseModel> GetPaymentOrderSalesList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderNo, string orderType, string tenderType, string userID, string staffCode, int searchBy, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetPaymentOrderSalesList(fromDate, toDate, serverIP, storeNo, orderNo, orderType, tenderType, userID, staffCode, searchBy, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcelPaymentOrderSalesModel> ExportExcelPaymentOrderSalesList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderNo, string orderType, string tenderType, string userID, string staffCode, int searchBy)
        {
            return _data.ExportExcelPaymentOrderSalesList(fromDate, toDate, serverIP, storeNo, orderNo, orderType, tenderType, userID, staffCode, searchBy);
        }
        public List<RevenueOrderSalesByStaffModel> GetRevenueOrderSalesByStaff(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffCode)
        {
            return _data.GetRevenueOrderSalesByStaff(fromDate, toDate, serverIP, storeNo, staffCode);
        }
        public List<ExportExcelRevenueOrderSalesByStaffModel> ExportExcel_RevenueOrderSalesByStaff(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffCode)
        {
            return _data.ExportExcel_RevenueOrderSalesByStaff(fromDate, toDate, serverIP, storeNo, staffCode);
        }
        public List<RevenueOrderSalesByStoreModel> GetRevenueOrderSalesByStore(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, int pageSize, int pageNumber)
        {
            return _data.GetRevenueOrderSalesByStore(serverIP, listStore, storeNo, fromDate, toDate, pageSize, pageNumber);
        }
        public List<RevenueOrderSalesByStoreModel> GetRevenueOrderSalesByStoreAll(string userName, string storeNo, DateTime fromDate, DateTime toDate)
        {
            return _data.GetRevenueOrderSalesByStoreAll(userName, storeNo, fromDate, toDate);
        }
        public List<RevenueOrderSalesByMCHModel> GetRevenueOrderSalesByMCH(string serverIP, string storeNo, DateTime fromDate, DateTime toDate)
        {
            return _data.GetRevenueOrderSalesByMCH(serverIP, storeNo, fromDate, toDate);
        }
        public List<ExportExcel_RevenueOrderSalesByMCHModel> ExportExcel_RevenueOrderSalesByMCH(string serverIP, string storeNo, DateTime fromDate, DateTime toDate)
        {
            return _data.ExportExcel_RevenueOrderSalesByMCH(serverIP, storeNo, fromDate, toDate);
        }
        public List<SalesDetailListByMCHModel> GetDetailRevenueSalesByStoreMCH(string storeNo, string MCH2, string MCH5, DateTime fromDate, DateTime toDate)
        {
            return _data.GetDetailRevenueSalesByStoreMCH(storeNo, MCH2, MCH5, fromDate, toDate);
        }
        public List<ExportExcelSalesDetailListByMCHModel> ExportExcel_GetDetailRevenueSalesByStoreMCH(string FromDate, string ToDate, string StoreNo, string MCH2, string MCH5)
        {
            return _data.ExportExcel_GetDetailRevenueSalesByStoreMCH(FromDate, ToDate, StoreNo, MCH2, MCH5);
        }
        public List<VoucherReceiptResponseModel> GetVoucherReceiptList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string voucherType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetVoucherReceiptList(fromDate, toDate, serverIP, storeNo, posID, voucherType, textSearch, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportVoucherReceiptModel> ExportGetVoucherReceiptList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string voucherType, string textSearch)
        {
            return _data.ExportGetVoucherReceiptList(fromDate, toDate, serverIP, storeNo, posID, voucherType, textSearch);
        }
        public List<ReportShiftEndModel> Report_ShiftEnd_VM(string serverIP, string listStore, DateTime businessDate, string staffCode, string isShiftClosed, int pageSize, int pageNumber)
        {
            return _data.Report_ShiftEnd_VM(serverIP, listStore, businessDate, staffCode, isShiftClosed, pageSize, pageNumber);
        }
        public List<ReportShiftEndModel> Report_ShiftEnd_VMP(string serverIP, string listStore, DateTime businessDate, string posID, string isShiftClosed, int pageSize, int pageNumber)
        {
            return _data.Report_ShiftEnd_VMP(serverIP, listStore, businessDate, posID, isShiftClosed, pageSize, pageNumber);
        }
        public List<SalesFailDateBusByStoreModel> Sales_By_Store_Fail_BussinessDate(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, int pageSize, int pageNumber)
        {
            return _data.Sales_By_Store_Fail_BussinessDate(serverIP, listStore, storeNo, fromDate, toDate, pageSize, pageNumber);
        }
        //public List<KPIStaffModel> ReportKPIStaff(string storeNo, string emp, DateTime fromDate, DateTime toDate)
        //{
        //    return _data.ReportKPIStaff(storeNo, emp, fromDate, toDate);
        //}
        public List<ShiftEndReportPLGResponseModel> GetShiftEndReportPLG(string serverIP, string listStore, DateTime businessDate, string posID, string isShiftClosed, int pageSize, int pageNumber)
        {
            return _data.GetShiftEndReportPLG(serverIP, listStore, businessDate, posID, isShiftClosed, pageSize, pageNumber);
        }
        public List<SaleOdooModel> ListSaleOdoo(string storeNo, string orderNo, out int totalRecord, int skip, int take)
        {
            return _data.ListSaleOdoo(storeNo, orderNo, out totalRecord, skip, take);
        }
        public List<SaleOdooModel> ListDetailOrder(string orderNo)
        {
            return _data.ListDetailOrder(orderNo);
        }
        public List<OrderSalesDiscountValueResponseModel> GetPromotionDiscountValue(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber)
        {
            return _data.GetPromotionDiscountValue(serverIP, listStore, storeNo, fromDate, toDate, offerType, pageSize, pageNumber);
        }
        public List<DetailOrderSalesDiscountValueResponseModel> GetDetailPromotionDiscountValue(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber)
        {
            return _data.GetDetailPromotionDiscountValue(serverIP, listStore, storeNo, fromDate, toDate, offerType, pageSize, pageNumber);
        }
        public List<PromotionOfferTypeComboResponseModel> GetPromotionOfferTypeByComboList(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber)
        {
            return _data.GetPromotionOfferTypeByComboList(serverIP, listStore, storeNo, fromDate, toDate, offerType, pageSize, pageNumber);
        }
        public List<DetailPromotionOfferTypeComboResponseModel> GetDetailPromotionOfferTypeByComboList(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber)
        {
            return _data.GetDetailPromotionOfferTypeByComboList(serverIP, listStore, storeNo, fromDate, toDate, offerType, pageSize, pageNumber);
        }
        public List<SaleUsedCupModel> SaleReportUsedCup(string storeNo, DateTime businessDate, string shiftCode)
        {
            return _data.SaleReportUsedCup(storeNo, businessDate, shiftCode);
        }
        public List<RevenueOrderSalesByProductModel> GetList_Revenue_OrderSales_ByProduct(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string category, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetList_Revenue_OrderSales_ByProduct(fromDate, toDate, serverIP, storeNo, category, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcelRevenueOrderSalesByProductModel> ExportExcel_Revenue_OrderSales_ByProduct(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string category)
        {
            return _data.ExportExcel_Revenue_OrderSales_ByProduct(fromDate, toDate, serverIP, storeNo, category);
        }
        public List<Sale_Report_Detail_Cup_Model> ListDetailCup(string StoreNo, string BusinessDate, string ShiftCode, string ItemCup, string Size, string OrderNo, string SaleType, string ItemNo, string SaleIsReturn, out int recordsTotal, int skip, int take)
        {
            return _data.ListDetailCup(StoreNo, BusinessDate, ShiftCode, ItemCup, Size, OrderNo, SaleType, ItemNo, SaleIsReturn, out recordsTotal, skip, take);
        }
        public List<GeneralPaymentOrderSalesModel> PaymentOrderSalesGeneralList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string tenderType, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.PaymentOrderSalesGeneralList(fromDate, toDate, serverIP, storeNo, tenderType, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcel_GeneralPaymentOrderSalesModel> ExportExcel_PaymentOrderSalesGeneralList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string tenderType)
        {
            return _data.ExportExcel_PaymentOrderSalesGeneralList(fromDate, toDate, serverIP, storeNo, tenderType);
        }
        public List<SalesTypeResponseModel> ReportSalesType(SalesTypeRequestModel model)
        {
            return _data.ReportSalesType(model);
        }
        public List<DetailRevenueReportWinLifeResponseModel> GetDetailRevenueReportWinLife(DateTime fromDate, DateTime toDate, string storeNo, string transactionType, string returnOrder, string textSearch, string chanelSales, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetDetailRevenueReportWinLife(fromDate, toDate, storeNo, transactionType, returnOrder, textSearch, chanelSales, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcelDetailWinLifeRevenueReportModel> ExportExcelDetailRevenueReportWinLife(DateTime fromDate, DateTime toDate, string storeNo, string transactionType, string returnOrder, string textSearch, string chanelSales)
        {
            return _data.ExportExcelDetailRevenueReportWinLife(fromDate, toDate, storeNo, transactionType, returnOrder, textSearch, chanelSales);
        }
        public List<ExportExcelPaymentOrderSalesWinLifeModel> ExportExcelPaymentOrderSalesListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string orderNo, string transactionType, string tenderType, string chanelSales)
        {
            return _data.ExportExcelPaymentOrderSalesListWinLife(fromDate, toDate, storeNo, orderNo, transactionType, tenderType, chanelSales);
        }
        public List<PaymentOrderSalesWinLifeResponseModel> GetPaymentOrderSalesListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string orderNo, string transactionType, string tenderType, string chanelSales, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetPaymentOrderSalesListWinLife(fromDate, toDate, storeNo, orderNo, transactionType, tenderType, chanelSales, out totalRecord, pageIndex, pageSize);
        }
        public List<RevenueSalesByHourlyModel> GetRevenueSalesByHourly(string fromDate, string toDate, string ipServer, string storeNo, string orderType, string salesType, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetRevenueSalesByHourly(fromDate, toDate, ipServer, storeNo, orderType, salesType, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcelRevenueSalesByHourlyModel> ExportExcel_GetRevenueSalesByHourly(string fromDate, string toDate, string ipServer, List<string> storeNo, string orderType, List<string> salesType)
        {
            return _data.ExportExcel_GetRevenueSalesByHourly(fromDate, toDate, ipServer, storeNo, orderType, salesType);
        }
        public List<CumulativeSalesResponseModel> GetCumulativeSalesList(string fromDateSelected, string toDateSelected, string fromDateComparison, string toDateComparison, string ipServer, string channel, string storeNo)
        {
            return _data.GetCumulativeSalesList(fromDateSelected, toDateSelected, fromDateComparison, toDateComparison, ipServer, channel, storeNo);
        }
        public List<ExportExcel_CumulativeSalesResponseModel> ExportExcel_CumulativeSalesList(string fromDateSelected, string toDateSelected, string fromDateComparison, string toDateComparison, string ipServer, string channel, List<string> storeNo)
        {
            return _data.ExportExcel_CumulativeSalesList(fromDateSelected, toDateSelected, fromDateComparison, toDateComparison, ipServer, channel, storeNo);
        }
        public List<RevenueOrderSalesByStoreWinLifeModel> GetRevenueOrderSalesByStoreWinLife(string listStore, DateTime fromDate, DateTime toDate, out int totalRecord, int pageSize, int pageNumber)
        {
            return _data.GetRevenueOrderSalesByStoreWinLife(listStore, fromDate, toDate, out totalRecord, pageSize, pageNumber);
        }
        public List<RevenueOrderSalesByStoreWinLifeModel> ExportExcel_GetRevenueOrderSalesByStoreWinLife(string listStore, DateTime fromDate, DateTime toDate)
        {
            return _data.ExportExcel_GetRevenueOrderSalesByStoreWinLife(listStore, fromDate, toDate);
        }

        public List<SalesDetailPromotionModel> GetSalesDetailPromotion(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderType, string salesType, string textSearch, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetSalesDetailPromotion(fromDate, toDate, serverIP, storeNo, orderType, salesType, textSearch, out totalRecord, pageSize, pageSize);
        }




    }
}
