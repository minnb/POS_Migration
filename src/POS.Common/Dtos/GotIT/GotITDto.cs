using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace POS.Common.Dtos.GotIT;

public enum VoucherTypeGotIt
{
    conditional,
    standard
}

public class GotITResponse
{
    public bool Success { get; set; }
    public string? Return_code { get; set; }
    public string? Message_en { get; set; }
    public string? Message_vi { get; set; }
    public List<object>? Data { get; set; }
}

public class VoucherCodeData
{
    public string? Code { get; set; }
    public ProductGotIt? Product { get; set; }
    public int? State { get; set; }
    public string? Used_date { get; set; }
    public string? Expiry_date { get; set; }
    public string? Cancel_date { get; set; }
    public UsedStoreGotIt? Used_store { get; set; }
    public string? Bill_number { get; set; }
}

public class VoucherCodeDataV2
{
    public string? Code { get; set; }
    public int? State { get; set; }
    public int Value { get; set; }
    public int Product_id { get; set; }
    public string? Expiry_date { get; set; }
    public string? Cancel_date { get; set; }
    public string? Voucher_type { get; set; }
    public RedemptionsGotIt? Redemptions { get; set; }
    public string? Bill_number { get; set; }
    public ConditionsGotIt? Conditions { get; set; }
}

public class ConditionsGotIt
{
    public List<string>? Redeemable_skus { get; set; }
}

public class RedemptionsGotIt
{
    [JsonIgnore]
    public List<RedeemSkuCode>? Redeem_sku_codes { get; set; }
    public int Redemption_value { get; set; }
    public string? Bill_number { get; set; }
    public UsedStore? Used_store { get; set; }
    public string? Used_date { get; set; }
}

public class RedeemSkuCode
{
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public int Price { get; set; }
}

public class UsedStore
{
    public string? Name_vi { get; set; }
    public string? Name_en { get; set; }
}

public class ProductGotIt
{
    public string? Product_name_en { get; set; }
    public string? Product_name_vi { get; set; }
    public string? Type { get; set; }
    public decimal? Value { get; set; }
    public string? Poscode { get; set; }
    public string? Sku { get; set; }
}

public class UsedStoreGotIt
{
    public string? Name_vi { get; set; }
    public string? Name_en { get; set; }
}

public class CheckMultipleDto
{
    [Required]
    public string Pin { get; set; } = string.Empty;
    [Required]
    public List<string> Codes { get; set; } = [];
    public string? Bill_number { get; set; }
    public bool Skip_reserved_when_mark_used { get; set; }
    public List<Skus_info_gotit>? Skus_info { get; set; }
    public decimal Total_bill { get; set; }
}

public class Skus_info_gotit
{
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
}

public class MarkUseMultipleDto
{
    [Required]
    public string Pin { get; set; } = string.Empty;
    [Required]
    public List<string> Codes { get; set; } = [];
    [Required]
    public string Bill_number { get; set; } = string.Empty;
    public decimal Total_bill { get; set; }
    public bool Skip_reserved_when_mark_used { get; set; }
    public List<Skus_info_gotit>? Skus_info { get; set; }
}
