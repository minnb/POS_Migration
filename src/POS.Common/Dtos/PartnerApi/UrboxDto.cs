using Newtonsoft.Json;

namespace POS.Common.Dtos.PartnerApi;

public class UrboxProducts
{
    public string product_code { get; set; } = string.Empty;
    public int quantity { get; set; }
    public int total_price { get; set; }
}

public class CheckUrboxRequest
{
    public int amount { get; set; }
    public string code { get; set; } = string.Empty;
    public string store_id { get; set; } = string.Empty;
    public string terminal_id { get; set; } = string.Empty;
}

public class CheckUrboxRequestV2
{
    public int amount { get; set; }
    public int brand_id { get; set; }
    public string code { get; set; } = string.Empty;
    public string store_id { get; set; } = string.Empty;
    public string terminal_id { get; set; } = string.Empty;
}

public class CheckUrboxRequestV2_SKU
{
    public int amount { get; set; }
    public string bill_at { get; set; } = string.Empty;
    public string bill_id { get; set; } = string.Empty;
    public int brand_id { get; set; }
    public string code { get; set; } = string.Empty;
    public int has_order_detail { get; set; }
    public List<UrboxProducts> products { get; set; } = [];
    public string store_id { get; set; } = string.Empty;
    public string terminal_id { get; set; } = string.Empty;
}

public class PayCodeToUrboxRequestV2
{
    public int amount { get; set; }
    public string bill_id { get; set; } = string.Empty;
    public int brand_id { get; set; }
    public string code { get; set; } = string.Empty;
    public string store_id { get; set; } = string.Empty;
    public string terminal_id { get; set; } = string.Empty;
}

public class PayCodeToUrboxRequestProducts
{
    public int amount { get; set; }
    public string bill_at { get; set; } = string.Empty;
    public string bill_id { get; set; } = string.Empty;
    public int brand_id { get; set; }
    public string code { get; set; } = string.Empty;
    public int has_order_detail { get; set; }
    public List<UrboxProducts> products { get; set; } = [];
    public string store_id { get; set; } = string.Empty;
    public string terminal_id { get; set; } = string.Empty;
}

public class CheckUrboxResponse
{
    public int done { get; set; }
    public int type { get; set; }
    public int status { get; set; }
    public UrboxResponseData? data { get; set; }
}

public class CheckUrboxErrorResponse
{
    [JsonProperty("0")]
    public CheckUrboxResponse? Zero { get; set; }
}

public class UrboxResponseData
{
    public string? msg { get; set; }
    public string? code { get; set; }
    public int amount { get; set; }
    public int brand_id { get; set; }
    public int supplier_id { get; set; }
    public int gift_detail_id { get; set; }
    public string? gift_title { get; set; }
    public List<string>? sku_apply { get; set; }
}

public class PayErrorUrboxResponse
{
    public List<CheckUrboxResponse>? Original { get; set; }
}

public class ResponseMessageUrbox
{
    public string? message { get; set; }
    public bool status { get; set; }
}
