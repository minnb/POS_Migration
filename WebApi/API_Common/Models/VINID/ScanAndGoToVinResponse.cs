using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class ScanAndGoToVinResponse
    {
        public ScanMeta meta { get; set; }
        public ScanData data { get; set; }
    }
    public class ScanMeta
    {
        public string message { get; set; }
        public int code { get; set; }
    }
    public class ScanData
    {
        public string order_no { get; set; }
        public string customer_id { get; set; }
        public string customer_name { get; set; }
        public string customer_phone { get; set; }
        public DateTime? order_date { get; set; }
        public double? amount { get; set; }
        public double? amount_deducted { get; set; }
        public double? vin_id_paid { get; set; }
        public double? amount_collect { get; set; }

        public string voucher_id { get; set; }
        public string voucher_name { get; set; }
        public string vin_id_card_number { get; set; }
        public string vin_id_card_name { get; set; }
        public double? delivery_type { get; set; }
        public string delivery_type_name { get; set; }
        public double? status { get; set; }
        public string address { get; set; }
        public double? fee { get; set; }
        public bool? is_expired { get; set; }
        public string site_code { get; set; }
        public string pricing_store_code { get; set; }
        public int? vin_id_earn { get; set; }
        public List<ScanPayments> payments { get; set; }
        public List<ScanItems> items { get; set; }
        public bool? has_vat_invoice { get; set; }
        public BillInfo billing_info { get; set; }
    }


    public class ScanPayments
    {
        public string payment_method { get; set; }
        public double? paid_amount { get; set; }
        public string transaction_id { get; set; }
    }
    public class ScanItems
    {
        public string id { get; set; }        
        public string article_id { get; set; }
        public string article_name { get; set; }
        public double? sold_price { get; set; }
        public double? quantity { get; set; }
        public string unit_code { get; set; }
        public string product_barcode { get; set; }
        public string origin_barcode { get; set; }
        public string item_type { get; set; }
        public double? vat_group { get; set; }
        public double? vat_percent { get; set; }
        public double? net_price { get; set; }
        public string promotion_code { get; set; }
        public double? promotion_value { get; set; }
        public string image_url { get; set; }

        public double? unit_quantity { get; set; }
        public double? sale_quantity { get; set; }
        public bool? is_freshfood { get; set; }
        public double? unit_sold_price { get; set; }
        public double? total_sold_price { get; set; }

    }
    public class BillInfo
    {
        public string customer_name { get; set; }
        public string address { get; set; }
        public string email { get; set; }
        public string company_name { get; set; }
        public string tax_code { get; set; }
        public string phone_number { get; set; }
        public string site_code { get; set; }

    }
}
