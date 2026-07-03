using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data;
using VCM.BLUEPOS.Data.Authen;
using VCM.BLUEPOS.Data.Common;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.DBRead.CentralSales;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Order;
using VCM.BLUEPOS.Model.Order.OrderWinLifeModel;
using VCM.BLUEPOS.Model.Order.PrintInvoiceOrderSalesModel;
using VCM.BLUEPOS.Common.Helpers;


namespace VCM.BLUEPOS.Data.Order
{
    public interface IOrderData
    {
        List<OrderListResponseModel> GetOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string orderType, string salesType, string userID, string textSearchOrder, string textSearchItem, float fromAmount, float toAmount, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportOrderListResponseModel> ExportExcelOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string orderType, string salesType, string userID, string textSearchOrder, string textSearchItem, float fromAmount, float toAmount);
        List<DetailOrderListResponseModel> GetDetailOrderList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50);
        List<ViewDetailPromotionVoucherCouponModel> GetOrderDetailPromotionList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50);
        List<ViewDetailPromotionVoucherCouponModel> GetOrderDetailPromotionByPosterminal(string fromDate, string toDate, string serverIP, string storeNo, string posTerminal, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);        
        List<ExportViewDetailPromotionVoucherCouponModel> Export_Get_Detail_Promotion_List_By_Posterminal(string fromDate, string toDate, string serverIP, string storeNo, string posTerminal, string orderNo);
        List<PaymentDetailOrderResponseModel> GetPaymentDetailOrderList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50);
        ResultResponseModel UpdateSalesType(UpdateSalesTypeForOrderModel req);

        /* --- Win Life ----*/
        List<OrderListWinLifeResponseModel> GetOrderListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string posID, string orderType, string transactionType, string textSearchOrder, string chanelSales, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportOrderListWinLifeResponseModel> ExportExcelOrderListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string posID, string orderType, string transactionType, string textSearchOrder, string chanelSales);
        List<DetailOrderListWinLifeResponseModel> GetDetailOrderListWinLife(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50);
        List<PaymentDetailOrderWinLifeResponseModel> GetPaymentDetailOrderListWinLife(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50);


    }

    public class OrderData : IOrderData
    {
        public List<OrderListResponseModel> GetOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string orderType, string salesType, string userID, string textSearchOrder, string textSearchItem, float fromAmount, float toAmount, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        { 
            try
            {
                var ipServerRead = serverIP.Split('\\')[0]; 
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;
                    // 11/03/2024 : theo sanh sách store cua user da duoc phan quyen
                    var data = db.Database.SqlQuery<OrderListResponseModel>("[dbo].[GET_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @PosID, @OrderType, @SalesType, @UserID, @TextSearch1, @TextSearch2, @FromAmount, @ToAmount, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosID", posID ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@SalesType", salesType ?? string.Empty),
                        new SqlParameter("@UserID", userID ?? string.Empty),
                        new SqlParameter("@TextSearch1", textSearchOrder ?? string.Empty),
                        new SqlParameter("@TextSearch2", textSearchItem ?? string.Empty),
                        new SqlParameter("@FromAmount", fromAmount),
                        new SqlParameter("@ToAmount", toAmount),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OrderListResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<OrderListResponseModel>();
            }
        }

        public List<ExportOrderListResponseModel> ExportExcelOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string orderType, string salesType, string userID, string textSearchOrder, string textSearchItem, float fromAmount, float toAmount)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;

                    // 11/03/2024 : theo sanh sách store cua user da duoc phan quyen
                    var data = db.Database.SqlQuery<ExportOrderListResponseModel>("[dbo].[GET_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @PosID, @OrderType, @SalesType, @UserID, @TextSearch1, @TextSearch2, @FromAmount, @ToAmount, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosID", posID ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@SalesType", salesType ?? string.Empty),
                        new SqlParameter("@UserID", userID ?? string.Empty),
                        new SqlParameter("@TextSearch1", textSearchOrder ?? string.Empty),
                        new SqlParameter("@TextSearch2", textSearchItem ?? string.Empty),
                        new SqlParameter("@FromAmount", fromAmount),
                        new SqlParameter("@ToAmount", toAmount),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportOrderListResponseModel>();
            }
        }

        public List<DetailOrderListResponseModel> GetDetailOrderList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerReadByStore(storeNo);             
                var ipServerRead = ipServer.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<DetailOrderListResponseModel>("[dbo].[GetDetailOrderList] @OrderNo",
                    new SqlParameter("@OrderNo", orderNo ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailOrderListResponseModel>();
                    }

                    totalRecord = data.Count();
                    var listData = data.Skip(skip).Take(take).ToList();
                    var result = (from x in listData
                                  select new DetailOrderListResponseModel
                                  {
                                      OrderNo = x.OrderNo,
                                      LineNo = x.LineNo,
                                      ItemNo = x.ItemNo,
                                      ItemNoPLG = x.ItemNoPLG,
                                      Description = x.Description,
                                      UnitOfMeasure = x.UnitOfMeasure,
                                      Barcode = x.Barcode,
                                      Quantity = x.Quantity,
                                      UnitPrice = x.UnitPrice,
                                      DiscountAmount = x.DiscountAmount,
                                      LineAmountIncVAT = x.LineAmountIncVAT,
                                      VATAmount = x.VATAmount,
                                      ToppingStr = x.ToppingStr,
                                      CardCrownXCVL = x.CardCrownXCVL,
                                      MemberPointsEarnCVL = x.MemberPointsEarnCVL,
                                      MemberPointsRedeemCVL = x.MemberPointsRedeemCVL,
                                      CardCrownXPLH = x.CardCrownXPLH,
                                      MemberPointsEarnPLH = x.MemberPointsEarnPLH,
                                      MemberPointsRedeemPLH = x.MemberPointsRedeemPLH
                                  }).OrderBy(x=>x.LineNo).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailOrderListResponseModel>();
            }
        }

        public List<ViewDetailPromotionVoucherCouponModel> GetOrderDetailPromotionList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerReadByStore(storeNo);
                var ipServerRead = ipServer.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<ViewDetailPromotionVoucherCouponModel>("[dbo].[GET_DETAIL_PROMOTION_BY_ORDER_SALES] @OrderNo",
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty)).ToList();
                    if (data == null || data.Count == 0)
                    {
                        return new List<ViewDetailPromotionVoucherCouponModel>();
                    }

                    totalRecord = data.Count();
                    var listData = data.Skip(skip).Take(take).ToList();

                    var result = (from x in listData
                                  select new ViewDetailPromotionVoucherCouponModel
                                  {
                                      OrderNo = x.OrderNo,
                                      OrderDateStr = x.OrderDateStr,
                                      StoreNo = x.StoreNo,
                                      POSTerminalNo = x.POSTerminalNo,
                                      OrderLineNo = x.OrderLineNo,
                                      LineNo = x.LineNo,
                                      LineType = x.LineType,
                                      OfferType = x.OfferType,
                                      OfferNo = x.OfferNo,
                                      ItemNo = x.ItemNo,
                                      Barcode = x.Barcode,
                                      Description = x.Description,
                                      UOM = x.UOM,
                                      Quantity = x.Quantity,
                                      UnitPrice = x.UnitPrice,
                                      DiscountAmount = x.DiscountAmount
                                  }).OrderBy(x=>x.LineNo).Distinct().ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ViewDetailPromotionVoucherCouponModel>();
            }
        }

        public List<ViewDetailPromotionVoucherCouponModel> GetOrderDetailPromotionByPosterminal(string fromDate, string toDate, string serverIP, string storeNo, string posTerminal, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<ViewDetailPromotionVoucherCouponModel>("[dbo].[GET_DETAIL_ORDER_PROMOTION_BY_STORE] @FromDate, @ToDate, @StoreNo, @Posterminal, @OrderNo, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate ?? string.Empty),
                        new SqlParameter("@ToDate", toDate ?? string.Empty),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@Posterminal", posTerminal ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ViewDetailPromotionVoucherCouponModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ViewDetailPromotionVoucherCouponModel>();
            }
        }

        public List<ExportViewDetailPromotionVoucherCouponModel> Export_Get_Detail_Promotion_List_By_Posterminal(string fromDate, string toDate, string serverIP, string storeNo, string posTerminal, string orderNo)
        {
            try
            {
                var ipServerRead = serverIP.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;

                    //var data = db.Database.SqlQuery<ExportViewDetailPromotionVoucherCouponModel>("[dbo].[GET_DETAIL_PROMOTION_VCP_VIEW_BY_STORE_EXP] @FromDate, @ToDate, @StoreNo, @Posterminal, @OrderNo",
                    //    new SqlParameter("@FromDate", fromDate ?? string.Empty),
                    //    new SqlParameter("@ToDate", toDate ?? string.Empty),
                    //    new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                    //    new SqlParameter("@Posterminal", posTerminal ?? string.Empty),
                    //    new SqlParameter("@OrderNo", orderNo ?? string.Empty)).ToList();

                    var data = db.Database.SqlQuery<ExportViewDetailPromotionVoucherCouponModel>("[dbo].[GET_DETAIL_ORDER_PROMOTION_BY_STORE_EXP] @FromDate, @ToDate, @StoreNo, @Posterminal, @OrderNo",
                        new SqlParameter("@FromDate", fromDate ?? string.Empty),
                        new SqlParameter("@ToDate", toDate ?? string.Empty),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@Posterminal", posTerminal ?? string.Empty),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportViewDetailPromotionVoucherCouponModel>();
                    }
                    
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportViewDetailPromotionVoucherCouponModel>();
            }
        }

        public List<PaymentDetailOrderResponseModel> GetPaymentDetailOrderList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerReadByStore(storeNo);
                var ipServerRead = ipServer.Split('\\')[0];
                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<PaymentDetailOrderResponseModel>("[dbo].[GetDetailPaymentOrderList] @OrderNo",
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<PaymentDetailOrderResponseModel>();
                    }

                    totalRecord = data.Count();
                    var listData = data.Skip(skip).Take(take).ToList();
                    var result = (from x in listData
                                  select new PaymentDetailOrderResponseModel
                                  {
                                      OrderNo = x.OrderNo,
                                      TenderType = x.TenderType,
                                      TenderTypeName = x.TenderTypeName,
                                      AmountTendered = x.AmountTendered,
                                      ReferenceNo = x.ReferenceNo,
                                      ApprovalCode = x.ApprovalCode,
                                      BankPOSCode = x.BankPOSCode,
                                      BankCardType = x.BankCardType,
                                      IsOnline = x.IsOnline
                                  }).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<PaymentDetailOrderResponseModel>();
            }
        }

        public ResultResponseModel UpdateSalesType(UpdateSalesTypeForOrderModel req)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(req.StoreNo);
                var ipAddress = ipServer.Split('\\')[0];

                using (var db = new CentralSalesStagingContainer(ipAddress))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.TransHeaders.FirstOrDefault(a => a.OrderNo == req.OrderNo);
                    if (data != null)
                    {
                        data.SalesType = req.SalesType;
                        db.SaveChanges();
                    }
                }

                using (var db1 = new CentralSalesContainer(ipAddress))
                {
                    db1.Database.CommandTimeout = 2 * 60;
                    var data1 = db1.TransHeaders.FirstOrDefault(a => a.OrderNo == req.OrderNo);
                    if (data1 == null)
                    {
                        return new ResultResponseModel
                        {
                            Item1 = req.OrderNo,
                            Item2 = req.SalesType,
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Đơn hàng {req.OrderNo} này không có trong table: TransHeaders. Vui lòng kiểm tra lại"
                        };
                    }

                    data1.SalesType = req.SalesType;
                    db1.SaveChanges();
                }

                return new ResultResponseModel
                {
                    Item1 = req.OrderNo,
                    Item2 = req.SalesType,
                    Status = Model.Enums.ResultEnum.Success,
                    Message = "Cập nhật thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        // ---- WIN LIFE ----
        public List<OrderListWinLifeResponseModel> GetOrderListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string posID, string orderType, string transactionType, string textSearchOrder, string chanelSales, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new INBOUNDContainer()) 
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<OrderListWinLifeResponseModel>("[dbo].[WLF_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @PosID, @OrderType, @TransactionType, @TextSearch, @ChanelSales, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosID", posID ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearchOrder ?? string.Empty),
                        new SqlParameter("@ChanelSales", chanelSales ?? string.Empty),
                        new SqlParameter("@Export", '1'),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OrderListWinLifeResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<OrderListWinLifeResponseModel>();
            }
        }

        public List<ExportOrderListWinLifeResponseModel> ExportExcelOrderListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string posID, string orderType, string transactionType, string textSearchOrder, string chanelSales)
        {
            try
            {
                using (var db = new INBOUNDContainer()) 
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Database.SqlQuery<ExportOrderListWinLifeResponseModel>("[dbo].[WLF_ORDER_SALES_LIST_V2] @FromDate, @ToDate, @StoreNo, @PosID, @OrderType, @TransactionType, @TextSearch, @ChanelSales, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosID", posID ?? string.Empty),
                        new SqlParameter("@OrderType", orderType ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearchOrder ?? string.Empty),
                        new SqlParameter("@ChanelSales", chanelSales ?? string.Empty),
                        new SqlParameter("@Export", '2'),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportOrderListWinLifeResponseModel>();
            }
        }
        public List<DetailOrderListWinLifeResponseModel> GetDetailOrderListWinLife(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50)
        {
            try
            {
                using (var db = new INBOUNDContainer()) 
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<DetailOrderListWinLifeResponseModel>("[dbo].[WLF_GET_DETAIL_ORDER_SALES_LIST] @OrderNo",
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailOrderListWinLifeResponseModel>();
                    }

                    totalRecord = data.Count();
                    var listData = data.Skip(skip).Take(take).ToList();
                    var result = (from x in listData
                                  select new DetailOrderListWinLifeResponseModel
                                  {
                                      OrderNo = x.OrderNo,
                                      LineNo = x.LineNo,
                                      ItemNo = x.ItemNo,
                                      ItemNoSAP = x.ItemNoSAP,
                                      Description = x.Description,
                                      UnitOfMeasure = x.UnitOfMeasure,
                                      Quantity = x.Quantity,
                                      UnitPrice = x.UnitPrice,
                                      DiscountAmount = x.DiscountAmount,
                                      LineAmountIncVAT = x.LineAmountIncVAT,
                                      VATAmount = x.VATAmount
                                  }).OrderBy(x => x.LineNo).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailOrderListWinLifeResponseModel>();
            }
        }

        public List<PaymentDetailOrderWinLifeResponseModel> GetPaymentDetailOrderListWinLife(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50)
        {
            try
            {
                using (var db = new INBOUNDContainer())
                {
                    db.Database.CommandTimeout = 15 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<PaymentDetailOrderWinLifeResponseModel>("[dbo].[WLF_DETAIL_PAYMENT_ORDER_SALE_LIST] @OrderNo",
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<PaymentDetailOrderWinLifeResponseModel>();
                    }

                    totalRecord = data.Count();
                    var listData = data.OrderBy(x => x.TenderType).ToList();
                    var result = (from x in listData
                                  select new PaymentDetailOrderWinLifeResponseModel
                                  {
                                      OrderNo = x.OrderNo,
                                      TenderType = x.TenderType,
                                      AmountTendered = x.AmountTendered,
                                      ReferenceNo = x.ReferenceNo
                                  }).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<PaymentDetailOrderWinLifeResponseModel>();
            }
        }



































































    }
}
