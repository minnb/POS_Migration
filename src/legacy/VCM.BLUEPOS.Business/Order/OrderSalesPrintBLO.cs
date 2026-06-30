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
using VCM.BLUEPOS.Model.Order.PrintInvoiceOrderSalesModel;

namespace VCM.BLUEPOS.Business.Order
{
    public interface IOrderSalesPrintBLO
    {
        List<TransHeaderPrintInvoiceOrderSalesModel> GetTransHeaderByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo);
        List<TransLinePrintInvoiceOrderSalesModel> GetTransLineByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo);
        List<TranBluePointModel> GetTransBluePointByPrintInvoiceOrderSales(string posID, string orderNo);
        List<CouponInforModel> GetListCouponByPrintInvoiceOrderSales(string posID, string orderNo);
        List<PromotionInforModel> GetListPromotionByPrintInvoiceOrderSales(string posID, string orderNo);
        List<PaymentEntryListModel> GetListPaymentEntryByPrintInvoiceOrderSales(string posID, string orderNo);
        List<TenderTypePaymentModel> GetListTenderTypePayment(string posID, string orderNo);
        List<TenderTypeSetUpModel> GetListTenderTypeSetUp(string posID, string orderNo);
        List<TransCpnVchIssueModel> GetTransCpnVchIssue(string posID, string orderNo);
        List<MarketingResponseModel> GetMarketingDataList(string posID, string storeNo);
        List<CompanyInforModel> GetCompanyInforList(string posID, string storeNo);
        List<PaymentEntryReturnVoucherListModel> GetReturnVoucherAmountByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo);
        List<ReasonReturnProductModel> GetReasonCodeByPrintInvoiceOrderSales(string storeNo, string posID, string reasonCode);
        List<VinIDRateByLoyatylRateModel> GetVinIDRateByLoyaltyRate(string storeNo, string posID);
        List<ListPOSDataSetupModel> GetPOSDataSetup(string storeNo, string posID);


    }

    public class OrderSalesPrintBLO : IOrderSalesPrintBLO
    {
        private readonly IOrderSalesPrintData _data;
        public OrderSalesPrintBLO()
        {
            _data = new OrderSalesPrintData();
        }
        public List<TransHeaderPrintInvoiceOrderSalesModel> GetTransHeaderByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo)
        {
            return _data.GetTransHeaderByPrintInvoiceOrderSales(storeNo, posID, orderNo);
        }
        public List<TransLinePrintInvoiceOrderSalesModel> GetTransLineByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo)
        {
            return _data.GetTransLineByPrintInvoiceOrderSales(storeNo, posID, orderNo);
        }
        public List<TranBluePointModel> GetTransBluePointByPrintInvoiceOrderSales(string posID, string orderNo)
        {
            return _data.GetTransBluePointByPrintInvoiceOrderSales(posID, orderNo);
        }
        public List<CouponInforModel> GetListCouponByPrintInvoiceOrderSales(string posID, string orderNo)
        {
            return _data.GetListCouponByPrintInvoiceOrderSales(posID, orderNo);
        }    
        public List<PromotionInforModel> GetListPromotionByPrintInvoiceOrderSales(string posID, string orderNo)
        {
            return _data.GetListPromotionByPrintInvoiceOrderSales(posID, orderNo);
        }
        public List<PaymentEntryListModel> GetListPaymentEntryByPrintInvoiceOrderSales(string posID, string orderNo)
        {
            return _data.GetListPaymentEntryByPrintInvoiceOrderSales(posID, orderNo);
        }
        public List<TenderTypePaymentModel> GetListTenderTypePayment(string posID, string orderNo)
        {
            return _data.GetListTenderTypePayment(posID, orderNo);
        }
        public List<TenderTypeSetUpModel> GetListTenderTypeSetUp(string posID, string orderNo)
        {
            return _data.GetListTenderTypeSetUp(posID, orderNo);
        }
        public List<TransCpnVchIssueModel> GetTransCpnVchIssue(string posID, string orderNo)
        {
            return _data.GetTransCpnVchIssue(posID, orderNo);
        }
        public List<MarketingResponseModel> GetMarketingDataList(string posID, string storeNo)
        {
            return _data.GetMarketingDataList(posID, storeNo);
        }
        public List<CompanyInforModel> GetCompanyInforList(string posID, string storeNo)
        {
            return _data.GetCompanyInforList(posID, storeNo);
        }
        public List<PaymentEntryReturnVoucherListModel> GetReturnVoucherAmountByPrintInvoiceOrderSales(string storeNo, string posID, string orderNo)
        {
            return _data.GetReturnVoucherAmountByPrintInvoiceOrderSales(storeNo, posID, orderNo);
        }
        public List<ReasonReturnProductModel> GetReasonCodeByPrintInvoiceOrderSales(string storeNo, string posID, string reasonCode)
        {
            return _data.GetReasonCodeByPrintInvoiceOrderSales(storeNo, posID, reasonCode);
        }
        public List<VinIDRateByLoyatylRateModel> GetVinIDRateByLoyaltyRate(string storeNo, string posID)
        {
            return _data.GetVinIDRateByLoyaltyRate(storeNo, posID);
        }
        public List<ListPOSDataSetupModel> GetPOSDataSetup(string storeNo, string posID)
        {
            return _data.GetPOSDataSetup(storeNo, posID);
        }







    }
}
