using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Data;
using VCM.BLUEPOS.Data.Order;
using VCM.BLUEPOS.Model.Order;
using VCM.BLUEPOS.Model.Order.OrderWinLifeModel;
using VCM.BLUEPOS.Model.Order.PrintInvoiceOrderSalesModel;

namespace VCM.BLUEPOS.Business.Order
{
    public interface IOrderBLO
    {
        List<OrderListResponseModel> GetOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string orderType, string salesType, string userID, string textSearchOrder, string textSearchItem, float fromAmount, float toAmount, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportOrderListResponseModel> ExportExcelOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string orderType, string salesType, string userID, string textSearch, string textSearchItem, float fromAmount, float toAmount);
        List<DetailOrderListResponseModel> GetDetailOrderList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50);
        List<ViewDetailPromotionVoucherCouponModel> GetOrderDetailPromotionList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50);      
        List<ViewDetailPromotionVoucherCouponModel> GetOrderDetailPromotionByPosterminal(string fromDate, string toDate, string serverIP, string storeNo, string posTerminal, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportViewDetailPromotionVoucherCouponModel> Export_Get_Detail_Promotion_List_By_Posterminal(string fromDate, string toDate, string serverIP, string storeNo, string posTerminal, string orderNo);
        List<PaymentDetailOrderResponseModel> GetPaymentDetailOrderList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50);
        ResultResponseModel UpdateSalesType(UpdateSalesTypeForOrderModel req);

        /*----- Win Life ----*/
        List<OrderListWinLifeResponseModel> GetOrderListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string posID, string orderType, string transactionType, string textSearchOrder, string chanelSales, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportOrderListWinLifeResponseModel> ExportExcelOrderListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string posID, string orderType, string transactionType, string textSearchOrder, string chanelSales);
        List<DetailOrderListWinLifeResponseModel> GetDetailOrderListWinLife(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50);
        List<PaymentDetailOrderWinLifeResponseModel> GetPaymentDetailOrderListWinLife(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50);




    }
    public class OrderBLO : IOrderBLO
    {
        private readonly IOrderData _data;
        public OrderBLO()
        {
            _data = new OrderData();
        }
        public List<OrderListResponseModel> GetOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string orderType, string salesType, string userID, string textSearchOrder, string textSearchItem, float fromAmount, float toAmount, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetOrderList(fromDate, toDate, serverIP, storeNo, posID, orderType, salesType, userID, textSearchOrder, textSearchItem, fromAmount, toAmount, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportOrderListResponseModel> ExportExcelOrderList(DateTime fromDate, DateTime toDate, string serverIP, string storeNo, string posID, string orderType, string salesType, string userID, string textSearch, string textSearchItem, float fromAmount, float toAmount)
        {
            return _data.ExportExcelOrderList(fromDate, toDate, serverIP, storeNo, posID, orderType, salesType, userID, textSearch, textSearchItem, fromAmount, toAmount);
        }
        public List<DetailOrderListResponseModel> GetDetailOrderList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50)
        {
            return _data.GetDetailOrderList(storeNo, orderNo, out totalRecord, skip, take);
        }

        // --- 16/10/2023 ---

        //public List<ViewDetailPromotionVoucherCouponModel> Get_Detail_Promotion_Voucher_Coupon_List(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50)
        //{
        //    return _data.Get_Detail_Promotion_Voucher_Coupon_List(storeNo, orderNo, out totalRecord, skip, take);
        //}

        public List<ViewDetailPromotionVoucherCouponModel> GetOrderDetailPromotionList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50)
        {
            return _data.GetOrderDetailPromotionList(storeNo, orderNo, out totalRecord, skip, take);
        }

        //public List<ViewDetailPromotionVoucherCouponModel> Get_Detail_Promotion_Voucher_Coupon_List_By_Posterminal(string fromDate, string toDate, string serverIP, string storeNo, string posTerminal, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        //{
        //    return _data.Get_Detail_Promotion_Voucher_Coupon_List_By_Posterminal(fromDate, toDate, serverIP, storeNo, posTerminal, orderNo, out totalRecord, pageIndex, pageSize);
        //}

        // 04/03/2024 :theo store ma user da duoc phan quyen
        public List<ViewDetailPromotionVoucherCouponModel> GetOrderDetailPromotionByPosterminal(string fromDate, string toDate, string serverIP, string storeNo, string posTerminal, string orderNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetOrderDetailPromotionByPosterminal(fromDate, toDate, serverIP, storeNo, posTerminal, orderNo, out totalRecord, pageIndex, pageSize);
        }

        public List<ExportViewDetailPromotionVoucherCouponModel> Export_Get_Detail_Promotion_List_By_Posterminal(string fromDate, string toDate, string serverIP, string storeNo, string posTerminal, string orderNo)
        {
            return _data.Export_Get_Detail_Promotion_List_By_Posterminal(fromDate, toDate, serverIP, storeNo, posTerminal, orderNo);
        }
        public List<PaymentDetailOrderResponseModel> GetPaymentDetailOrderList(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50)
        {
            return _data.GetPaymentDetailOrderList(storeNo, orderNo, out totalRecord, skip, take);
        }
        public ResultResponseModel UpdateSalesType(UpdateSalesTypeForOrderModel req)
        {
            return _data.UpdateSalesType(req);
        }
        public List<OrderListWinLifeResponseModel> GetOrderListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string posID, string orderType, string transactionType, string textSearchOrder, string chanelSales, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
          return _data.GetOrderListWinLife(fromDate, toDate, storeNo, posID, orderType, transactionType, textSearchOrder, chanelSales, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportOrderListWinLifeResponseModel> ExportExcelOrderListWinLife(DateTime fromDate, DateTime toDate, string storeNo, string posID, string orderType, string transactionType, string textSearchOrder, string chanelSales)
        {
            return _data.ExportExcelOrderListWinLife(fromDate, toDate, storeNo, posID, orderType, transactionType, textSearchOrder, chanelSales);
        }
        public List<DetailOrderListWinLifeResponseModel> GetDetailOrderListWinLife(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50)
        {
            return _data.GetDetailOrderListWinLife(storeNo, orderNo, out totalRecord, skip, take);
        }
        public List<PaymentDetailOrderWinLifeResponseModel> GetPaymentDetailOrderListWinLife(string storeNo, string orderNo, out int totalRecord, int skip = 0, int take = 50)
        {
            return _data.GetPaymentDetailOrderListWinLife(storeNo, orderNo, out totalRecord, skip, take);
        }



    }
}
