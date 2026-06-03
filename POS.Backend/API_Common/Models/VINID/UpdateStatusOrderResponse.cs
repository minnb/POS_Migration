using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class UpdateStatusOrderResponse
    {
        public StatusMeta meta { get; set; }
        public StatusData data { get; set; }
    }
    public class StatusMeta
    {
        public string message { get; set; }
        public int code { get; set; }
    }
    public class StatusData
    {
        public string receipt_number { get; set; }
        public string transaction_type { get; set; }
        public string store_code { get; set; }
        public string pos_code { get; set; }   

        public string cashier_code { get; set; }
        public string merchant_id { get; set; }
        public string terminal_id { get; set; }

        public string customer_name { get; set; }     
        public string vinid_card_number { get; set; }    
        public string vinid_csn { get; set; }
        public double? total_bill_amount { get; set; }     
        public string reference_number { get; set; }
        public int? extra_earn_by_items { get; set; }
        public int? extra_earn_by_campaign { get; set; }

        public int? vin_id_earn { get; set; }
        public bool? is_earn_success { get; set; }
        public bool? over_quota { get; set; }
        public List<StatusBillLine> bill_lines { get; set; }

    }
    public class StatusBillLine
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
        public float? vcm_unit_price { get; set; }
    }
}
