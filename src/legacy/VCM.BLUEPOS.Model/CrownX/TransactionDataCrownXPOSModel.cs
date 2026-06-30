using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.CrownX
{
    public class TransactionDataCrownXPOSModel
    {
        public long ID { get; set; }
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string DateTimeStr { get; set; }
        public string CardNumber { get; set; }
        public string QRCode { get; set; }
        public string OrderNo { get; set; }
        public string OrigOrderNo { get; set; }
        public string TransactionName { get; set; }
        public double? AmountOnOrderPOS { get; set; }
        public double? GrossTransactionAmountCX { get; set; }
        public double? RefundAmount { get; set; }
        public double? SpendPoints { get; set; }
        public double? AwardPointCX { get; set; }
        public double? AwardAmountCX { get; set; }
        public double? RedeemAmountCX { get; set; }
        public int Total { get; set; }

    }
 
    public class ExportExcelTransactionDataCrownXPOSModel
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string DateTimeStr { get; set; }
        public string CardNumber { get; set; }
        public string OrderNo { get; set; }
        public string OrigOrderNo { get; set; }
        public string TransactionName { get; set; }
        public Nullable<double> AmountOnOrderPOS { get; set; }
        public Nullable<double> RefundAmount { get; set; }
        public Nullable<double> AwardPointCX { get; set; }          // Diem tich
        public Nullable<double> SpendPoints { get; set; }           // Diem tieu
        public Nullable<double> AwardAmountCX { get; set; }         // Tien tich diem
        public Nullable<double> RedeemAmountCX { get; set; }        // Tien tieu diem
    }

    public class TransPointLineResponseModel
    {
        public long ID { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string OrderDateStr { get; set; }
        public string OrderNo { get; set; }
        public string OrderType { get; set; }
        public string MerchantId { get; set; }
        public string OrigOrder { get; set; }
        public string CardNumber { get; set; }
        public string ReceiptNumber { get; set; }
        public Nullable<double> EarnPoints { get; set; }
        public Nullable<double> RedeemPoints { get; set; }
        public string MemberName { get; set; }
        public string CardLevel { get; set; }
        public string MemberNumber { get; set; }
        public int Total { get; set; }

    }

    public class ExportExcelTransPointLineResponseModel
    {
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string OrderDateStr { get; set; }
        public string OrderNo { get; set; }
        public string OrderType { get; set; }
        public string MerchantId { get; set; }
        public string OrigOrder { get; set; }
        public string CardNumber { get; set; }
        public string ReceiptNumber { get; set; }
        public Nullable<double> EarnPoints { get; set; }
        public Nullable<double> RedeemPoints { get; set; }
        public string MemberName { get; set; }
        public string CardLevel { get; set; }
        public string MemberNumber { get; set; }
        
    }

    public class TransactionByMemberStampModel
    {        
        public string BusinessDate { get; set; }        
        public string OrderNo { get; set; }
        public string OrderType { get; set; }
        public string ReturnedOrderNo { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string MemberCard { get; set; }
        public int? QuantityEarn { get; set; }
        public int? QuantityRedeem { get; set; }
        public int Total { get; set; }
    }


    public class ExportExcelTransactionByMemberStampModel
    {       
        public string BusinessDate { get; set; }
        public string OrderNo { get; set; }
        public string OrderType { get; set; }
        public string ReturnedOrderNo { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string MemberCard { get; set; }
        public int? QuantityEarn { get; set; }
        public int? QuantityRedeem { get; set; }
    }
    public class DetailMemberEarnTransactionLineModel
    {     
        public string BusinessDate { get; set; }
        public string OrderNo { get; set; }
        public string OrderType { get; set; }
        public string ReturnedOrderNo { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string MemberCard { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Uom { get; set; }
        public int? SaleQty { get; set; }
        public int? PointEarn { get; set; }   // tich tem
        public int Total { get; set; }
    }
    public class ExportExcel_DetailMemberEarnTransactionLineModel
    {
        public string BusinessDate { get; set; }
        public string OrderNo { get; set; }
        public string OrderType { get; set; }
        public string ReturnedOrderNo { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string MemberCard { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Uom { get; set; }
        public int? SaleQty { get; set; }
        public int? PointEarn { get; set; }   // tich tem
    }
    public class POPupMemberCardModel
    {
        public string OrderNo { get; set; }         
        public string CustomerName { get; set; } 
        public string MemberCardNo { get; set; } // Số thẻ Phuc Long
    }

    public class StaffMemberRemnResponeModel
    {
        public string CompCode { get; set; }
        public string StaffCode { get; set; }
        public string StaffName { get; set; }
        public string PhoneNumber { get; set; }
        public string Blocked { get; set; }
        public DateTime? CrtDate { get; set; }
        public DateTime? ChgDate { get; set; }
        public string ClubCode {get; set;}
        public string Month { get; set; }
        public Nullable<decimal> InitQuota { get; set; }
        public Nullable<decimal> RemainQuota { get; set; }
        public Nullable<decimal> QuotaUsed { get; set; }
        public int Total { get; set; }

    }
    public class ReportStaffMemberRemnModel
    {
        public string StoreNo { get; set; }
        public string PosNo { get; set; }
        public string OrderNo { get; set; }
        public string OrderDate { get; set; }
        public string CrtDateStr { get; set; }
        public string TimeDateStr { get; set; }
        public Nullable<double> AmountInclVAT { get; set; }  // Tổng số tiền
        public Nullable<decimal> Amount { get; set; }
        public string OrderType { get; set; }
        public string ReturnedOrderNo { get; set; }
        public string CompCode { get; set; }
        public string StaffCode { get; set; }
        public string StaffName { get; set; }
        public string PhoneNumber { get; set; }
        public string Blocked { get; set; }
        public DateTime? CrtDate { get; set; }
        public DateTime? ChgDate { get; set; }
        public string ClubCode { get; set; }
        public string Month { get; set; }
        public Nullable<decimal> InitQuota { get; set; }
        public Nullable<decimal> RemainQuota { get; set; }
        public Nullable<decimal> QuotaUsed { get; set; }
        public string CashierID { get; set; }
        public int Total { get; set; }

    }
    public class ExportReportStaffMemberRemnModel
    {
        public string CrtDateStr { get; set; }
        public string TimeDateStr { get; set; }
        public string StoreNo { get; set; }
        public string PosNo { get; set; }
        public string OrderNo { get; set; }
        public string OrderType { get; set; }
        public string ReturnedOrderNo { get; set; }
        public Nullable<double> AmountInclVAT { get; set; }  // Tổng số tiền  
        public string StaffCode { get; set; }       
        public string PhoneNumber { get; set; }       
        public string ClubCode { get; set; }
        public string CashierID { get; set; }

    }

    public class DetailReportStaffMemberRemnModel
    {
        public int LineNo { get; set; }
        public string OrderNo { get; set; }
        public string Barcode { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public Nullable<double> DiscountAmount { get; set; }
        public Nullable<double> VATAmount { get; set; }
        public Nullable<double> LineAmountIncVAT { get; set; }
        public string OfferNo { get; set; }
        public int Total  { get; set; }
    }

    public class GetOrderListByStaffRemnModel
    {
        public string StoreNo { get; set; }
        public string OrderNo { get; set; }
        public string OrderDate { get; set; }
        public string CrtDateStr { get; set; }
        public Nullable<double> AmountInclVAT { get; set; }  // Tổng số tiền
        public string OrderType { get; set; }
        public string ReturnedOrderNo { get; set; }
        public string StaffCode { get; set; }
        public string PhoneNumber { get; set; }
        public string ClubCode { get; set; }
        public string Month { get; set; }
        public int Total { get; set; }

    }

}
