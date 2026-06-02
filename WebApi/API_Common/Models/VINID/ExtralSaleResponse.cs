using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class ExtralSaleResponse
    {
        public ExtSMeta meta { get; set; }
        public ExtSData data { get; set; }
    }
    public class ExtSMeta
    {
        public int code { get; set; }       
        public string message { get; set; }
    }
    public class ExtSData
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
        public float? extra_earn_by_items { get; set; }
        public float? extra_earn_by_campaign { get; set; }
        public bool? over_quota { get; set; }
        public string employee_code { get; set; }
        public string company_code { get; set; }
        public List<BillLineResponse> bill_lines { get; set; }
    }
    public class BillLineResponse
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
        public float? extra_quantity_earn { get; set; }
        public float? extra_amount_earn { get; set; }
    }
}
