using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace VCM.POSBLUE.Shared.DTOs;

// SAP / ROP / PLG voucher request-response DTOs — port nguyên field từ bản cũ.

// ── Shared response ──────────────────────────────────────────────────────────
public class VoucherStatusResponse
{
    public string? Status { get; set; }
    public string? Return { get; set; }
    public string? ActicleNo { get; set; }
    public string? ActicleType { get; set; }
    public string? VoucherNumber { get; set; }
    public string? Value { get; set; }
    public string? Voucher_Currency { get; set; }
    public string? Validity_From_Date { get; set; }
    public string? Expiry_Date { get; set; }
    public string? CompanyCode { get; set; }
    public string? Partner { get; set; }
    public bool? IsEmployee { get; set; }
    public string? PhoneNumber { get; set; }
    public string? VoucherType { get; set; }
}

public class EVoucherResponse
{
    public string? CSN { get; set; }
    public string? SerialNo { get; set; }
    public string? ArticleNo { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Return { get; set; }
    public string? Message { get; set; }
}

// ── CheckVoucher / CheckVoucherBySerials ──────────────────────────────────────
public class SapVoucherInput
{
    public string VoucherType { get; set; } = "V";
    public string ArticleNo { get; set; } = "";
    public string SerialNo { get; set; } = "";
    public string VoucherValue { get; set; } = "";
    public string VoucherCurrency { get; set; } = "VND";
    public string ValidityFromDate { get; set; } = "";
    public string ExpiryDate { get; set; } = "";
    public string ProcessingType { get; set; } = "C"; // A=create, C=check, U=update
    public string Status { get; set; } = "";
    public string BonusBuy { get; set; } = "";
    public string Site { get; set; } = "";
}

public class SapVoucherOutput
{
    public string? Return { get; set; }
    public string? Status { get; set; }
    public string? Article_No { get; set; }
    public string? Article_Type { get; set; }
    public string? Voucher_Value { get; set; }
    public string? Voucher_Currency { get; set; }
    public string? Validity_From_Date { get; set; }
    public string? Expiry_Date { get; set; }
    public string? CompanyCode { get; set; }
}

public class SapEVoucherInput
{
    public string CSN { get; set; } = "";
    public string SerialNo { get; set; } = "";
}

public class SapEVoucherOutput
{
    public string? Return { get; set; }
    public string? Message { get; set; }
    public string? CSN { get; set; }
    public string? SerialNo { get; set; }
    public string? ArticleNo { get; set; }
    public string? Amount { get; set; }
    public string? ValidFrom { get; set; }
    public string? ValidTo { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
}

// ── CreateNewVoucher ──────────────────────────────────────────────────────────
public class CreateVoucherModel
{
    [Required] public string VoucherNumber { get; set; } = "";
    public decimal Value { get; set; }
    [Required] public string From_Date { get; set; } = "";
    [Required] public string Expiry_Date { get; set; } = "";
    [Required] public string SiteCode { get; set; } = "";
    public string? BonusBuy { get; set; }
    public string? Article_No { get; set; }
    [Required] public string POSTerminal { get; set; } = "";
    public string? OrderNo { get; set; }
}

// ── UpdateVoucher ─────────────────────────────────────────────────────────────
public class VoucherUpdateRequest
{
    public string CompanyCode { get; set; } = "";
    [Required] public string VoucherNumber { get; set; } = "";
    public string? ArticleNo { get; set; }
    public string? ArticleType { get; set; }
    [Required] public string Status { get; set; } = "RDM"; // SOLD/RDM/EXP
    [Required] public string SiteCode { get; set; } = "";
    [Required] public string POSTerminal { get; set; } = "";
    public string? OrderNo { get; set; }
}

public class VoucherUpdateModel
{
    public string OrderNo { get; set; } = "";
    public double TotalBill { get; set; }
    public string SiteCode { get; set; } = "";
    public string POSTerminal { get; set; } = "";
    public bool IsVoucher { get; set; } = true;
    public bool IsRetry { get; set; }
    public string PhoneNumber { get; set; } = "";
    public List<VoucherUpdateSerial>? ListSeriNo { get; set; }
}

public class VoucherUpdateSerial
{
    public string CompanyCode { get; set; } = "";
    public string Partner { get; set; } = "";
    public string voucherNumber { get; set; } = "";
    public bool isVoucher { get; set; }
    public double value { get; set; }
    public string? articleNo { get; set; }
    public string? articleType { get; set; }
    public string? status { get; set; }
}

// ── ROP API DTOs ─────────────────────────────────────────────────────────────
public class RopApiResponse
{
    [JsonProperty("responseCode")] public int ResponseCode { get; set; }
    [JsonProperty("data")] public object? Data { get; set; }
    [JsonProperty("technicalMessage")] public string[]? TechnicalMessage { get; set; }
    [JsonProperty("message")] public string? Message { get; set; }
}

public class RopVoucherInfo
{
    public string? SAPCode { get; set; }
    public string? NameCode { get; set; }
    public string? SerialNumber { get; set; }
    public string? VoucherCode { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public decimal VoucherValue { get; set; }
    public decimal VoucherPercent { get; set; }
    public string? BonusBuy { get; set; }
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? StorageSite { get; set; }
    public string? VoucherTypeName { get; set; }
    public string? CompanyCode { get; set; }
    public string? SAPVoucherType { get; set; }
    public string? SAPArticleType { get; set; }
}

public class RopVoucherInfoNullable
{
    public string? SAPCode { get; set; }
    public string? SerialNumber { get; set; }
    public string? VoucherCode { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public decimal VoucherValue { get; set; }
    public int StatusId { get; set; }
    public string? StorageSite { get; set; }
    public string? CompanyCode { get; set; }
}

public class RopInsertRequest
{
    public string? SiteCode { get; set; }
    public string? POSTerminal { get; set; }
    public string? ArticleType { get; set; }
    public IList<RopInsertVoucherItem>? Vouchers { get; set; }
}

public class RopInsertVoucherItem
{
    public string VoucherCode { get; set; } = "";
    public decimal VoucherValue { get; set; }
    public int VoucherPercent { get; set; }
    public string? OrderNumber { get; set; }
    public string? BonusBuy { get; set; }
    public string? SAPCode { get; set; }
    public int StatusId { get; set; }
    public string? ValidFrom { get; set; }
    public string? ValidTo { get; set; }
}

public class RopUpdateStatusRequest
{
    public string[]? VoucherNumbers { get; set; }
    public string? Site { get; set; }
    public string? PosTerminal { get; set; }
    public string? OrderNumber { get; set; }
    public int Status { get; set; }
}

public class RopCheckRequest
{
    public string[]? VoucherNumbers { get; set; }
    public string? Site { get; set; }
    public string? PosTerminal { get; set; }
    public string? OrderNumber { get; set; }
}

// ── PLG (Phúc Long) DTOs ──────────────────────────────────────────────────────
public class PlgCheckVoucherRequest
{
    [JsonProperty("isWeb")] public bool isWeb { get; set; }
    [JsonProperty("storeNo")] public string? storeNo { get; set; }
    [JsonProperty("posID")] public string? posID { get; set; }
    [JsonProperty("partner")] public string? partner { get; set; }
    [JsonProperty("isVoucher")] public bool isVoucher { get; set; }
    [JsonProperty("listSeriNo")] public List<PlgSeriNoRequest>? listSeriNo { get; set; }
}

public class PlgSeriNoRequest
{
    [JsonProperty("seriNo")] public string? seriNo { get; set; }
    [JsonProperty("isEmployee")] public bool isEmployee { get; set; }
}

public class PlgVoucherResponse
{
    [JsonProperty("statusVoucher")] public string? StatusVoucher { get; set; }
    [JsonProperty("descVoucher")] public string? DescVoucher { get; set; }
    [JsonProperty("typeVoucher")] public string? TypeVoucher { get; set; }
    [JsonProperty("value")] public decimal Value { get; set; }
}

public class PlgCheckCouponRequest
{
    [JsonProperty("isWeb")] public bool isWeb { get; set; }
    [JsonProperty("storeNo")] public string? storeNo { get; set; }
    [JsonProperty("posID")] public string? posID { get; set; }
    [JsonProperty("userPOS")] public string? userPOS { get; set; }
    [JsonProperty("listSeriNo")] public List<PlgSeriNoCouponRequest>? listSeriNo { get; set; }
}

public class PlgSeriNoCouponRequest
{
    [JsonProperty("seriNo")] public string? seriNo { get; set; }
    [JsonProperty("quantity")] public int quantity { get; set; }
    [JsonProperty("isEmployee")] public bool isEmployee { get; set; }
}

public class PlgCouponResponse
{
    [JsonProperty("statusCoupon")] public string? StatusCoupon { get; set; }
    [JsonProperty("descCoupon")] public string? DescCoupon { get; set; }
    [JsonProperty("itemNo")] public string? ItemNo { get; set; }
    [JsonProperty("quantity")] public int Quantity { get; set; }
    [JsonProperty("isEmployee")] public bool IsEmployee { get; set; }
}

public class PlgRedeemVoucherRequest
{
    [JsonProperty("partner")] public string? partner { get; set; }
    [JsonProperty("orderNo")] public string? orderNo { get; set; }
    [JsonProperty("storeNo")] public string? storeNo { get; set; }
    [JsonProperty("posID")] public string? posID { get; set; }
    [JsonProperty("totalBill")] public double totalBill { get; set; }
    [JsonProperty("staffCode")] public string? staffCode { get; set; }
    [JsonProperty("listSeriNo")] public List<PlgRedeemSeriNoItem>? listSeriNo { get; set; }
}

public class PlgRedeemSeriNoItem
{
    [JsonProperty("seriNo")] public string? seriNo { get; set; }
    [JsonProperty("isVoucher")] public bool isVoucher { get; set; }
    [JsonProperty("value")] public double value { get; set; }
}

public class PlgRedeemVoucherResponse
{
    [JsonProperty("seriNo")] public string? SeriNo { get; set; }
    [JsonProperty("message")] public string? Message { get; set; }
    [JsonProperty("statusVoucher")] public bool StatusVoucher { get; set; }
    [JsonProperty("amount")] public decimal Amount { get; set; }
}

public class PlgRedeemCouponRequest
{
    [JsonProperty("orderNo")] public string? orderNo { get; set; }
    [JsonProperty("storeNo")] public string? storeNo { get; set; }
    [JsonProperty("posID")] public string? posID { get; set; }
    [JsonProperty("staffCode")] public string? staffCode { get; set; }
    [JsonProperty("listSeriNo")] public List<PlgRedeemCouponItem>? listSeriNo { get; set; }
}

public class PlgRedeemCouponItem
{
    [JsonProperty("seriNo")] public string? seriNo { get; set; }
    [JsonProperty("quantityRedeem")] public int quantityRedeem { get; set; }
}

public class PlgRedeemCouponResponse
{
    [JsonProperty("seriNo")] public string? SeriNo { get; set; }
    [JsonProperty("message")] public string? Message { get; set; }
    [JsonProperty("statusCode")] public bool StatusCode { get; set; }
    [JsonProperty("quantityRemain")] public int QuantityRemain { get; set; }
}

// ── SendCodeCpnVch ────────────────────────────────────────────────────────────
public class CpnVchCodeSendModel
{
    public string? SerialNumber { get; set; }
    public string? ItemNo { get; set; }
    public string? ItemName { get; set; }
    public string? OrderNo { get; set; }
    public string? StatusCode { get; set; }
    public string? Status { get; set; }
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? ActicleType { get; set; }
    public bool IsCheckItem { get; set; }
    public int MaxQtyUse { get; set; }
    public double MaxAmount { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public double ValueCpnVch { get; set; }
    public string? PhoneNumber { get; set; }
}

public class CpnVchCodeSendQuota
{
    public string? ItemNo { get; set; }
    public int QtyIssue { get; set; }
    public int LimitQty { get; set; }
    public int QtyOfDay { get; set; }
    public bool Blocked { get; set; }
}

public class CpnVchCodeQuotaRemn
{
    public string? ItemNo { get; set; }
    public DateTime OrderDate { get; set; }
    public string? PhoneNumber { get; set; }
    public int Qty { get; set; }
    public int InitQuota { get; set; }
}

public class CpnVchCodeSendInsertDto
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public DateTime? Date { get; set; }
    public string? OrderNo { get; set; }
    public string? ItemNo { get; set; }
    public string? Code { get; set; }
    public string? PhoneNumber { get; set; }
}
