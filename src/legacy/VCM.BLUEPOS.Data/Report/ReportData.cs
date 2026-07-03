using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Newtonsoft.Json;
using System.Globalization;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.DBRead.CentralSales;
using VCM.BLUEPOS.Model.Report;
using VCM.BLUEPOS.Model.Report.PromotionOfferTypeComboModel;
using VCM.BLUEPOS.Model.Report.WinLifeModel;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.MCH;
using VCM.BLUEPOS.Common.Helpers;
using System.Data;
using System.Drawing;

namespace VCM.BLUEPOS.Data.Report
{
    public interface IReportData
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
        List<SalesDetailListByMCHModel> GetDetailRevenueSalesByStoreMCH(string storeNo, string MCH2, string MCH5, DateTime fromDate, DateTime toDate);
        List<ExportExcelSalesDetailListByMCHModel> ExportExcel_GetDetailRevenueSalesByStoreMCH(string FromDate, string ToDate, string StoreNo, string MCH2, string MCH5);
        List<VoucherReceiptResponseModel> GetVoucherReceiptList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string voucherType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportVoucherReceiptModel> ExportGetVoucherReceiptList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string voucherType, string textSearch);
        List<ReportShiftEndModel> Report_ShiftEnd_VM(string serverIP, string listStore, DateTime businessDate, string staffCode, string isShiftClosed, int pageSize, int pageNumber);
        List<ReportShiftEndModel> Report_ShiftEnd_VMP(string serverIP, string listStore, DateTime businessDate, string posID, string isShiftClosed, int pageSize, int pageNumber);
        List<SalesFailDateBusByStoreModel> Sales_By_Store_Fail_BussinessDate(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, int pageSize, int pageNumber);
        List<ShiftEndReportPLGResponseModel> GetShiftEndReportPLG(string serverIP, string listStore, DateTime businessDate, string posID, string isShiftClosed, int pageSize, int pageNumber);
        List<OrderSalesDiscountValueResponseModel> GetPromotionDiscountValue(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber);
        List<DetailOrderSalesDiscountValueResponseModel> GetDetailPromotionDiscountValue(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber);
        List<SaleOdooModel> ListSaleOdoo(string storeNo, string orderNo, out int totalRecord, int skip, int take);
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
        List<ExportExcel_RevenueOrderSalesByMCHModel> ExportExcel_RevenueOrderSalesByMCH(string serverIP, string storeNo, DateTime fromDate, DateTime toDate);

        /* --- Win Life ---*/
        List<DetailRevenueReportWinLifeResponseModel> GetDetailRevenueReportWinLife(DateTime fromDate, DateTime toDate, string storeNo, string transactionType, string returnOrder, string textSearch, string chanelSales, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportExcelDetailWinLifeRevenueReportModel> ExportExcelDetailRevenueReportWinLife(DateTime fromDate, DateTime toDate, string storeNo, string transactionType, string returnOrder, string textSearch, string chanelSales);
        List<ExportExcelPaymentOrderSalesWinLifeModel> ExportExcelPaymentOrderSalesListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string orderNo, string transactionType, string tenderType, string chanelSales);
        List<PaymentOrderSalesWinLifeResponseModel> GetPaymentOrderSalesListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string transactionType, string orderType, string tenderType, string chanelSales, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<RevenueOrderSalesByStoreWinLifeModel> GetRevenueOrderSalesByStoreWinLife(string listStore, DateTime fromDate, DateTime toDate, out int totalRecord, int pageSize, int pageNumber);
        List<RevenueOrderSalesByStoreWinLifeModel> ExportExcel_GetRevenueOrderSalesByStoreWinLife(string listStore, DateTime fromDate, DateTime toDate);

        /*--- Bao cao theo giờ ---*/
        List<RevenueSalesByHourlyModel> GetRevenueSalesByHourly(string fromDate, string toDate, string ipServer, string storeNo, string orderType, string salesType, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportExcelRevenueSalesByHourlyModel> ExportExcel_GetRevenueSalesByHourly(string fromDate, string toDate, string ipServer, List<string> storeNo, string orderType, List<string> salesType);
        
        /*--- Bao cao Cumulative Sales ---*/
        List<CumulativeSalesResponseModel> GetCumulativeSalesList(string fromDateSelected, string toDateSelected, string fromDateComparison, string toDateComparison, string ipServer, string channel, string storeNo);
        List<ExportExcel_CumulativeSalesResponseModel> ExportExcel_CumulativeSalesList(string fromDateSelected, string toDateSelected, string fromDateComparison, string toDateComparison, string ipServer, string channel, List<string> storeNo);

        // tungnt8: 11/02/2025, báo cáo chi tiết khuyến mãi
        List<SalesDetailPromotionModel> GetSalesDetailPromotion(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderType, string salesType, string textSearch, out int totalRecord, int pageIndex = 1, int pageSize = 100);



    }

    public class ReportData : IReportData
    {
        public OfferTypeListModel LoadOfferTypeCombo()
        {
            var result = new OfferTypeListModel();
            var offerType = new List<OfferTypePromotionModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    /*  --- Khuyến mãi Combo ---
                     *  OfferType1 (ZB08)
                     */

                    offerType = db.OfferTypes.Where(d => d.Enabled == true && (d.OfferType1 == "ZB08"))
                                .Select(a => new OfferTypePromotionModel
                                {
                                    ID = a.ID,
                                    OfferType1 = a.OfferType1,
                                    OfferName = a.OfferName,
                                    IsTotalBill = a.IsTotalBill,
                                    IsSetupBuy = a.IsSetupBuy,
                                    IsSetupGet = a.IsSetupGet,
                                    IsVoucher = a.IsVoucher
                                }).OrderBy(d => d.OfferType1).ToList();

                    var salesType = db.SalesOrderTypes.Where(a => a.IsActive == true).Select(a => new SalesTypePromotionModel
                    {
                        Code = a.Code,
                        TenderType = a.TenderType,
                        Description = a.Description,
                        Percent = a.Percent,
                        ImageName = a.ImageName,
                        Pkey = a.Pkey
                    }).ToList();
                    result = new OfferTypeListModel
                    {
                        ListOfferType = offerType, 
                        ListSalesType = salesType
                    };
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }
        public OfferTypeListModel LoadOfferTypeDiscountValue()
        {
            var result = new OfferTypeListModel();
            var offerType = new List<OfferTypePromotionModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    /*  --- Khuyến mãi giảm giá ---
                     *  OfferType1 <> ZB08
                     */

                    offerType = db.OfferTypes.Where(d => d.Enabled == true && (d.OfferType1 == "ZB01" || d.OfferType1 == "ZB02" || d.OfferType1 == "ZB03" || d.OfferType1 == "ZB04"|| d.OfferType1 == "ZB05" || d.OfferType1 == "ZB06" || d.OfferType1 == "ZB07" 
                                              || d.OfferType1 == "ZB09" || d.OfferType1 == "ZB10" || d.OfferType1 == "ZB11" || d.OfferType1 == "ZB12" || d.OfferType1 == "ZB13" || d.OfferType1 == "ZB14" || d.OfferType1 == "ZB15" || d.OfferType1 == "ZB16"))
                                .Select(a => new OfferTypePromotionModel
                                {
                                    ID = a.ID,
                                    OfferType1 = a.OfferType1,
                                    OfferName = a.OfferName,
                                    IsTotalBill = a.IsTotalBill,
                                    IsSetupBuy = a.IsSetupBuy,
                                    IsSetupGet = a.IsSetupGet,
                                    IsVoucher = a.IsVoucher
                                }).OrderBy(d => d.OfferType1).ToList();

                    var salesType = db.SalesOrderTypes.Where(a => a.IsActive == true).Select(a => new SalesTypePromotionModel
                    {
                        Code = a.Code,
                        TenderType = a.TenderType,
                        Description = a.Description,
                        Percent = a.Percent,
                        ImageName = a.ImageName,
                        Pkey = a.Pkey
                    }).ToList();
                    result = new OfferTypeListModel
                    {
                        ListOfferType = offerType, 
                        ListSalesType = salesType
                    };
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }
        public List<ReportDeleteTotalRowOrderModel> GetDeleteRowTotalOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, out int totalRecord, int skip = 0, int take = 100)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<ReportDeleteTotalRowOrderModel>("[dbo].[GetDeleteRowTotalOrderListV2] @FromDate, @ToDate, @StoreNo",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ReportDeleteTotalRowOrderModel>();
                    }
                    totalRecord = data.Count();
                    var result = data.Skip(skip).Take(take).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ReportDeleteTotalRowOrderModel>();
            }
        }
        public List<ExportReportDeleteTotalRowOrderModel> ExportExcelGetDeleteRowTotalOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Database.SqlQuery<ExportReportDeleteTotalRowOrderModel>("[dbo].[GetDeleteRowTotalOrderListV2] @FromDate, @ToDate, @StoreNo",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty)).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportReportDeleteTotalRowOrderModel>(); 
            }
        }
        public List<ReportDetailDeleteRowOrderModel> GetDetailDeleteRowOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffID, string orderNo, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<ReportDetailDeleteRowOrderModel>("[dbo].[GetDetailDeleteRowOrderListV2] @FromDate, @ToDate, @StoreNo, @StaffCode, @OrderNo, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@StaffCode", staffID ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ReportDetailDeleteRowOrderModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ReportDetailDeleteRowOrderModel>();
            }
        }
        public List<ExportDetailDeleteRowOrderModel> ExportExcelGetDetailDeleteRowOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffCode, string orderNo)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Database.SqlQuery<ExportDetailDeleteRowOrderModel>("[dbo].[GetDetailDeleteRowOrderListV2] @FromDate, @ToDate, @StoreNo, @StaffCode, @OrderNo, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@StaffCode", staffCode ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ExportDetailDeleteRowOrderModel>));
            }
        }

        public List<ReportDeleteOrderListModel> GetDeleteOrderSalesList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string orderType, string orderNo, string staffCode, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<ReportDeleteOrderListModel>("[dbo].[GetDeleteOrderSalesListV2] @FromDate, @ToDate, @StoreNo, @PosID, @DocumentType, @OrderNo, @StaffCode, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosID", posID ?? string.Empty),
                        new SqlParameter("@DocumentType", orderType ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@StaffCode", staffCode ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ReportDeleteOrderListModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ReportDeleteOrderListModel>();
            }
        }

        public List<ExportReportDeleteOrderListModel> ExportExcelGetDeleteOrderSalesList(DateTime FromDate, DateTime ToDate, string serverIP, string StoreNo, string PosID, string OrderType, string OrderNo, string StaffCode)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Database.SqlQuery<ExportReportDeleteOrderListModel>("[dbo].[GetDeleteOrderSalesListV2] @FromDate, @ToDate, @StoreNo, @PosID, @DocumentType, @OrderNo, @StaffCode, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", FromDate),
                        new SqlParameter("@ToDate", ToDate),
                        new SqlParameter("@StoreNo", StoreNo ?? string.Empty),
                        new SqlParameter("@PosID", PosID ?? string.Empty),
                        new SqlParameter("@DocumentType", OrderType ?? string.Empty),
                        new SqlParameter("@OrderNo", OrderNo ?? string.Empty),
                        new SqlParameter("@StaffCode", StaffCode ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    return data;

                }
            }
            catch (Exception ex)
            {
                return new List<ExportReportDeleteOrderListModel>();
            }
        }

        public List<DetailRevenueReportResponseModel> GetDetailRevenueReport(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderType, string salesType, string vatCode, string returnOrder, string userID, string textSearch, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<DetailRevenueReportResponseModel>("[dbo].[GET_REVENUE_DETAIL_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @OrderType, @SalesType, @VatCode, @ReturnOrder, @UserID, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@SalesType", salesType ?? string.Empty),
                        new SqlParameter("@VatCode", vatCode ?? string.Empty),
                        new SqlParameter("@ReturnOrder", returnOrder ?? string.Empty),
                        new SqlParameter("@UserID", userID ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailRevenueReportResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailRevenueReportResponseModel>();
            }
        }

        public List<ExportExcelDetailRevenueReportModel> ExportExcelDetailRevenueReport(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderType, string salesType, string vatCode, string returnOrder, string userID, string textSearch)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    var data = db.Database.SqlQuery<ExportExcelDetailRevenueReportModel>("[dbo].[GET_REVENUE_DETAIL_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @OrderType, @SalesType, @VatCode, @ReturnOrder, @UserID, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@SalesType", salesType ?? string.Empty),
                        new SqlParameter("@VatCode", vatCode ?? string.Empty),
                        new SqlParameter("@ReturnOrder", returnOrder ?? string.Empty),
                        new SqlParameter("@UserID", userID ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    return data;

                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelDetailRevenueReportModel>();
            }
        }

        public List<PaymentOrderSalesResponseModel> GetPaymentOrderSalesList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderNo, string orderType, string tenderType, string userID, string staffCode, int searchBy, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<PaymentOrderSalesResponseModel>("[dbo].[GET_PAYMENT_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @OrderNo, @OrderType, @TenderType, @UserID, @StaffID, @SearchBy, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@TenderType", tenderType ?? string.Empty),
                        new SqlParameter("@UserID", userID ?? string.Empty),
                        new SqlParameter("@StaffID", staffCode ?? string.Empty),
                        new SqlParameter("@SearchBy", searchBy),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<PaymentOrderSalesResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<PaymentOrderSalesResponseModel>();
            }
        }

        public List<ExportExcelPaymentOrderSalesModel> ExportExcelPaymentOrderSalesList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderNo, string orderType, string tenderType, string userID, string staffCode, int searchBy)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    var data = db.Database.SqlQuery<ExportExcelPaymentOrderSalesModel>("[dbo].[GET_PAYMENT_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @OrderNo, @OrderType, @TenderType, @UserID, @StaffID, @SearchBy, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@TenderType", tenderType ?? string.Empty),
                        new SqlParameter("@UserID", userID ?? string.Empty),
                        new SqlParameter("@StaffID", staffCode ?? string.Empty),
                        new SqlParameter("@SearchBy", searchBy),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelPaymentOrderSalesModel>();
                    }

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelPaymentOrderSalesModel>();
            }
        }

        public List<RevenueOrderSalesByStaffModel> GetRevenueOrderSalesByStaff(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffCode)
        {
            var result = new List<RevenueOrderSalesByStaffModel>();
            var ipServerRead = serverIP.Split('\\')[0];
            using (var db = new ReadCentralSalesContainer(ipServerRead))
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<RevenueOrderSalesByStaffModel>("[dbo].[GET_REVENUE_ORDER_SALES_BY_STAFF] @FromDate, @ToDate, @StoreNo, @StaffCode",
                       new SqlParameter("@FromDate", fromDate),
                       new SqlParameter("@ToDate", toDate),
                       new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                       new SqlParameter("@StaffCode", staffCode ?? string.Empty)).ToList();
                }
                catch (Exception ex)
                {
                    return new List<RevenueOrderSalesByStaffModel>();
                }
                return result;
            }
        }

        public List<ExportExcelRevenueOrderSalesByStaffModel> ExportExcel_RevenueOrderSalesByStaff(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string staffCode)
        {
            var result = new List<ExportExcelRevenueOrderSalesByStaffModel>();
            var ipServerRead = serverIP.Split('\\')[0];
            using (var db = new ReadCentralSalesContainer(ipServerRead))
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<ExportExcelRevenueOrderSalesByStaffModel>("[dbo].[GET_REVENUE_ORDER_SALES_BY_STAFF] @FromDate, @ToDate, @StoreNo, @StaffCode",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@StaffCode", staffCode ?? string.Empty)).ToList();

                    if (result.Count == 0 || result == null)
                    {
                        return new List<ExportExcelRevenueOrderSalesByStaffModel>();
                    }

                }
                catch (Exception ex)
                {
                    return new List<ExportExcelRevenueOrderSalesByStaffModel>();
                }

                return result;
            }
        }
        public List<RevenueOrderSalesByStoreModel> GetRevenueOrderSalesByStore(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, int pageSize, int pageNumber)
        {
            var result = new List<RevenueOrderSalesByStoreModel>();
            var ipServerRead = serverIP.Split('\\')[0];
            using (var db = new ReadCentralSalesContainer(ipServerRead))
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<RevenueOrderSalesByStoreModel>("[dbo].[SP_SALES_BY_STORE_BUSSINESS_DATE] @ListStoreJson, @FromDate, @ToDate, @PageSize, @PageNumber",
                        new SqlParameter("@ListStoreJson", listStore),
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)).ToList();
                }
                catch (Exception ex)
                {
                    return new List<RevenueOrderSalesByStoreModel>();
                }
                return result;
            }
        }

        public List<RevenueOrderSalesByStoreModel> GetRevenueOrderSalesByStoreAll(string userName, string storeNo, DateTime fromDate, DateTime toDate)
        {
            var result = new List<RevenueOrderSalesByStoreModel>();
            using (var db = new CentralGeneralContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<RevenueOrderSalesByStoreModel>("[dbo].[Partner_GetRevenueOrderSalesReportByStoreAll]  @Username, @StoreNo, @FromDate, @ToDate ",
                        new SqlParameter("@Username", userName),
                        new SqlParameter("@StoreNo", storeNo),
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate)).ToList();
                }
                catch (Exception ex)
                {
                    return new List<RevenueOrderSalesByStoreModel>();
                }
                return result;
            }
        }
        public List<RevenueOrderSalesByMCHModel> GetRevenueOrderSalesByMCH(string serverIP, string storeNo, DateTime fromDate, DateTime toDate)
        {
            var data = new List<RevenueOrderSalesByMCHModel>();
            var ipServerRead = serverIP.Split('\\')[0];
            using (var db = new ReadCentralSalesContainer(ipServerRead))
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    data = db.Database.SqlQuery<RevenueOrderSalesByMCHModel>("[dbo].[GET_REVENUE_ORDER_SALES_BY_MCH_V2] @FromDate, @ToDate, @StoreNo",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo)).ToList();
                }
                catch (Exception ex)
                {
                    return new List<RevenueOrderSalesByMCHModel>();
                }
                return data;
            }
        }

        public List<ExportExcel_RevenueOrderSalesByMCHModel> ExportExcel_RevenueOrderSalesByMCH(string serverIP, string storeNo, DateTime fromDate, DateTime toDate)
        {
            var data = new List<ExportExcel_RevenueOrderSalesByMCHModel>();
            var ipServerRead = serverIP.Split('\\')[0];
            using (var db = new ReadCentralSalesContainer(ipServerRead))
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    data = db.Database.SqlQuery<ExportExcel_RevenueOrderSalesByMCHModel>("[dbo].[GET_REVENUE_ORDER_SALES_BY_MCH_V2] @FromDate, @ToDate, @StoreNo",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo)).ToList();
                }
                catch (Exception ex)
                {
                    return new List<ExportExcel_RevenueOrderSalesByMCHModel>();
                }
                return data;
            }
        }

        public List<SalesDetailListByMCHModel> GetDetailRevenueSalesByStoreMCH(string storeNo, string MCH2, string MCH5, DateTime fromDate, DateTime toDate)
        {
            var result = new List<SalesDetailListByMCHModel>();
            var ipServer = ServerIPConnection.GetIPServerReadByStore(storeNo);
            using (var db = new ReadCentralSalesContainer(ipServer))
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<SalesDetailListByMCHModel>("[dbo].[GET_DETAIL_REVENUE_SALES_BY_STORE_MCH_V2] @StoreNo, @MCH2, @MCH5, @FromDate, @ToDate",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@MCH2", MCH2 ?? string.Empty),        // Nganh hang
                        new SqlParameter("@MCH5", MCH5 ?? string.Empty),        // Mat hang
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate)).ToList();
                }
                catch (Exception ex)
                {
                    return new List<SalesDetailListByMCHModel>();
                }

                return result;
            }
        }

        public List<ExportExcelSalesDetailListByMCHModel> ExportExcel_GetDetailRevenueSalesByStoreMCH(string FromDate, string ToDate, string StoreNo, string MCH2, string MCH5)
        {
            try
            {
                var result = new List<ExportExcelSalesDetailListByMCHModel>();
                var ipServer = ServerIPConnection.GetIPServerReadByStore(StoreNo);               
                using (var db = new ReadCentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<ExportExcelSalesDetailListByMCHModel>("[dbo].[GET_DETAIL_REVENUE_SALES_BY_STORE_MCH_EXP_V2] @FromDate, @ToDate, @StoreNo, @MCH2, @MCH5",
                        new SqlParameter("@FromDate", FromDate),
                        new SqlParameter("@ToDate", ToDate),
                        new SqlParameter("@StoreNo", StoreNo ?? string.Empty),
                        new SqlParameter("@MCH2", MCH2 ?? string.Empty),
                        new SqlParameter("@MCH5", MCH5 ?? string.Empty)).ToList();

                    if (result == null || result.Count == 0)
                    {
                        return new List<ExportExcelSalesDetailListByMCHModel>();
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelSalesDetailListByMCHModel>();
            }
        }

        public List<ExportVoucherReceiptModel> ExportGetVoucherReceiptList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string voucherType, string textSearch)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Database.SqlQuery<ExportVoucherReceiptModel>("[dbo].[GetPaymentVoucherUsageList_Export] @FromDate, @ToDate, @StoreNo, @PosID, @TenderType, @TextSearch",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosID", posID ?? string.Empty),
                        new SqlParameter("@TenderType", voucherType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportVoucherReceiptModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportVoucherReceiptModel>();
            }
        }

        public List<VoucherReceiptResponseModel> GetVoucherReceiptList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string voucherType, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<VoucherReceiptResponseModel>("[dbo].[GetPaymentVoucherUsageList] @FromDate, @ToDate, @StoreNo, @PosID, @TenderType, @TextSearch, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosID", posID ?? string.Empty),
                        new SqlParameter("@TenderType", voucherType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<VoucherReceiptResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<VoucherReceiptResponseModel>();
            }
        }

        public List<ReportShiftEndModel> Report_ShiftEnd_VM(string serverIP, string listStore, DateTime businessDate, string staffCode, string isShiftClosed, int pageSize, int pageNumber)
        {
            var result = new List<ReportShiftEndModel>();
            try
            {
                using (var db = new CentralSalesContainer(serverIP))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<ReportShiftEndModel>("[dbo].[SP_REPORT_SHIFTEND_VM_ACCOUNTING] @ListStoreJson, @BusinessDate,@StaffCode,@isShiftClosed,@PageSize,@PageNumber",
                        new SqlParameter("@ListStoreJson", listStore),
                        new SqlParameter("@BusinessDate", businessDate),
                        new SqlParameter("@StaffCode", staffCode),
                        new SqlParameter("@isShiftClosed", isShiftClosed),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        public List<ReportShiftEndModel> Report_ShiftEnd_VMP(string serverIP, string listStore, DateTime businessDate, string posID, string isShiftClosed, int pageSize, int pageNumber)
        {
            var result = new List<ReportShiftEndModel>();
            try
            {
                using (var db = new CentralSalesContainer(serverIP))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<ReportShiftEndModel>("[dbo].[SP_REPORT_SHIFTEND_VMP_ACCOUNTING] @ListStoreJson,@BusinessDate,@Keyword,@isShiftClosed,@PageSize,@PageNumber ",
                        new SqlParameter("@ListStoreJson", listStore),
                        new SqlParameter("@BusinessDate", businessDate),
                        new SqlParameter("@Keyword", posID),
                        new SqlParameter("@isShiftClosed", isShiftClosed),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return result;
            }
        }
        public List<SalesFailDateBusByStoreModel> Sales_By_Store_Fail_BussinessDate(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, int pageSize, int pageNumber)
        {
            var result = new List<SalesFailDateBusByStoreModel>();
            var ipServerRead = serverIP.Split('\\')[0];
            using (var db = new ReadCentralSalesContainer(ipServerRead))
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<SalesFailDateBusByStoreModel>("[dbo].[SP_SALES_BY_STORE_FAIL_BUSSINESS_DATE] @ListStoreJson, @FromDate, @ToDate, @PageSize, @PageNumber",
                        new SqlParameter("@ListStoreJson", listStore),
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)).ToList();

                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }

        //public List<KPIStaffModel> ReportKPIStaff(string storeNo, string emp, DateTime fromDate, DateTime toDate)
        //{
        //    var result = new List<KPIStaffModel>();
        //    var ipServer = string.IsNullOrEmpty(storeNo) ? "10.235.25.72" : ServerIPConnection.GetIPServerReadByStore(storeNo);
        //    //using (var db = new ReadCentralSalesContainer(ipServer))
        //    using (var db = new CentralSalesContainer(ipServer))
        //    {
        //        try
        //        {
        //            db.Database.CommandTimeout = 2 * 60;
        //            result = db.Database.SqlQuery<KPIStaffModel>("[dbo].[SP_CAL_KPI] @Staff,@StoreNo,@FromDate,@ToDate ",
        //                new SqlParameter("@Staff", emp),
        //                new SqlParameter("@StoreNo", storeNo),
        //                new SqlParameter("@FromDate", fromDate),
        //                new SqlParameter("@ToDate", toDate)).ToList();
        //        }
        //        catch (Exception ex)
        //        {
        //        }
        //        return result;
        //    }
        //}
        public List<ShiftEndReportPLGResponseModel> GetShiftEndReportPLG(string serverIP, string listStore, DateTime businessDate, string posID, string isShiftClosed, int pageSize, int pageNumber)
        {
            var result = new List<ShiftEndReportPLGResponseModel>();
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<ShiftEndReportPLGResponseModel>("[dbo].[SP_REPORT_SHIFT_END_PLG_ACCOUNTING] @ListStoreJson, @BusinessDate, @Keyword, @isShiftClosed, @PageSize, @PageNumber",
                        new SqlParameter("@ListStoreJson", listStore),
                        new SqlParameter("@BusinessDate", businessDate),
                        new SqlParameter("@Keyword", posID),
                        new SqlParameter("@isShiftClosed", isShiftClosed),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        public List<SaleOdooModel> ListSaleOdoo(string storeNo, string orderNo, out int totalRecord, int skip, int take)
        {
            try
            {
                var result = new List<SaleOdooModel>();
                var dateTime = DateTime.Now.Date;
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Odoo_SALE.GroupBy(d => new { d.OrderNo, d.StoreNo }).Select(d => new SaleOdooModel { OrderNo = d.FirstOrDefault().OrderNo, StoreNo = d.FirstOrDefault().StoreNo, Amount = d.Sum(x => x.Amount) });

                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        data = data.Where(d => d.StoreNo.Contains(storeNo));
                    }

                    if (!string.IsNullOrEmpty(orderNo))
                    {
                        data = data.Where(d => d.OrderNo.Contains(orderNo));
                    }
                    totalRecord = data.Count();
                    result = data.OrderByDescending(d => d.OrderNo).Skip(skip).Take(take).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return (default(List<SaleOdooModel>));
            }
        }

        public List<SaleOdooModel> ListDetailOrder(string orderNo)
        {
            try
            {
                var result = new List<SaleOdooModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Odoo_SALE.Where(d => d.OrderNo.ToString().Trim() == orderNo.Trim()).Select(a => new SaleOdooModel
                    {
                        OrderNo = a.OrderNo,
                        ItemNo = a.ItemNo,
                        BarcodeNo = a.BarcodeNo,
                        UOM = a.UOM,
                        UnitPrice = a.UnitPrice,
                        Quantity = a.Quantity,
                        Discount = a.Discount,
                        Amount = a.Amount
                    }).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<SaleOdooModel>));
            }
        }

        public List<OrderSalesDiscountValueResponseModel> GetPromotionDiscountValue(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber)
        {
            var result = new List<OrderSalesDiscountValueResponseModel>();
            var ipServerRead = serverIP.Split('\\')[0];
            using (var db = new ReadCentralSalesContainer(ipServerRead))
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<OrderSalesDiscountValueResponseModel>("[dbo].[GET_PROMOTION_DISCOUNT_VALUE_V2] @ListStoreJson, @FromDate, @ToDate, @OfferType, @PageSize, @PageNumber",
                        new SqlParameter("@ListStoreJson", listStore),
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@OfferType", offerType),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (result == null || result.Count == 0)
                    {
                        return new List<OrderSalesDiscountValueResponseModel>();
                    }
                }
                catch (Exception ex)
                {
                    return new List<OrderSalesDiscountValueResponseModel>();
                }

                return result;
            }
        }

        public List<DetailOrderSalesDiscountValueResponseModel> GetDetailPromotionDiscountValue(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber)
        {
            var result = new List<DetailOrderSalesDiscountValueResponseModel>();
            var ipServerRead = serverIP.Split('\\')[0];
            using (var db = new ReadCentralSalesContainer(ipServerRead))
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<DetailOrderSalesDiscountValueResponseModel>("[dbo].[GET_DETAIL_PROMOTION_DISCOUNT_VALUE_V2] @ListStoreJson, @FromDate, @ToDate, @OfferType, @PageSize, @PageNumber",
                       new SqlParameter("@ListStoreJson", listStore),
                       new SqlParameter("@FromDate", fromDate),
                       new SqlParameter("@ToDate", toDate),
                       new SqlParameter("@OfferType", offerType),
                       new SqlParameter("@PageSize", pageSize),
                       new SqlParameter("@PageNumber", pageNumber)).ToList();
                    if (result == null || result.Count == 0)
                    {
                        return new List<DetailOrderSalesDiscountValueResponseModel>();
                    }
                }
                catch (Exception ex)
                {
                    return new List<DetailOrderSalesDiscountValueResponseModel>();
                }
                return result;
            }
        }

        public List<PromotionOfferTypeComboResponseModel> GetPromotionOfferTypeByComboList(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber)
        {
            var result = new List<PromotionOfferTypeComboResponseModel>();
            var ipServerRead = serverIP.Split('\\')[0];
            using (var db = new ReadCentralSalesContainer(ipServerRead))
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<PromotionOfferTypeComboResponseModel>("[dbo].[GET_PROMOTION_BY_COMBO_LIST_V2] @ListStoreJson, @FromDate, @ToDate, @OfferType, @PageSize, @PageNumber",
                       new SqlParameter("@ListStoreJson", listStore),
                       new SqlParameter("@FromDate", fromDate),
                       new SqlParameter("@ToDate", toDate),
                       new SqlParameter("@OfferType", offerType),
                       new SqlParameter("@PageSize", pageSize),
                       new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (result == null || result.Count == 0)
                    {
                        return new List<PromotionOfferTypeComboResponseModel>();
                    }
                }
                catch (Exception ex)
                {
                    return new List<PromotionOfferTypeComboResponseModel>();
                }
                return result;
            }
        }
        public List<DetailPromotionOfferTypeComboResponseModel> GetDetailPromotionOfferTypeByComboList(string serverIP, string listStore, string storeNo, DateTime fromDate, DateTime toDate, string offerType, int pageSize, int pageNumber)
        {
            var result = new List<DetailPromotionOfferTypeComboResponseModel>();
            var ipServerRead = serverIP.Split('\\')[0];
            using (var db = new ReadCentralSalesContainer(ipServerRead))
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<DetailPromotionOfferTypeComboResponseModel>("[dbo].[GET_DETAIL_PROMOTION_BY_COMBO_LIST_V2] @ListStoreJson, @FromDate, @ToDate, @OfferType, @PageSize, @PageNumber",
                       new SqlParameter("@ListStoreJson", listStore),
                       new SqlParameter("@FromDate", fromDate),
                       new SqlParameter("@ToDate", toDate),
                       new SqlParameter("@OfferType", offerType),
                       new SqlParameter("@PageSize", pageSize),
                       new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (result == null || result.Count == 0)
                    {
                        return new List<DetailPromotionOfferTypeComboResponseModel>();
                    }

                }
                catch (Exception ex)
                {
                    return new List<DetailPromotionOfferTypeComboResponseModel>();
                }

                return result;
            }
        }
        public List<SaleUsedCupModel> SaleReportUsedCup(string storeNo, DateTime businessDate, string shiftCode)
        {
            var result = new List<SaleUsedCupModel>();
            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
            using (var db = new ReadCentralSalesContainer(ipServer))
            {
                try
                {
                    db.Database.CommandTimeout = 15 * 60;
                    result = db.Database.SqlQuery<SaleUsedCupModel>("[dbo].[SP_REPORT_SHIFT_CUP_USED] @StoreNo, @BusinessDate, @ShiftCode",
                        new SqlParameter("@StoreNo", storeNo),
                        new SqlParameter("@BusinessDate", businessDate),
                        new SqlParameter("@ShiftCode", shiftCode)).ToList();
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }    
        public List<RevenueOrderSalesByProductModel> GetList_Revenue_OrderSales_ByProduct(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string category, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<RevenueOrderSalesByProductModel>("[dbo].[PLG_REVENUE_ORDER_SALE_BY_PRODUCT_LIST_V2] @FromDate, @ToDate, @StoreNo, @CategoryCode, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@CategoryCode", category ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<RevenueOrderSalesByProductModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<RevenueOrderSalesByProductModel>();
            }
        }

        public List<ExportExcelRevenueOrderSalesByProductModel> ExportExcel_Revenue_OrderSales_ByProduct(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string category)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Database.SqlQuery<ExportExcelRevenueOrderSalesByProductModel>("[dbo].[PLG_REVENUE_ORDER_SALE_BY_PRODUCT_LIST_V2] @FromDate, @ToDate, @StoreNo, @CategoryCode, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@CategoryCode", category ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelRevenueOrderSalesByProductModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelRevenueOrderSalesByProductModel>();
            }

        }
        public List<Sale_Report_Detail_Cup_Model> ListDetailCup(string StoreNo, string BusinessDate, string ShiftCode, string ItemCup,string Size, string OrderNo, string SaleType, string ItemNo,string SaleIsReturn, out int recordsTotal, int skip, int take)
        {
            var result = new List<Sale_Report_Detail_Cup_Model>();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(StoreNo);
                using (var db = new ReadCentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;                     
                    result = db.Database.SqlQuery<Sale_Report_Detail_Cup_Model>("[dbo].[SP_REPORT_SHIFT_CUP_DETAIL] @StoreNo, @BusinessDate,@ShiftCode,@ItemCup,@Size,@OrderNo,@SaleType,@ItemNo,@SaleIsReturn, @PageSize, @PageNumber ",
                        new SqlParameter("@StoreNo", StoreNo),
                        new SqlParameter("@BusinessDate", BusinessDate),
                        new SqlParameter("@ShiftCode", ShiftCode),
                        new SqlParameter("@ItemCup", ItemCup),
                        new SqlParameter("@Size", Size),
                        new SqlParameter("@OrderNo", OrderNo),
                        new SqlParameter("@SaleType", SaleType),
                        new SqlParameter("@ItemNo", ItemNo),
                        new SqlParameter("@SaleIsReturn", SaleIsReturn),
                        new SqlParameter("@PageSize", take),
                        new SqlParameter("@PageNumber", skip)).ToList();

                    recordsTotal = 100000;  //(result != null && result.Count > 0) ? result.FirstOrDefault().TotalRow : 0;
                    return result;
                }
            }
            catch (Exception ex)
            {
                recordsTotal = 0;
                return result;
            }
        }
        public List<GeneralPaymentOrderSalesModel> PaymentOrderSalesGeneralList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string tenderType, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<GeneralPaymentOrderSalesModel>("[dbo].[GET_PAYMENT_ORDER_SALES_GENERAL_LIST_V2] @FromDate, @ToDate, @StoreNo, @TenderType, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TenderType", tenderType ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<GeneralPaymentOrderSalesModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<GeneralPaymentOrderSalesModel>();
            }
        }

        public List<ExportExcel_GeneralPaymentOrderSalesModel> ExportExcel_PaymentOrderSalesGeneralList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string tenderType)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Database.SqlQuery<ExportExcel_GeneralPaymentOrderSalesModel>("[dbo].[GET_PAYMENT_ORDER_SALES_GENERAL_LIST_V2] @FromDate, @ToDate, @StoreNo, @TenderType, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TenderType", tenderType ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcel_GeneralPaymentOrderSalesModel>();
                    }

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcel_GeneralPaymentOrderSalesModel>();
            }
        }

        public List<SalesTypeResponseModel> ReportSalesType(SalesTypeRequestModel model)
        {
            try
            {
                var ipServer = model.SetDB.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var result = db.Database.SqlQuery<SalesTypeResponseModel>("[dbo].[SP_REPORT_SALETYPE_V2] @ListStoreJson, @FromDate,@ToDate,@SaleType,@PageSize,@PageNumber",
                        new SqlParameter("@ListStoreJson", model.ListStoreJson),
                        new SqlParameter("@FromDate", model.FromDate),
                        new SqlParameter("@ToDate", model.ToDate),
                        new SqlParameter("@SaleType", model.SalesType),
                        new SqlParameter("@PageSize", model.PageSize),
                        new SqlParameter("@PageNumber", model.PageNumber)).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<SalesTypeResponseModel>();
            }
        }

        public List<DetailRevenueReportWinLifeResponseModel> GetDetailRevenueReportWinLife(DateTime fromDate, DateTime toDate, string storeNo, string transactionType, string returnOrder, string textSearch, string chanelSales, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                using (var db = new INBOUNDContainer())
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<DetailRevenueReportWinLifeResponseModel>("[dbo].[WLF_REVENUE_DETAIL_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @TransactionType, @ReturnOrder, @TextSearch, @ChanelSales, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                        new SqlParameter("@ReturnOrder", returnOrder ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@ChanelSales", chanelSales ?? string.Empty),
                        new SqlParameter("@Export", '1'),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailRevenueReportWinLifeResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailRevenueReportWinLifeResponseModel>();
            }
        }

        public List<ExportExcelDetailWinLifeRevenueReportModel> ExportExcelDetailRevenueReportWinLife(DateTime fromDate, DateTime toDate, string storeNo, string transactionType, string returnOrder, string textSearch, string chanelSales)
        {
            try
            {
                using (var db = new INBOUNDContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Database.SqlQuery<ExportExcelDetailWinLifeRevenueReportModel>("[dbo].[WLF_REVENUE_DETAIL_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @TransactionType, @ReturnOrder, @TextSearch, @ChanelSales, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                        new SqlParameter("@ReturnOrder", returnOrder ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@ChanelSales", chanelSales ?? string.Empty),
                        new SqlParameter("@Export", '2'),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelDetailWinLifeRevenueReportModel>();
                    }
                    return data; 
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelDetailWinLifeRevenueReportModel>();
            }
        }

        public List<PaymentOrderSalesWinLifeResponseModel> GetPaymentOrderSalesListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string orderNo, string transactionType, string tenderType, string chanelSales, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                using (var db = new INBOUNDContainer()) 
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<PaymentOrderSalesWinLifeResponseModel>("[dbo].[WLF_PAYMENT_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @OrderNo, @TransactionType, @TenderType, @ChanelSales, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                        new SqlParameter("@TenderType", tenderType ?? string.Empty),
                        new SqlParameter("@ChanelSales", chanelSales ?? string.Empty),
                        new SqlParameter("@Export", '1'),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<PaymentOrderSalesWinLifeResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<PaymentOrderSalesWinLifeResponseModel>();
            }
        }
        public List<ExportExcelPaymentOrderSalesWinLifeModel> ExportExcelPaymentOrderSalesListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string orderNo, string transactionType, string tenderType, string chanelSales)
        {
            try
            {
                using (var db = new INBOUNDContainer())
                {
                    db.Database.CommandTimeout = 15 * 60;
                    var data = db.Database.SqlQuery<ExportExcelPaymentOrderSalesWinLifeModel>("[dbo].[WLF_PAYMENT_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @OrderNo, @TransactionType, @TenderType, @ChanelSales, @Export, @PageSize, @PageNumber",
                       new SqlParameter("@FromDate", fromDate),
                       new SqlParameter("@ToDate", toDate),
                       new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                       new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                       new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                       new SqlParameter("@TenderType", tenderType ?? string.Empty),
                       new SqlParameter("@ChanelSales", chanelSales ?? string.Empty),
                       new SqlParameter("@Export", '2'),
                       new SqlParameter("@PageSize", string.Empty),
                       new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelPaymentOrderSalesWinLifeModel>();
                    }

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelPaymentOrderSalesWinLifeModel>();
            }
        }

        public List<RevenueSalesByHourlyModel> GetRevenueSalesByHourly(string fromDate, string toDate, string ipServer, string storeNo, string orderType, string salesType, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                var ipServerRead = ipServer.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;
                    // --- 03/10/2023 ---
                    var data = db.Database.SqlQuery<RevenueSalesByHourlyModel>("[dbo].[GET_REVENUNE_SALES_LIST_BY_HOURLY_V2] @FromDate, @ToDate, @StoreNo, @OrderType, @SalesType, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),  // loai don hang
                        new SqlParameter("@SalesType", salesType ?? string.Empty),  // hinh thuc ban hang
                        new SqlParameter("@Export", '1'),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<RevenueSalesByHourlyModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<RevenueSalesByHourlyModel>();
            }
        }
        public List<ExportExcelRevenueSalesByHourlyModel> ExportExcel_GetRevenueSalesByHourly(string fromDate, string toDate, string ipServer, List<string> storeNo, string orderType, List<string> salesType)
        {
            try
            {
                var data = new List<ExportExcelRevenueSalesByHourlyModel>();
                var ipServerRead = ipServer.Split('\\')[0];               
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    var listStore = string.Join(";", storeNo);
                    var listSalesType = string.Join(";", salesType);

                    data = db.Database.SqlQuery<ExportExcelRevenueSalesByHourlyModel>("[dbo].[GET_REVENUNE_SALES_LIST_BY_HOURLY_V2] @FromDate, @ToDate, @StoreNo, @OrderType, @SalesType, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", listStore),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@SalesType", listSalesType ?? string.Empty),
                        new SqlParameter("@Export", '2'),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelRevenueSalesByHourlyModel>();
                    }

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelRevenueSalesByHourlyModel>();
            }
        }
        public List<CumulativeSalesResponseModel> GetCumulativeSalesList(string fromDateSelected, string toDateSelected, string fromDateComparison, string toDateComparison, string ipServer, string channel, string storeNo)
        {
            try
            {
                var ipServerRead = ipServer.Split('\\')[0];
                if (channel == "WINLIFE")
                {
                    using (var db = new INBOUNDContainer())
                    {
                        db.Database.CommandTimeout = 15 * 60;
                        var data = db.Database.SqlQuery<CumulativeSalesResponseModel>("[dbo].[WLF_GET_CUMULATIVE_SALES_LIST_BY_DATE] @FromDateSelected, @ToDateSelected, @FromDateComparison, @ToDateComparison, @StoreNo",
                        new SqlParameter("@FromDateSelected", fromDateSelected),
                        new SqlParameter("@ToDateSelected", toDateSelected),
                        new SqlParameter("@FromDateComparison", fromDateComparison),
                        new SqlParameter("@ToDateComparison", toDateComparison),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty)).ToList();

                        if (data == null || data.Count == 0)
                        {
                            return new List<CumulativeSalesResponseModel>();
                        }

                        DateTime fromSelectedDate = DateTime.ParseExact(fromDateSelected, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string fromDateSelectStr = fromSelectedDate.ToString("dd/MM/yyyy");

                        DateTime toSelectedDate = DateTime.ParseExact(toDateSelected, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string toDateSelectStr = toSelectedDate.ToString("dd/MM/yyyy");

                        DateTime fromComparisonDate = DateTime.ParseExact(fromDateComparison, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string fromDateComparisonStr = fromComparisonDate.ToString("dd/MM/yyyy");

                        DateTime toComparisonDate = DateTime.ParseExact(toDateComparison, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string toDateComparisonStr = toComparisonDate.ToString("dd/MM/yyyy");

                        var listData = data.Select(a => new CumulativeSalesResponseModel()
                        {
                            SelectedDate = fromDateSelectStr +" - "+ toDateSelectStr,
                            OnHourForSelected = a.OnHourForSelected,
                            
                            TotalAmountNETSelectedDate = a.TotalAmountNETSelectedDate,
                            CumulativeSalesBySelectedDate = a.CumulativeSalesBySelectedDate,
                            PercentBySelectedDate = a.PercentBySelectedDate,
                            TotalSalesDineInSelectedDate = a.TotalSalesDineInSelectedDate,
                            PercentTotalSalesDineInSelectedDate = a.PercentTotalSalesDineInSelectedDate,
                            TotalSalesDeliverySelectedDate = a.TotalSalesDeliverySelectedDate,
                            PercentTotalSalesDeliverySelectedDate = a.PercentTotalSalesDeliverySelectedDate,

                            ComparisonDate = fromDateComparisonStr +" - "+ toDateComparisonStr,
                            OnHourForComparison = a.OnHourForComparison,
                            
                            TotalAmountNETComparisonDate = a.TotalAmountNETComparisonDate,
                            CumulativeSalesByComparisonDate = a.CumulativeSalesByComparisonDate,
                            PercentByComparisonDate = a.PercentByComparisonDate,
                            TotalSalesDineInComparisonDate = a.TotalSalesDineInComparisonDate,
                            PercentTotalSalsesDineInComparisonDate = a.PercentTotalSalsesDineInComparisonDate,
                            TotalSalesDeliveryComparisonDate = a.TotalSalesDeliveryComparisonDate,
                            PercentTotalSalesDeliveryComparisonDate = a.PercentTotalSalesDeliveryComparisonDate
                        }).OrderBy(x => x.SelectedDate).ToList();

                        return listData;
                    }
                }
                else
                {
                    //using (var db = new CentralSalesContainer(ipServerRead))

                    using (var db = new ReadCentralSalesContainer(ipServerRead))
                    {
                        db.Database.CommandTimeout = 15 * 60;

                        var data = db.Database.SqlQuery<CumulativeSalesResponseModel>("[dbo].[GET_CUMULATIVE_SALES_LIST_BY_DATE] @FromDateSelected, @ToDateSelected, @FromDateComparison, @ToDateComparison, @Channel, @StoreNo",
                        new SqlParameter("@FromDateSelected", fromDateSelected),
                        new SqlParameter("@ToDateSelected", toDateSelected),
                        new SqlParameter("@FromDateComparison", fromDateComparison),
                        new SqlParameter("@ToDateComparison", toDateComparison),
                        new SqlParameter("@Channel", channel ?? string.Empty),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty)).ToList();

                        if (data == null || data.Count == 0)
                        {
                            return new List<CumulativeSalesResponseModel>();
                        }

                        DateTime fromSelectedDate = DateTime.ParseExact(fromDateSelected, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string fromDateSelectStr = fromSelectedDate.ToString("dd/MM/yyyy");

                        DateTime toSelectedDate = DateTime.ParseExact(toDateSelected, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string toDateSelectStr = toSelectedDate.ToString("dd/MM/yyyy");

                        DateTime fromComparisonDate = DateTime.ParseExact(fromDateComparison, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string fromDateComparisonStr = fromComparisonDate.ToString("dd/MM/yyyy");

                        DateTime toComparisonDate = DateTime.ParseExact(toDateComparison, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string toDateComparisonStr = toComparisonDate.ToString("dd/MM/yyyy");

                        var listData = data.Select(a => new CumulativeSalesResponseModel()
                        {
                            SelectedDate = fromDateSelectStr +" - "+ toDateSelectStr,
                            OnHourForSelected = a.OnHourForSelected,
                            
                            TotalAmountNETSelectedDate = a.TotalAmountNETSelectedDate,
                            CumulativeSalesBySelectedDate = a.CumulativeSalesBySelectedDate,
                            PercentBySelectedDate = a.PercentBySelectedDate,
                            TotalSalesDineInSelectedDate = a.TotalSalesDineInSelectedDate,
                            PercentTotalSalesDineInSelectedDate = a.PercentTotalSalesDineInSelectedDate,
                            TotalSalesDeliverySelectedDate = a.TotalSalesDeliverySelectedDate,
                            PercentTotalSalesDeliverySelectedDate = a.PercentTotalSalesDeliverySelectedDate,

                            ComparisonDate = fromDateComparisonStr +" - "+ toDateComparisonStr,
                            OnHourForComparison = a.OnHourForComparison,
                            
                            TotalAmountNETComparisonDate = a.TotalAmountNETComparisonDate,
                            CumulativeSalesByComparisonDate = a.CumulativeSalesByComparisonDate,
                            PercentByComparisonDate = a.PercentByComparisonDate,
                            TotalSalesDineInComparisonDate = a.TotalSalesDineInComparisonDate,
                            PercentTotalSalsesDineInComparisonDate = a.PercentTotalSalsesDineInComparisonDate,
                            TotalSalesDeliveryComparisonDate = a.TotalSalesDeliveryComparisonDate,
                            PercentTotalSalesDeliveryComparisonDate = a.PercentTotalSalesDeliveryComparisonDate
                        }).OrderBy(x => x.SelectedDate).ToList();

                        return listData;
                    }
                }              
            }
            catch (Exception ex)
            {
                return new List<CumulativeSalesResponseModel>();
            }
        }

        public List<ExportExcel_CumulativeSalesResponseModel> ExportExcel_CumulativeSalesList(string fromDateSelected, string toDateSelected, string fromDateComparison, string toDateComparison, string ipServer, string channel, List<string> storeNo)
        {
            try
            {
                var data = new List<ExportExcel_CumulativeSalesResponseModel>();
                var ipServerRead = ipServer.Split('\\')[0];
                if (channel == "WINLIFE")
                {
                    using (var db = new INBOUNDContainer())
                    {
                        db.Database.CommandTimeout = 15 * 60;
                        var listStore = "";
                        if (storeNo != null)
                        {
                            listStore = string.Join(";", storeNo);
                        }

                        data = db.Database.SqlQuery<ExportExcel_CumulativeSalesResponseModel>("[dbo].[WLF_GET_CUMULATIVE_SALES_LIST_BY_DATE_EXP] @FromDateSelected, @ToDateSelected, @FromDateComparison, @ToDateComparison, @StoreNo",
                            new SqlParameter("@FromDateSelected", fromDateSelected),
                            new SqlParameter("@ToDateSelected", toDateSelected),
                            new SqlParameter("@FromDateComparison", fromDateComparison),
                            new SqlParameter("@ToDateComparison", toDateComparison),
                            new SqlParameter("@StoreNo", listStore ?? string.Empty)).ToList();

                        if (data == null || data.Count == 0)
                        {
                            return new List<ExportExcel_CumulativeSalesResponseModel>();
                        }

                        DateTime fromSelectedDate = DateTime.ParseExact(fromDateSelected, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string fromDateSelectStr = fromSelectedDate.ToString("dd/MM/yyyy");

                        DateTime toSelectedDate = DateTime.ParseExact(toDateSelected, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string toDateSelectStr = toSelectedDate.ToString("dd/MM/yyyy");

                        DateTime fromComparisonDate = DateTime.ParseExact(fromDateComparison, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string fromDateComparisonStr = fromComparisonDate.ToString("dd/MM/yyyy");

                        DateTime toComparisonDate = DateTime.ParseExact(toDateComparison, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string toDateComparisonStr = toComparisonDate.ToString("dd/MM/yyyy");

                        var listData = data.Select(a => new ExportExcel_CumulativeSalesResponseModel()
                        {
                            SelectedDate = fromDateSelectStr +" - "+ toDateSelectStr,
                            OnHourForSelected = a.OnHourForSelected,
                            
                            TotalAmountNETSelectedDate = a.TotalAmountNETSelectedDate,
                            CumulativeSalesBySelectedDate = a.CumulativeSalesBySelectedDate,
                            PercentBySelectedDate = a.PercentBySelectedDate,
                            TotalSalesDineInSelectedDate = a.TotalSalesDineInSelectedDate,
                            PercentTotalSalesDineInSelectedDate = a.PercentTotalSalesDineInSelectedDate,
                            TotalSalesDeliverySelectedDate = a.TotalSalesDeliverySelectedDate,
                            PercentTotalSalesDeliverySelectedDate = a.PercentTotalSalesDeliverySelectedDate,

                            ComparisonDate = fromDateComparisonStr +" - "+ toDateComparisonStr,
                            OnHourForComparison = a.OnHourForComparison,
                            
                            TotalAmountNETComparisonDate = a.TotalAmountNETComparisonDate,
                            CumulativeSalesByComparisonDate = a.CumulativeSalesByComparisonDate,
                            PercentByComparisonDate = a.PercentByComparisonDate,
                            TotalSalesDineInComparisonDate = a.TotalSalesDineInComparisonDate,
                            PercentTotalSalsesDineInComparisonDate = a.PercentTotalSalsesDineInComparisonDate,
                            TotalSalesDeliveryComparisonDate = a.TotalSalesDeliveryComparisonDate,
                            PercentTotalSalesDeliveryComparisonDate = a.PercentTotalSalsesDineInComparisonDate
                        }).OrderBy(x => x.SelectedDate).ToList();

                        return listData;
                    }
                }
                else
                {
                    using (var db = new ReadCentralSalesContainer(ipServerRead))
                    {
                        db.Database.CommandTimeout = 15 * 60;
                        var listStore = "";
                        if (storeNo != null)
                        {
                            listStore = string.Join(";", storeNo);
                        }

                        data = db.Database.SqlQuery<ExportExcel_CumulativeSalesResponseModel>("[dbo].[GET_CUMULATIVE_SALES_LIST_BY_DATE_EXP] @FromDateSelected, @ToDateSelected, @FromDateComparison, @ToDateComparison, @Channel, @StoreNo",
                            new SqlParameter("@FromDateSelected", fromDateSelected),
                            new SqlParameter("@ToDateSelected", toDateSelected),
                            new SqlParameter("@FromDateComparison", fromDateComparison),
                            new SqlParameter("@ToDateComparison", toDateComparison),
                            new SqlParameter("@Channel", channel ?? string.Empty),
                            new SqlParameter("@StoreNo", listStore ?? string.Empty)).ToList();

                        if (data == null || data.Count == 0)
                        {
                            return new List<ExportExcel_CumulativeSalesResponseModel>();
                        }

                        DateTime fromSelectedDate = DateTime.ParseExact(fromDateSelected, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string fromDateSelectStr = fromSelectedDate.ToString("dd/MM/yyyy");

                        DateTime toSelectedDate = DateTime.ParseExact(toDateSelected, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string toDateSelectStr = toSelectedDate.ToString("dd/MM/yyyy");

                        DateTime fromComparisonDate = DateTime.ParseExact(fromDateComparison, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string fromDateComparisonStr = fromComparisonDate.ToString("dd/MM/yyyy");

                        DateTime toComparisonDate = DateTime.ParseExact(toDateComparison, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string toDateComparisonStr = toComparisonDate.ToString("dd/MM/yyyy");

                        var listData = data.Select(a => new ExportExcel_CumulativeSalesResponseModel()
                        {
                            SelectedDate = fromDateSelectStr +" - "+ toDateSelectStr,
                            OnHourForSelected = a.OnHourForSelected,
                            
                            TotalAmountNETSelectedDate = a.TotalAmountNETSelectedDate,
                            CumulativeSalesBySelectedDate = a.CumulativeSalesBySelectedDate,
                            PercentBySelectedDate = a.PercentBySelectedDate,
                            TotalSalesDineInSelectedDate = a.TotalSalesDineInSelectedDate,
                            PercentTotalSalesDineInSelectedDate = a.PercentTotalSalesDineInSelectedDate,
                            TotalSalesDeliverySelectedDate = a.TotalSalesDeliverySelectedDate,
                            PercentTotalSalesDeliverySelectedDate = a.PercentTotalSalesDeliverySelectedDate,

                            ComparisonDate = fromDateComparisonStr +" - "+ toDateComparisonStr,
                            OnHourForComparison = a.OnHourForComparison,
                            
                            TotalAmountNETComparisonDate = a.TotalAmountNETComparisonDate,
                            CumulativeSalesByComparisonDate = a.CumulativeSalesByComparisonDate,
                            PercentByComparisonDate = a.PercentByComparisonDate,
                            TotalSalesDineInComparisonDate = a.TotalSalesDineInComparisonDate,
                            PercentTotalSalsesDineInComparisonDate = a.PercentTotalSalsesDineInComparisonDate,
                            TotalSalesDeliveryComparisonDate = a.TotalSalesDeliveryComparisonDate,
                            PercentTotalSalesDeliveryComparisonDate = a.PercentTotalSalsesDineInComparisonDate
                        }).OrderBy(x => x.SelectedDate).ToList();
 
                        return listData;
                    }
                }               
            }
            catch (Exception ex)
            {
                return new List<ExportExcel_CumulativeSalesResponseModel>();
            }
        }

        public List<RevenueOrderSalesByStoreWinLifeModel> GetRevenueOrderSalesByStoreWinLife(string listStore, DateTime fromDate, DateTime toDate,  out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            var result = new List<RevenueOrderSalesByStoreWinLifeModel>();           
            using (var db = new INBOUNDContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    result = db.Database.SqlQuery<RevenueOrderSalesByStoreWinLifeModel>("[dbo].[WLF_REVENUE_ORDER_SALES_BY_STORE] @ListStoreJson, @FromDate, @ToDate, @PageSize, @PageNumber",
                        new SqlParameter("@ListStoreJson", listStore),
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (result == null || result.Count == 0)
                    {
                        return new List<RevenueOrderSalesByStoreWinLifeModel>();
                    }

                    totalRecord = result.FirstOrDefault().Total;
                    return result;
                }
                catch (Exception ex)
                {
                    totalRecord = 0;
                    return new List<RevenueOrderSalesByStoreWinLifeModel>();
                }               
            }
        }

        public List<RevenueOrderSalesByStoreWinLifeModel> ExportExcel_GetRevenueOrderSalesByStoreWinLife(string listStore, DateTime fromDate, DateTime toDate)
        {
            var result = new List<RevenueOrderSalesByStoreWinLifeModel>();
            using (var db = new INBOUNDContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Database.SqlQuery<RevenueOrderSalesByStoreWinLifeModel>("[dbo].[WLF_REVENUE_ORDER_SALES_BY_STORE_EXP] @ListStoreJson, @FromDate, @ToDate",
                        new SqlParameter("@ListStoreJson", listStore),
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate)).ToList();
                }
                catch (Exception ex)
                {
                    return new List<RevenueOrderSalesByStoreWinLifeModel>();
                }
                return result;
            }
        }

        // tungnt8: 11/02/2025, báo cáo chi tiết khuyến mãi
        public List<SalesDetailPromotionModel> GetSalesDetailPromotion(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string orderType, string salesType, string textSearch, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<SalesDetailPromotionModel>("[dbo].[GetSalesDetailPromotion] @FromDate, @ToDate, @StoreNo, @OrderType, @SalesType, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@SalesType", salesType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<SalesDetailPromotionModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<SalesDetailPromotionModel>();
            }
        }







    }
}
