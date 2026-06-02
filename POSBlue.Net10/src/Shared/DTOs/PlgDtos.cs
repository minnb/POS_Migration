using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace VCM.POSBLUE.Shared.DTOs;

// ── api/plg/* request DTOs ────────────────────────────────────────────────────

public class PlCheckVoucherRequest
{
    public string? partner { get; set; }
    public string? storeNo { get; set; }
    public string? posID { get; set; }
    public bool isVoucher { get; set; }
    public List<PlSeriNoRequest>? listSeriNo { get; set; }
    public bool isWeb { get; set; }
}

public class PlSeriNoRequest
{
    public string? seriNo { get; set; }
    public string? articleSAP { get; set; }
    public bool isEmployee { get; set; }
}

public class SaleVoucherRequest
{
    public string? partner { get; set; }
    public string? seriNo { get; set; }
    public string? storeNo { get; set; }
    public string? posID { get; set; }
    public string? staffCode { get; set; }
    public double salePrice { get; set; }
    public double discountAmount { get; set; }
    public string? orderNo { get; set; }
    public List<SaleVoucherItemRequest>? listSeriNo { get; set; }
}

public class SaleVoucherItemRequest
{
    public string? seriNo { get; set; }
    public bool isVoucher { get; set; }
    public double salePrice { get; set; }
    public double discountAmount { get; set; }
}

public class RedeemVoucherRequest
{
    public string? partner { get; set; }
    public List<PlSeriNoItem>? listSeriNo { get; set; }
    public string? orderNo { get; set; }
    public string? storeNo { get; set; }
    public string? posID { get; set; }
    public string? staffCode { get; set; }
    public double totalBill { get; set; }
}

public class PlSeriNoItem
{
    public string? seriNo { get; set; }
    public bool isVoucher { get; set; }
    public double value { get; set; }
}

public class PlgSaleCxRequest
{
    [Required] public string cardNumber { get; set; } = "";
    public string? merchantId { get; set; }
    public string? clubCode { get; set; }
    [Required] public string storeNo { get; set; } = "";
    [Required] public string posID { get; set; } = "";
    public string? posTerminal { get; set; }
    [Required] public string orderNo { get; set; } = "";
    public double awardAmount { get; set; }
    public double orderAmount { get; set; }
    public int spendPoints { get; set; }
    public int redeemStampsPoints { get; set; }
    public string? referenceInvoice { get; set; }
}

public class OdooUpdateVoucherRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? OrderNo { get; set; }
    public List<OdooVoucherItem>? ListSeriNo { get; set; }
}

public class OdooVoucherItem
{
    public string? SerialNo { get; set; }
    public string? ArticleNo { get; set; }
    public string? ArticleType { get; set; }
    public string? Status { get; set; }
}

// GiftBox DTOs
public class GiftBoxVoucherStatusRequest
{
    public string? authKey { get; set; }
    public string? brandCode { get; set; }
    public string? pinNo { get; set; }
    public string? storeCode { get; set; }
}

// Giftee DTOs
public class GifteeStatusRequest
{
    public string? StoreNo { get; set; }
    public string? PosTerminal { get; set; }
    public string? SeriNo { get; set; }
}

// GotIt DTOs
public class GotItCheckPosRequest
{
    public string? serialNo { get; set; }
    public string? partner { get; set; }
    public string? storeNo { get; set; }
    public string? posID { get; set; }
}

// ── UrBox / Partner DTOs (dùng cho IUrboxService và PLGService) ───────────────
public class CheckVoucherPartnerPOSRequest
{
    [Required] public string Partner { get; set; } = "";
    [Required] public string StoreNo { get; set; } = "";
    [Required] public string PosNo { get; set; } = "";
    public string? PhoneNumber { get; set; }
    [Required] public List<string> SerialNo { get; set; } = [];
    public List<SkuApplyVoucherPartner>? Items { get; set; }
    // UrBox-specific convenience props
    public double amount { get; set; }
    public string? code { get; set; }
    public string? store_id { get; set; }
    public string? terminal_id { get; set; }
}

public class SkuApplyVoucherPartner
{
    public int LineNo { get; set; }
    public string? ItemNo { get; set; }
    public string? Barcode { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Qty { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineAmount { get; set; }
}

public class UpdateStatusVoucherPartnerRequest
{
    [Required] public string Partner { get; set; } = "";
    [Required] public string StoreNo { get; set; } = "";
    [Required] public string PosNo { get; set; } = "";
    [Required] public string OrderNo { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public List<LstVoucherPartner>? SerialNo { get; set; }
    public List<SkuApplyVoucherPartner>? Items { get; set; }
}

public class LstVoucherPartner
{
    [Required] public string Code { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

// ── api/v2/partner/* — Payment DTOs ──────────────────────────────────────────

public class ReActiveCouponRequest
{
    [Required] public string Partner { get; set; } = "";
    [Required] public string StoreNo { get; set; } = "";
    [Required] public List<string> SerialNo { get; set; } = [];
    [Required] public string UserName { get; set; } = "";
    public string? PhoneNumber { get; set; }
}
