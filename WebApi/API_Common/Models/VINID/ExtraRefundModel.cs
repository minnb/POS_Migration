using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class ExtraRefundModel
    {
        public string receipt_number { get; set; }
        public string transaction_type { get; set; }
        public string store_code { get; set; }
        public string pos_code { get; set; }
        public string merchant_id { get; set; }
        public string terminal_id { get; set; }
        public string cashier_code { get; set; }
        public Int64 transaction_time { get; set; }
        public Int64 business_time { get; set; }
        public float? total_bill_amount { get; set; }
        public string original_receipt_number { get; set; }
        public string original_pos_number { get; set; }
        public string original_reference_number { get; set; }
        public string reference_number { get; set; }

        public float? earn_by_default { get; set; }
        public float? refund_amount { get; set; }
        public float? refund_point_by_default { get; set; }
        public List<BillLineRefund> bill_lines { get; set; }
        public string signature { get; set; }
    }
    public class ExtraRefundPosRequestModel
    {
        public string CardNumber { get; set; }
        public string CustomerName { get; set; }
        public string ReceiptNumber { get; set; }
        public string OrderNo { get; set; } = "";
        public string StoreCode { get; set; }
        public string PosTerminal { get; set; }
        public string CashierCode { get; set; }
        public float? TotalBillAmount { get; set; }
        public string OriginalReceiptNumber { get; set; }
        public string OriginalPosNumber { get; set; }
        public string OriginalReferenceNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public float? EarnByDefault { get; set; }//số điểm 3% đã tich
        public float? RefundAmount { get; set; }//Tổng giá trị refund
        public float? RefundPointDefault { get; set; }//Số điểm cần thu hồi theo chính sách 3%
        public List<BillLineRefundRequest> BillLines { get; set; }
    }

    public class BillLineRefundRequest
    {
        public double RecordNo { get; set; }
        public string Barcode { get; set; }
        public string Article { get; set; }
        public string ArticleName { get; set; }
        public string UOM { get; set; }
        public float? Quantity { get; set; }
        public float? RefundQuantity { get; set; }
        public float? SalePrice { get; set; }
        public float? Amount { get; set; }
        public float? DiscountAmount { get; set; }
        public float? LineAmount { get; set; }
    }

    public class BillLineRefund
    {
        public float? record_no { get; set; }
        public string barcode { get; set; }
        public string article { get; set; }
        public string article_name { get; set; }
        public string uom { get; set; }
        public float? quantity { get; set; }
        public float? refund_quantity { get; set; }
        public float? sale_price { get; set; }
        public float? amount { get; set; }
        public float? discount_amount { get; set; }
        public float? line_amount { get; set; }
    }
}
