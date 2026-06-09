using Newtonsoft.Json;

namespace POS.Common.Dtos.WinX;

public class OrderDataWinXResponse
{
    [JsonProperty("bill")]
    public BillWinX? Bill { get; set; }
}

public class BillWinX
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("number")]
    public string? Number { get; set; }

    [JsonProperty("date")]
    public DateTime Date { get; set; }

    [JsonProperty("user")]
    public UserWinX? User { get; set; }

    [JsonProperty("amounts")]
    public AmountWinX? Amounts { get; set; }

    [JsonProperty("lineItems")]
    public List<LineItemWinX>? LineItems { get; set; }
}

public class UserWinX
{
    [JsonProperty("userId")]
    public string? UserId { get; set; }

    [JsonProperty("mobile")]
    public string? Mobile { get; set; }
}

public class AmountWinX
{
    [JsonProperty("gross")]
    public decimal Gross { get; set; }

    [JsonProperty("discount")]
    public decimal Discount { get; set; }

    [JsonProperty("net")]
    public decimal Net { get; set; }
}

public class LineItemWinX
{
    [JsonProperty("lineId")]
    public int LineId { get; set; }

    [JsonProperty("itemCode")]
    public string? ItemCode { get; set; }

    [JsonProperty("itemName")]
    public string? ItemName { get; set; }

    [JsonProperty("quantity")]
    public decimal Quantity { get; set; }

    [JsonProperty("uom")]
    public string? UOM { get; set; }

    [JsonProperty("pricing")]
    public PricingWinX? Pricing { get; set; }
}

public class PricingWinX
{
    [JsonProperty("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonProperty("discount")]
    public decimal Discount { get; set; }

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("tax")]
    public decimal Tax { get; set; }
}

public class ResponseDynamicVouchersWinX
{
    public int Status { get; set; }
    public ResponseDataDynamicVoucherWinX? Data { get; set; }
    public string? Message { get; set; }
}

public class ResponseDataDynamicVoucherWinX
{
    public List<DataDynamicVoucherWinX>? Data { get; set; }
    public List<ResponseErrorDynamicVoucherWinX>? Errors { get; set; }
}

public class ResponseErrorDynamicVoucherWinX
{
    public string? Dynamic_code { get; set; }
    public string? Error { get; set; }
}

public class DataDynamicVoucherWinX
{
    public string? Dynamic_code { get; set; }
    public string? Capillary_voucher_code { get; set; }
    public DateTime Valid_from { get; set; }
    public DateTime Valid_until { get; set; }
    public string? Uuser_phone_number { get; set; }
    public string? Capillary_user_id { get; set; }
    public string? Series_id { get; set; }
}

public class RequestDynamicVouchersWinX
{
    public List<string>? Dynamic_codes { get; set; }
}

public class ResolveWinxUserIdRequest
{
    public List<string>? Winx_user_ids { get; set; }
}

public class ResolveWinxUserIdCapillary
{
    public int Status { get; set; }
    public ResolveWinxUserIdCapillaryData? Data { get; set; }
    public string? Message { get; set; }
}

public class ResolveWinxUserIdCapillaryData
{
    public List<WinxUserIdCapillaryData>? Data { get; set; }
    public List<ResponseErrorDynamicVoucherWinX>? Errors { get; set; }
}

public class WinxUserIdCapillaryData
{
    public string? Winx_user_id { get; set; }
    public string? Capillary_user_id { get; set; }
    public string? Phone { get; set; }
    public string? Valid_from { get; set; }
    public string? Valid_until { get; set; }
}

public class WinxResponse
{
    public int Status { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}

public class PosPostTransactionsRequest
{
    public string? Bill_id { get; set; }
}

public class PosPostTransactionsResponse
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
