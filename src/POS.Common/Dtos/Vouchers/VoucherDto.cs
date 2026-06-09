namespace POS.Common.Dtos.Vouchers;

public class TokenOneURequest
{
    public string? Client_id { get; set; }
    public string? Client_secret { get; set; }
    public string? Audience { get; set; }
}

public class EstimateRequest
{
    public string? Usecase { get; set; }
    public string? Invoice_no { get; set; }
    public List<VoucherDataItem>? Apply_items { get; set; }
    public CheckoutDataOneU? Checkout_data { get; set; }
}

public class CheckoutDataOneU
{
    public decimal Total_amount { get; set; }
    public int Id { get; set; }
    public MerchantOunU? Merchant { get; set; }
    public MerchantOunU? Store { get; set; }
    public List<MoneySourcesOneU>? Money_sources { get; set; }
    public OrderDetailsOneU? Order_details { get; set; }
}

public class OrderDetailsOneU
{
    public string? Code { get; set; }
}

public class MoneySourcesOneU
{
    public string? Payment_method_code { get; set; }
}

public class MerchantOunU
{
    public string? Code { get; set; }
    public string? Name { get; set; }
}

public class VoucherDataItem
{
    public string? Type { get; set; }
    public string? Serial { get; set; }
    public string? Merchant_code { get; set; }
}

public class TokenOneUResponse
{
    public MetaOneResponse? Meta { get; set; }
    public TokenDataOneUResponse? Data { get; set; }
}

public class MetaOneResponse
{
    public string? Request_id { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
    public string? Service_code { get; set; }
}

public class TokenDataOneUResponse
{
    public string? Access_token { get; set; }
    public string? Token_type { get; set; }
    public int Expires_in { get; set; }
}

public class RedeemResponseOneU
{
    public MetaOneResponse? Meta { get; set; }
    public RedeemDataOneU? Data { get; set; }
}

public class RedeemDataOneU
{
    public string? Transaction_id { get; set; }
    public decimal Total_discount { get; set; }
}

public class EstimateResponseOneU
{
    public MetaOneResponse? Meta { get; set; }
    public EstimateDataOneU? Data { get; set; }
}

public class EstimateDataOneU
{
    public decimal Total_discount { get; set; }
    public List<EstimateItemDetails>? Item_details { get; set; }
}

public class EstimateItemDetails
{
    public string? Item_id { get; set; }
    public string? Item_code { get; set; }
    public string? Item_type { get; set; }
    public long Item_value { get; set; }
    public long Item_max_allow { get; set; }
    public List<string>? Variant_code { get; set; }
    public string? Payment_method { get; set; }
    public decimal Discount_amount { get; set; }
}

public class VoucherIssueDetail
{
    public string? Status { get; set; }
    public string? PhoneNumber { get; set; }
    public string? VoucherNumber { get; set; }
    public decimal Value { get; set; }
    public string? FromDate { get; set; }
    public string? ExpiryDate { get; set; }
    public string? BonusBuy { get; set; }
    public string? ArticleType { get; set; }
    public string? ActicleNo { get; set; }
    public DateTime CrtDate { get; set; }
    public Guid RefId { get; set; }
    public string? RequestId { get; set; }
    public Guid Id { get; set; }
    public string? OrderNo { get; set; }
    public string? PosUsed { get; set; }
    public DateTime? UsedTime { get; set; }
}
