using Newtonsoft.Json;

namespace VCM.POSBLUE.Shared.DTOs;

// DTOs dùng để gọi VINID REST API (service → VINID backend).
// Tên field snake_case theo đúng contract VINID API — KHÔNG đổi.

// ── Common ─────────────────────────────────────────────────────────────────

public class VinIdMeta
{
    [JsonProperty("code")]      public int Code { get; set; }
    [JsonProperty("message")]   public string? Message { get; set; }
}

public class VinIdApiResponse<T>
{
    [JsonProperty("meta")] public VinIdMeta Meta { get; set; } = new();
    [JsonProperty("data")] public T? Data { get; set; }
}

// ── GetInfoMember — POST /internal/checkout/v1/get-loyalty-information ─────

public class VinIdInfoMemberRequest
{
    [JsonProperty("code_value")]  public string? code_value { get; set; }
    [JsonProperty("merchant_id")] public string? merchant_id { get; set; }
    [JsonProperty("terminal_id")] public string? terminal_id { get; set; }
    [JsonProperty("date")]        public string? date { get; set; }
    [JsonProperty("time")]        public string? time { get; set; }
    [JsonProperty("time_zone")]   public string? time_zone { get; set; }
}

public class VinIdInfoMemberData
{
    [JsonProperty("oe_info")] public VinIdOeInfo? oe_info { get; set; }
}

public class VinIdOeInfo
{
    [JsonProperty("member_balance")]  public VinIdMemberBalance? member_balance { get; set; }
    [JsonProperty("member_accounts")] public List<VinIdMemberAccount>? member_accounts { get; set; }
    [JsonProperty("member_profile")]  public VinIdMemberProfile? member_profile { get; set; }
    [JsonProperty("has_extra_point")] public bool has_extra_point { get; set; }
}

public class VinIdMemberBalance
{
    [JsonProperty("pool")]   public VinIdPool? pool { get; set; }
    [JsonProperty("awards")] public List<VinIdAward>? awards { get; set; }
}

public class VinIdPool
{
    [JsonProperty("redeemable_pool_units")] public string? redeemable_pool_units { get; set; }
    [JsonProperty("total_pool_units")]      public string? total_pool_units { get; set; }
}

public class VinIdAward
{
    [JsonProperty("pool_units")] public string? pool_units { get; set; }
}

public class VinIdRedeemPool
{
    [JsonProperty("pool_units_to_redeem")] public string? pool_units_to_redeem { get; set; }
}

public class VinIdMemberAccount
{
    [JsonProperty("account_no")]     public string? account_no { get; set; }
    [JsonProperty("account_status")] public string? account_status { get; set; }
    [JsonProperty("block_code")]     public string? block_code { get; set; }
    [JsonProperty("account_type")]   public string? account_type { get; set; }
}

public class VinIdMemberProfile
{
    [JsonProperty("full_name")]       public string? full_name { get; set; }
    [JsonProperty("csn")]             public string? csn { get; set; }
    [JsonProperty("client_id_1")]     public string? client_id_1 { get; set; }
    [JsonProperty("contact_details")] public List<VinIdContactDetail>? contact_details { get; set; }
}

public class VinIdContactDetail
{
    [JsonProperty("mobile_number")] public string? mobile_number { get; set; }
}

// ── Sales — POST /internal/checkout/v1/sale-loyalty-points ────────────────

public class VinIdSalesApiRequest
{
    [JsonProperty("transaction_ref_number")] public string? transaction_ref_number { get; set; }
    [JsonProperty("description")]            public string? description { get; set; }
    [JsonProperty("code_value")]             public string? code_value { get; set; }
    [JsonProperty("date")]                   public string? date { get; set; }
    [JsonProperty("time")]                   public string? time { get; set; }
    [JsonProperty("time_zone")]              public string? time_zone { get; set; }
    [JsonProperty("merchant_id")]            public string? merchant_id { get; set; }
    [JsonProperty("terminal_id")]            public string? terminal_id { get; set; }
    [JsonProperty("channel_code")]           public string? channel_code { get; set; }
    [JsonProperty("invoice_no")]             public string? invoice_no { get; set; }
    [JsonProperty("spend_points")]           public string? spend_points { get; set; }
    [JsonProperty("bill_amount")]            public string? bill_amount { get; set; }
    [JsonProperty("signature")]              public string? signature { get; set; }
}

public class VinIdSalesData
{
    [JsonProperty("customer_identifier")] public VinIdCustomerIdentifier? customer_identifier { get; set; }
    [JsonProperty("rs_header")]           public VinIdRsHeader? rs_header { get; set; }
    [JsonProperty("transaction")]         public VinIdTransactionDetail? transaction { get; set; }
    [JsonProperty("member_balance")]      public VinIdMemberBalance? member_balance { get; set; }
    [JsonProperty("redeem_pool_list")]    public List<VinIdRedeemPool>? redeem_pool_list { get; set; }
}

public class VinIdCustomerIdentifier
{
    [JsonProperty("id")] public string? id { get; set; }
}

public class VinIdRsHeader
{
    [JsonProperty("access_token")]  public string? access_token { get; set; }
    [JsonProperty("channel_id")]    public string? channel_id { get; set; }
    [JsonProperty("response_code")] public string? response_code { get; set; }
    [JsonProperty("error_code")]    public string? error_code { get; set; }
    [JsonProperty("error_message")] public string? error_message { get; set; }
}

public class VinIdTransactionDetail
{
    [JsonProperty("gross_transaction_amount")] public double gross_transaction_amount { get; set; }
    [JsonProperty("nett_amt")]                 public double nett_amt { get; set; }
}

// ── Refund — POST /internal/checkout/v1/refund-loyalty-points ─────────────

public class VinIdRefundApiRequest
{
    [JsonProperty("transaction_ref_number")] public string? transaction_ref_number { get; set; }
    [JsonProperty("description")]            public string? description { get; set; }
    [JsonProperty("code_value")]             public string? code_value { get; set; }
    [JsonProperty("date")]                   public string? date { get; set; }
    [JsonProperty("time")]                   public string? time { get; set; }
    [JsonProperty("time_zone")]              public string? time_zone { get; set; }
    [JsonProperty("merchant_id")]            public string? merchant_id { get; set; }
    [JsonProperty("terminal_id")]            public string? terminal_id { get; set; }
    [JsonProperty("channel_code")]           public string? channel_code { get; set; }
    [JsonProperty("invoice_no")]             public string? invoice_no { get; set; }
    [JsonProperty("refund_ref_number")]      public string? refund_ref_number { get; set; }
    [JsonProperty("spend_points")]           public string? spend_points { get; set; }
    [JsonProperty("refund_amount")]          public string? refund_amount { get; set; }
    [JsonProperty("signature")]              public string? signature { get; set; }
}

// ── InitTransaction — POST /internal/checkout/v1/transactions ────────────

public class VinIdTransactionApiRequest
{
    [JsonProperty("transaction_ref_number")] public string? transaction_ref_number { get; set; }
    [JsonProperty("code_value")]             public string? code_value { get; set; }
    [JsonProperty("description")]            public string? description { get; set; }
    [JsonProperty("merchant_id")]            public string? merchant_id { get; set; }
    [JsonProperty("terminal_id")]            public string? terminal_id { get; set; }
    [JsonProperty("order_created_at")]       public string? order_created_at { get; set; }
    [JsonProperty("currency")]               public string? currency { get; set; }
    [JsonProperty("invoice_no")]             public string? invoice_no { get; set; }
    [JsonProperty("bill_amount")]            public string? bill_amount { get; set; }
    [JsonProperty("amount")]                 public string? amount { get; set; }
    [JsonProperty("channel_code")]           public string? channel_code { get; set; }
    [JsonProperty("signature")]              public string? signature { get; set; }
}

public class VinIdTransactionApiData
{
    [JsonProperty("transaction_id")]             public string? transaction_id { get; set; }
    [JsonProperty("transactionStatus")]          public string? transactionStatus { get; set; }
    [JsonProperty("transaction_status")]         public string? transaction_status { get; set; }
    [JsonProperty("transaction_wallet_number")]  public string? transaction_wallet_number { get; set; }
}

// ── CancelTransaction — POST /internal/checkout/v1/transactions/cancel ────

public class VinIdCancelApiRequest
{
    [JsonProperty("channel_code")]   public string? channel_code { get; set; }
    [JsonProperty("transaction_id")] public string? transaction_id { get; set; }
    [JsonProperty("signature")]      public string? signature { get; set; }
}

// ── ScanAndGo — GET /vincart/v1/vcm?barcode=... ───────────────────────────

public class VinIdScanAndGoData
{
    [JsonProperty("order_no")]            public string? order_no { get; set; }
    [JsonProperty("customer_id")]         public string? customer_id { get; set; }
    [JsonProperty("customer_name")]       public string? customer_name { get; set; }
    [JsonProperty("customer_phone")]      public string? customer_phone { get; set; }
    [JsonProperty("order_date")]          public DateTime? order_date { get; set; }
    [JsonProperty("amount")]              public double? amount { get; set; }
    [JsonProperty("amount_deducted")]     public double? amount_deducted { get; set; }
    [JsonProperty("vin_id_paid")]         public double? vin_id_paid { get; set; }
    [JsonProperty("amount_collect")]      public double? amount_collect { get; set; }
    [JsonProperty("voucher_id")]          public string? voucher_id { get; set; }
    [JsonProperty("voucher_name")]        public string? voucher_name { get; set; }
    [JsonProperty("vin_id_card_number")]  public string? vin_id_card_number { get; set; }
    [JsonProperty("vin_id_card_name")]    public string? vin_id_card_name { get; set; }
    [JsonProperty("delivery_type")]       public double? delivery_type { get; set; }
    [JsonProperty("delivery_type_name")]  public string? delivery_type_name { get; set; }
    [JsonProperty("status")]              public double? status { get; set; }
    [JsonProperty("address")]             public string? address { get; set; }
    [JsonProperty("fee")]                 public double? fee { get; set; }
    [JsonProperty("is_expired")]          public bool? is_expired { get; set; }
    [JsonProperty("site_code")]           public string? site_code { get; set; }
    [JsonProperty("pricing_store_code")]  public string? pricing_store_code { get; set; }
    [JsonProperty("vin_id_earn")]         public int? vin_id_earn { get; set; }
    [JsonProperty("has_vat_invoice")]     public bool? has_vat_invoice { get; set; }
    [JsonProperty("payments")]            public List<VinIdScanPayment>? payments { get; set; }
    [JsonProperty("items")]               public List<VinIdScanItem>? items { get; set; }
    [JsonProperty("billing_info")]        public VinIdBillingInfo? billing_info { get; set; }
}

public class VinIdScanPayment
{
    [JsonProperty("payment_method")] public string? payment_method { get; set; }
    [JsonProperty("paid_amount")]    public double? paid_amount { get; set; }
    [JsonProperty("transaction_id")] public string? transaction_id { get; set; }
}

public class VinIdScanItem
{
    [JsonProperty("id")]              public string? id { get; set; }
    [JsonProperty("article_id")]      public string? article_id { get; set; }
    [JsonProperty("article_name")]    public string? article_name { get; set; }
    [JsonProperty("sold_price")]      public double? sold_price { get; set; }
    [JsonProperty("quantity")]        public double? quantity { get; set; }
    [JsonProperty("unit_code")]       public string? unit_code { get; set; }
    [JsonProperty("product_barcode")] public string? product_barcode { get; set; }
    [JsonProperty("origin_barcode")]  public string? origin_barcode { get; set; }
    [JsonProperty("item_type")]       public string? item_type { get; set; }
    [JsonProperty("vat_group")]       public double? vat_group { get; set; }
    [JsonProperty("vat_percent")]     public double? vat_percent { get; set; }
    [JsonProperty("net_price")]       public double? net_price { get; set; }
    [JsonProperty("promotion_value")] public double? promotion_value { get; set; }
    [JsonProperty("unit_quantity")]   public double? unit_quantity { get; set; }
    [JsonProperty("sale_quantity")]   public double? sale_quantity { get; set; }
    [JsonProperty("is_freshfood")]    public bool? is_freshfood { get; set; }
    [JsonProperty("unit_sold_price")] public double? unit_sold_price { get; set; }
    [JsonProperty("total_sold_price")]public double? total_sold_price { get; set; }
}

public class VinIdBillingInfo
{
    [JsonProperty("customer_name")] public string? customer_name { get; set; }
    [JsonProperty("address")]       public string? address { get; set; }
    [JsonProperty("email")]         public string? email { get; set; }
    [JsonProperty("company_name")]  public string? company_name { get; set; }
    [JsonProperty("tax_code")]      public string? tax_code { get; set; }
    [JsonProperty("phone_number")]  public string? phone_number { get; set; }
    [JsonProperty("site_code")]     public string? site_code { get; set; }
}

// ── UpdateStatusOrder — PATCH /vincart/v1/vcm/update-order-status ─────────

public class VinIdStatusOrderApiData
{
    [JsonProperty("receipt_number")]         public string? receipt_number { get; set; }
    [JsonProperty("transaction_type")]       public string? transaction_type { get; set; }
    [JsonProperty("store_code")]             public string? store_code { get; set; }
    [JsonProperty("pos_code")]               public string? pos_code { get; set; }
    [JsonProperty("merchant_id")]            public string? merchant_id { get; set; }
    [JsonProperty("terminal_id")]            public string? terminal_id { get; set; }
    [JsonProperty("customer_name")]          public string? customer_name { get; set; }
    [JsonProperty("vinid_card_number")]      public string? vinid_card_number { get; set; }
    [JsonProperty("vinid_csn")]              public string? vinid_csn { get; set; }
    [JsonProperty("total_bill_amount")]      public double? total_bill_amount { get; set; }
    [JsonProperty("reference_number")]       public string? reference_number { get; set; }
    [JsonProperty("extra_earn_by_items")]    public int? extra_earn_by_items { get; set; }
    [JsonProperty("extra_earn_by_campaign")] public int? extra_earn_by_campaign { get; set; }
    [JsonProperty("vin_id_earn")]            public int? vin_id_earn { get; set; }
    [JsonProperty("is_earn_success")]        public bool? is_earn_success { get; set; }
    [JsonProperty("over_quota")]             public bool? over_quota { get; set; }
    [JsonProperty("bill_lines")]             public List<VinIdStatusBillLine>? bill_lines { get; set; }
}

public class VinIdStatusBillLine
{
    [JsonProperty("record_no")]           public float? record_no { get; set; }
    [JsonProperty("barcode")]             public string? barcode { get; set; }
    [JsonProperty("article")]            public string? article { get; set; }
    [JsonProperty("article_name")]        public string? article_name { get; set; }
    [JsonProperty("uom")]                 public string? uom { get; set; }
    [JsonProperty("quantity")]            public float? quantity { get; set; }
    [JsonProperty("sale_price")]          public float? sale_price { get; set; }
    [JsonProperty("amount")]              public float? amount { get; set; }
    [JsonProperty("discount_amount")]     public float? discount_amount { get; set; }
    [JsonProperty("line_amount")]         public float? line_amount { get; set; }
    [JsonProperty("extra_quantity_earn")] public float? extra_quantity_earn { get; set; }
    [JsonProperty("extra_amount_earn")]   public float? extra_amount_earn { get; set; }
    [JsonProperty("vcm_unit_price")]      public float? vcm_unit_price { get; set; }
}

// ── ExtraSales — POST /loyalty-extra-point/v1/sale ────────────────────────

public class VinIdExtraSaleApiRequest
{
    [JsonProperty("receipt_number")]   public string? receipt_number { get; set; }
    [JsonProperty("transaction_type")] public string? transaction_type { get; set; }
    [JsonProperty("store_code")]       public string? store_code { get; set; }
    [JsonProperty("pos_code")]         public string? pos_code { get; set; }
    [JsonProperty("merchant_id")]      public string? merchant_id { get; set; }
    [JsonProperty("terminal_id")]      public string? terminal_id { get; set; }
    [JsonProperty("cashier_code")]     public string? cashier_code { get; set; }
    [JsonProperty("transaction_time")] public long transaction_time { get; set; }
    [JsonProperty("business_time")]    public long business_time { get; set; }
    [JsonProperty("customer_name")]    public string? customer_name { get; set; }
    [JsonProperty("vinid_card_number")]public string? vinid_card_number { get; set; }
    [JsonProperty("vinid_csn")]        public string? vinid_csn { get; set; }
    [JsonProperty("total_bill_amount")]public float? total_bill_amount { get; set; }
    [JsonProperty("reference_number")] public string? reference_number { get; set; }
    [JsonProperty("bill_lines")]       public List<VinIdExtraBillLine>? bill_lines { get; set; }
    [JsonProperty("signature")]        public string? signature { get; set; }
}

public class VinIdExtraBillLine
{
    [JsonProperty("record_no")]       public float? record_no { get; set; }
    [JsonProperty("barcode")]         public string? barcode { get; set; }
    [JsonProperty("article")]         public string? article { get; set; }
    [JsonProperty("article_name")]    public string? article_name { get; set; }
    [JsonProperty("uom")]             public string? uom { get; set; }
    [JsonProperty("quantity")]        public float? quantity { get; set; }
    [JsonProperty("sale_price")]      public float? sale_price { get; set; }
    [JsonProperty("amount")]          public float? amount { get; set; }
    [JsonProperty("discount_amount")] public float? discount_amount { get; set; }
    [JsonProperty("line_amount")]     public float? line_amount { get; set; }
}

public class VinIdExtraSaleApiData
{
    [JsonProperty("receipt_number")]         public string? receipt_number { get; set; }
    [JsonProperty("transaction_type")]       public string? transaction_type { get; set; }
    [JsonProperty("transaction_time")]       public string? transaction_time { get; set; }
    [JsonProperty("business_time")]          public string? business_time { get; set; }
    [JsonProperty("customer_name")]          public string? customer_name { get; set; }
    [JsonProperty("vinid_card_number")]      public string? vinid_card_number { get; set; }
    [JsonProperty("vinid_csn")]              public string? vinid_csn { get; set; }
    [JsonProperty("total_bill_amount")]      public double? total_bill_amount { get; set; }
    [JsonProperty("reference_number")]       public string? reference_number { get; set; }
    [JsonProperty("over_quota")]             public bool? over_quota { get; set; }
    [JsonProperty("extra_earn_by_items")]    public int? extra_earn_by_items { get; set; }
    [JsonProperty("extra_earn_by_campaign")] public int? extra_earn_by_campaign { get; set; }
    [JsonProperty("employee_code")]          public string? employee_code { get; set; }
    [JsonProperty("vin_id_earn")]            public int? vin_id_earn { get; set; }
    [JsonProperty("bill_lines")]             public List<VinIdStatusBillLine>? bill_lines { get; set; }
}

// ── ExtraRefund — POST /loyalty-extra-point/v1/return ─────────────────────

public class VinIdExtraRefundApiRequest
{
    [JsonProperty("card_number")]               public string? card_number { get; set; }
    [JsonProperty("customer_name")]             public string? customer_name { get; set; }
    [JsonProperty("receipt_number")]            public string? receipt_number { get; set; }
    [JsonProperty("order_no")]                  public string? order_no { get; set; }
    [JsonProperty("store_code")]                public string? store_code { get; set; }
    [JsonProperty("pos_terminal")]              public string? pos_terminal { get; set; }
    [JsonProperty("cashier_code")]              public string? cashier_code { get; set; }
    [JsonProperty("total_bill_amount")]         public float? total_bill_amount { get; set; }
    [JsonProperty("original_receipt_number")]   public string? original_receipt_number { get; set; }
    [JsonProperty("original_pos_number")]       public string? original_pos_number { get; set; }
    [JsonProperty("original_reference_number")] public string? original_reference_number { get; set; }
    [JsonProperty("reference_number")]          public string? reference_number { get; set; }
    [JsonProperty("earn_by_default")]           public float? earn_by_default { get; set; }
    [JsonProperty("refund_amount")]             public float? refund_amount { get; set; }
    [JsonProperty("refund_point_default")]      public float? refund_point_default { get; set; }
    [JsonProperty("bill_lines")]                public List<VinIdExtraRefundLine>? bill_lines { get; set; }
}

public class VinIdExtraRefundLine
{
    [JsonProperty("record_no")]       public double record_no { get; set; }
    [JsonProperty("barcode")]         public string? barcode { get; set; }
    [JsonProperty("article")]         public string? article { get; set; }
    [JsonProperty("article_name")]    public string? article_name { get; set; }
    [JsonProperty("uom")]             public string? uom { get; set; }
    [JsonProperty("quantity")]        public float? quantity { get; set; }
    [JsonProperty("refund_quantity")] public float? refund_quantity { get; set; }
    [JsonProperty("sale_price")]      public float? sale_price { get; set; }
    [JsonProperty("amount")]          public float? amount { get; set; }
    [JsonProperty("discount_amount")] public float? discount_amount { get; set; }
    [JsonProperty("line_amount")]     public float? line_amount { get; set; }
}

// ── SnGRefund — POST /vincart/v1/vcm/order-returns/{barcode} ───────────────

public class VinIdSnGRefundApiRequest
{
    [JsonProperty("return_no")]       public string? return_no { get; set; }
    [JsonProperty("return_date")]     public string? return_date { get; set; }
    [JsonProperty("return_reason")]   public string? return_reason { get; set; }
    [JsonProperty("note")]            public string? note { get; set; }
    [JsonProperty("redeeming_point")] public long? redeeming_point { get; set; }
    [JsonProperty("items")]           public List<VinIdSnGRefundItem>? items { get; set; }
}

public class VinIdSnGRefundItem
{
    [JsonProperty("note")]                 public string? note { get; set; }
    [JsonProperty("order_item_code")]      public string? order_item_code { get; set; }
    [JsonProperty("order_item_id")]        public long? order_item_id { get; set; }
    [JsonProperty("return_sale_quantity")] public long? return_sale_quantity { get; set; }
    [JsonProperty("return_unit_quantity")] public double? return_unit_quantity { get; set; }
}

// ── SnGTopup — POST /vincart-vinmart-order/v2/vcm/{barcode}/topup ──────────

public class VinIdSnGTopupApiRequest
{
    [JsonProperty("topup_no")]   public string? topup_no { get; set; }
    [JsonProperty("topup_data")] public List<VinIdTopupItem>? topup_data { get; set; }
}

public class VinIdTopupItem
{
    [JsonProperty("amount")] public long? amount { get; set; }
    [JsonProperty("type")]   public string? type { get; set; }
}
