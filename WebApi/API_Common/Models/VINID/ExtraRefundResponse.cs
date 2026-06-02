using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class ExtraRefundResponse
    {
        public ExtRMeta meta { get; set; }
        public ExtRData data { get; set; }
    }
    public class ExtRMeta
    {
        public int code { get; set; }
        public string message { get; set; }
    }
    public class ExtRData
    {
        public string receipt_number { get; set; }
        public string transaction_type { get; set; }
        public string store_code { get; set; }
        public string pos_code { get; set; }
        public string merchant_id { get; set; }
        public string terminal_id { get; set; }
        public string cashier_code { get; set; }
        public float? transaction_time { get; set; }
        public float? business_time { get; set; }
        public string customer_name { get; set; }
        public string vinid_card_number { get; set; }
        public string vinid_csn { get; set; }
        public float? total_bill_amount { get; set; }
        public string reference_number { get; set; }
        public float? earn_by_default { get; set; }
        public float? refund_amount { get; set; }
        public float? refund_point_by_default { get; set; }
        public string employee_code { get; set; }
        public string company_code { get; set; }
        public float? extra_refund_by_items { get; set; }
        public float? extra_refund_by_campaign { get; set; }
        public string original_receipt_number { get; set; }
        public string original_pos_number { get; set; }
        public string original_reference_number { get; set; }
     
        public List<BillRLineResponse> bill_lines { get; set; }
    }
    public class BillRLineResponse
    {
        public float? record_no { get; set; }
        public string barcode { get; set; }
        public string article { get; set; }
        public string article_name { get; set; }
        public string uom { get; set; }
        public float? quantity { get; set; }
        public float? sale_price { get; set; }
        public float? amount { get; set; }
        public float? discount_amount { get; set; }
        public float? line_amount { get; set; }
        public float? extra_quantity_refund { get; set; }
        public float? extra_amount_refund { get; set; }
    }
}
