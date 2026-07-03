using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Order.PrintInvoiceOrderSalesModel
{
    public class PrintInvoiceOrderSalesResponseModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string Address { get; set; }
        public string PhoneNo { get; set; }
        public string POSTerminalNo { get; set; }
        public DateTime? OrderTime { get; set; }
        public string CashierID { get; set; }
        public string ScanTime { get; set; }
        public string OrderNo { get; set; }
        public string ReferenceNo { get; set; }       // So tham chieu
        public string Ref { get; set; }
        public int? SalesIsReturn { get; set; }
        public List<TransLinePrintInvoiceOrderSalesModel> ListData { get; set; }
        public List<PromotionInforModel> ListVoucher { get; set; }
        public List<CouponInforModel> ListCoupon { get; set; }
        public List<TranBluePointModel> ListBluePoint { get; set; }
        public Tuple<List<MarketingModel>, List<MappingQRCodeModel>> MarketingList { get; set; }
        public List<CompanyInforModel> CompanyInforList { get; set; }

        public Nullable<double> TotalAmount { get; set; }                   // Tong tien thanh toan
        public Nullable<double> TotalDiscountAmount { get; set; }           // Tong tien da giam
        public Nullable<double> TotalPayment { get; set; }                  // Tong tien khach tra

        public Nullable<double> TotalPoint { get; set; }                    // Vinpoint - Điểm blue
        public Nullable<double> CampaignPoint { get; set; }                 // Diem cho nhan vien
        public Nullable<double> BluePoint { get; set; }

        public Nullable<double> RefundCashAmount { get; set; }                // Kiem tra lai
        public List<TransCpnVchIssueModel> ListCpnVchIssue { get; set; }
        public List<TenderTypePaymentModel> ListPaymentInfor { get; set; }
        public List<ListPOSDataSetupModel> ListPOSDataSetup { get; set; }

        public string PaymentAmount { get; set; }                   // tiền mặt khách thanh toán
        public double? MemberPointsRedeem { get; set; }
        public string MemberCardNo { get; set; }
        public bool? IsOfflineVinID { get; set; }

        public string Status { get; set; }
        public string Message { get; set; }
        public string Host { get; set; }

        public string ReturnVoucherNo { get; set; }
        public Nullable<double> ReturnVoucherAmount { get; set; }
        public string ReturnVoucherExpireDate { get; set; }
        public string Description { get; set; }           // ly do tra hang
        public string ReturnedOrderNo { get; set; }       // Số đơn hàng gốc
        public double? VinIDRate { get; set; }

    }

    public class TransHeaderPrintInvoiceOrderSalesModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string Address { get; set; }
        public string PhoneNo { get; set; }
        public string POSTerminalNo { get; set; }
        public string OrderNo { get; set; }
        public DateTime? OrderTime { get; set; }
        public string CashierID { get; set; }
        public double? MemberPointsEarn { get; set; }
        public double? MemberPointsRedeem { get; set; }
        public string MemberCardNo { get; set; }
        public bool? IsOfflineVinID { get; set; }
        public int? IsTenancy { get; set; }
        public string TanencyNo { get; set; }
        public string RefKey1 { get; set; }
        public string RefKey2 { get; set; }
        public int? SalesIsReturn { get; set; }
        public string ReturnVoucherNo { get; set; }
        public string ReturnedOrderNo { get; set; }

    }


    public class TransLinePrintInvoiceOrderSalesModel
    {
        public int? LineNo { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public string SerialNo { get; set; }
        public string OrderNo { get; set; }
        public string UnitOfMeasure { get; set; }
        public string CashierID { get; set; }
        public DateTime? ScanTime { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> AmountBeforeDiscount { get; set; }
        public Nullable<double> DiscountAmount { get; set; }
        public Nullable<double> VATAmount { get; set; }
        public Nullable<double> LineAmountIncVAT { get; set; }
        public Nullable<double> PrepaymentAmount { get; set; }
        public Nullable<double> MemberPointsEarn { get; set; }

    }
    public class PromotionInforModel
    {
        public Nullable<int> LineNo { get; set; }
        public string OrderNo { get; set; }
        public Nullable<int> OrderLineNo { get; set; }
        public string ItemNo { get; set; }
        public string OfferNo { get; set; }
        public string OfferType { get; set; }
        public double? Quantity { get; set; }
        public Nullable<int> DiscountType { get; set; }
        public double? DiscountAmount { get; set; }
        public string Barcode { get; set; }
        public string VoucherName { get; set; }
        public string VoucherCode { get; set; }

    }

    public class CouponInforModel
    {
        public Nullable<int> LineNo { get; set; }
        public string OrderNo { get; set; }
        public Nullable<int> OrderLineNo { get; set; }
        public string ItemNo { get; set; }
        public string OfferNo { get; set; }
        public string OfferType { get; set; }
        public double? Quantity { get; set; }
        public Nullable<int> DiscountType { get; set; }
        public double? DiscountAmount { get; set; }
        public string Barcode { get; set; }
        public string CouponCode { get; set; }
        public string CouponName { get; set; }
        public double? DiscountUnitAmount { get; set; }

    }

    public class TranBluePointModel
    {
        public Nullable<int> TransactionType { get; set; }
        public Nullable<int> LineNo { get; set; }
        public Nullable<int> OrderLineNo { get; set; }
        public string OrderNo { get; set; }
        public string ItemNo { get; set; }
        public string Uom { get; set; }
        public string Barcode { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public Nullable<double> Amount { get; set; }
        public Nullable<double> DiscountAmount { get; set; }
        public Nullable<double> LineAmount { get; set; }
        public Nullable<double> ExtraQuantityEarn { get; set; }
        public Nullable<double> ExtraAmountEarn { get; set; }
        public Nullable<double> ExtraQuantityNotEarn { get; set; }
        public Nullable<double> ExtraQuantityNotEarnReturned { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string DiscountType { get; set; }

    }

    public class MarketingModel
    {
        public Int64? ID { get; set; }
        public string ContentType { get; set; }
        public string QRLink { get; set; }
        public string Content { get; set; }
        public bool? IsActive { get; set; }
        public bool? Status { get; set; }
        public int? Order { get; set; }
        public int? CheckSumPosition { get; set; }
        public Nullable<double> FromAmount { get; set; }
        public Nullable<double> ToAmount { get; set; }
        public bool? IsCheckSum { get; set; }
        public bool? IsDynamicURL { get; set; }
        public bool? IsEncryptCheckSum { get; set; }
        public bool? RetrictPaymentMethod { get; set; }
        public Nullable<double> RetrictPaymentAmount { get; set; }
        public string Pkey { get; set; }
    }

    public class MappingQRCodeModel
    {
        public Int64? ID { get; set; }
        public string Column { get; set; }
        public string Table { get; set; }
        public bool? IsEncrypt { get; set; }
        public Int64? MarketingID { get; set; }
        public int? Position { get; set; }
        public string Value { get; set; }

    }

    public class CompanyInforModel
    {
        public string Hotline { get; set; }
        public string Website { get; set; }
    }

    public class ResultPingIPLocalModel
    {
        public bool? Status { get; set; }
        public string IPAddress { get; set; }
        public string Message { get; set; }
        public string HtmlData { get; set; }
        public bool? bIsConnected { get; set; }
    }

    public class PaymentEntryReturnVoucherListModel
    {
        public string OrderNo { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public string TenderType { get; set; }
        public Nullable<double> AmountTendered { get; set; }
        public Nullable<double> AmountInCurrency { get; set; }
        public int? SalesIsReturn { get; set; }
        public string ReferenceNo { get; set; }
        public string ReturnVoucherNo { get; set; }
        public string ReturnVoucherExpire { get; set; }

    }

    public class ReasonReturnProductModel
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    public class VinIDRateByLoyatylRateModel
    {
        public string Code { get; set; }
        public Nullable<double> Rate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Pkey { get; set; }

    }

    public class ListPOSDataSetupModel
    {
        public string Code { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string Pkey { get; set; }

    }

    public class TransCpnVchIssueModel
    {
        public string ItemName { get; set; }
        public string OrderNo { get; set; }
        public string Voucher_Type { get; set; }
        public string SerialNo { get; set; }
        public double? Voucher_Value { get; set; }
        public DateTime? Validity_From_Date { get; set; }
        public DateTime? Expiry_Date { get; set; }
    }

    public class TenderTypePaymentModel
    {
        public string Code { get; set; }
        public string TenderType { get; set; }
        public string TenderTypeName { get; set; }
        public string Description { get; set; }
        public Nullable<double> Amount { get; set; }
        public Nullable<double> AmountTendered { get; set; }
        public Nullable<double> AmountInCurrency { get; set; }
        public string CardOrAccount { get; set; }
        public string BankCardType { get; set; }
        public string ReferenceNo { get; set; }
        public string ApprovalCode { get; set; }     // Ma chuan chi

    }

    public class PaymentEntryListModel
    {
        public string OrderNo { get; set; }
        public Nullable<int> LineNo { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public string TenderType { get; set; }
        public string TenderTypeName { get; set; }
        public Nullable<double> AmountTendered { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime? PaymentTime { get; set; }
        public string StaffID { get; set; }
        public string BankPOSCode { get; set; }
        public string BankCardType { get; set; }

    }

    public class TenderTypeSetUpModel
    {
        public string Code { get; set; }
        public string PrimaryCode { get; set; }
        public string Description { get; set; }
        public int? MayBeUsed { get; set; }
        public bool? Status { get; set; }

    }

    public class MarketingResponseModel
    {
        public string ContentType { get; set; }
        public string QRLink { get; set; }
        public string Content { get; set; }
        public bool? IsActive { get; set; }
        public bool? Status { get; set; }

    }







}
