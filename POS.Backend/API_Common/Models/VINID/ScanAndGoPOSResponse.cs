using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class ScanAndGoPOSResponse
    {
        public string OrderNo { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public DateTime? OrderDate { get; set; }
        public double? Amount { get; set; }
        public double? AmountDeducted { get; set; }
        public double? VinIdPaid { get; set; }
        public double? AmountCollect { get; set; }
        public string VoucherId { get; set; }
        public string VoucherName { get; set; }
        public string VinIdCardNumber { get; set; }
        public string VinIdCardName { get; set; }
        public double? DeliveryType { get; set; }
        public string DeliveryTypeName { get; set; }
        public double? Status { get; set; }
        public string Address { get; set; }
        public double? Fee { get; set; }
        public bool? IsExpired { get; set; }
        public string SiteCode { get; set; }
        public string PricingStoreCode { get; set; }
        public int? VinIdEarn { get; set; }
        public bool? HasVatInvoice { get; set; }
        public List<POSScanPayments> ListPayments { get; set; }
        public List<POSScanItems> ListItems { get; set; }
        public POSBillInfo BillingInfo { get; set; }
    }
    public class POSScanPayments
    {
        public string PaymentMethod { get; set; }
        public double? PaidAmount { get; set; }
        public string TransactionId { get; set; }
    }
    public class POSScanItems
    {
        public string Id { get; set; }
        public string ArticleId { get; set; }
        public string ArticleName { get; set; }
        public double? SoldPrice { get; set; }
        public double? Quantity { get; set; }
        public string UnitCode { get; set; }
        public string ProductBarcode { get; set; }
        public string OriginBarcode { get; set; }
        public string ItemType { get; set; }
        public double? VatGroup { get; set; }
        public double? VatPercent { get; set; }
        public double? NetPrice { get; set; }
        public string PromotionCode { get; set; }
        public double? PromotionValue { get; set; }
        public double? UnitQuantity { get; set; }
        public double? SaleQuantity { get; set; }
        public bool? IsFreshfood { get; set; }
        public double? UnitSoldPrice { get; set; }
        public double? TotalSoldPrice { get; set; }

    }
    public class POSBillInfo
    {
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string TaxCode { get; set; }
        public string PhoneNumber { get; set; }
        public string SiteCode { get; set; }

    }
}
