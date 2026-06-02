using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace VCM.POSBLUE.Shared.DTOs;

// Request/Response DTOs cho ValidateController — port nguyên field từ bản cũ.

// ── POST api/v2/telegram/send ────────────────────────────────────────────────
public class MessageTelegramRequest
{
    [Required] public string MessageType { get; set; } = "";
    [Required] public string Message { get; set; } = "";
}

// ── GET api/v2/validate/tax-code ─────────────────────────────────────────────
public class PartnerApiMeta
{
    [JsonProperty("code")] public int Code { get; set; }
    [JsonProperty("message")] public string? Message { get; set; }
}

public class PartnerApiResponse<T>
{
    [JsonProperty("meta")] public PartnerApiMeta Meta { get; set; } = new();
    [JsonProperty("data")] public T? Data { get; set; }
}

public class TaxCustInfo
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

// ── GET api/v2/validate/transaction ──────────────────────────────────────────
public class ValidateTransactionDto
{
    public string? StoreNo { get; set; }
    public string? StoreName { get; set; }
    public string? Address { get; set; }
    public string? OrderNo { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime OrderTime { get; set; }
    public string? CustomerName { get; set; }
    public string? MemberCardNo { get; set; }
    public int? MemberPointsEarn { get; set; }
    public int TransactionType { get; set; }
    public int DeliveringMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public int SalesIsReturn { get; set; }
    public string? ReturnedOrderNo { get; set; }
    public string? RefKey1 { get; set; }
    public string? CQT { get; set; }
    public bool IsEOD { get; set; }
    public string? Key { get; set; }
    public string? CompTaxCode { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<ValidateTransactionLine>? TransLine { get; set; }
}

public class ValidateTransactionLine
{
    public int LineNo { get; set; }
    public string? ItemNo { get; set; }
    public string? ItemName { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineAmountIncVAT { get; set; }
    public int VATPercent { get; set; }
    public decimal VATAmount { get; set; }
    public string? DivisionCode { get; set; }
    public string? Barcode { get; set; }
}

public class CashOrderSentToEInvoice
{
    public string? CompTaxCode { get; set; }
    public string? Key { get; set; }
    public string? OrderNo { get; set; }
    public bool IsEOD { get; set; }
}

// ── POST api/v2/invoice/create ────────────────────────────────────────────────
public class InvoiceCreatedRequest
{
    [Required] public string SiteNo { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? PhoneNumber { get; set; }
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
    public string? Email { get; set; }
    public string? Address { get; set; }
    [Required] public string[] OrderNo { get; set; } = [];
    public string? Passport { get; set; }
    public string? CCCD { get; set; }
    public string? DVQHNS { get; set; }
}

public class InvoiceCreatedItem
{
    public string? SiteNo { get; set; }
    public string? OrderNo { get; set; }
    public string? CustomerName { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsNotVAT { get; set; }
    public string IsNotVATPartner { get; set; } = "WCM";
    public string? Id { get; set; }
    public string? Passport { get; set; }
    public string? CCCD { get; set; }
    public string? DVQHNS { get; set; }
}

// ── POST api/v2/validate/member-business ─────────────────────────────────────
public class ValidateMemberBusinessRequest
{
    [Required] public string StoreNo { get; set; } = "";
    [Required] public string PosNo { get; set; } = "";
    [Required] public string MemberCard { get; set; } = "";
    [Required] public string Key { get; set; } = "";
}

public class CheckMemberBusinessData
{
    public bool IsChecked { get; set; }
    public string? Description { get; set; }
    public MemberBusinessData? MemberBusinessData { get; set; }
}

public class MemberBusinessData
{
    public string? Month { get; set; }
    public string? CardLevel { get; set; }
    public string? MemberCard { get; set; }
    public string? MemberStore { get; set; }
    public bool IsDiscount { get; set; }
    public string? Key { get; set; }
    public ExistsProcessing? ExistsProcessing { get; set; }
    public List<MemberBusinessItemQuota>? Items { get; set; }
}

public class MemberBusinessItemQuota
{
    public string? ItemNo { get; set; }
    public string? Uom { get; set; }
    public decimal MaxValue { get; set; }
    public decimal UsedValue { get; set; }
    public decimal RemnValue { get; set; }
}

public class ExistsProcessing
{
    public string? PosNo { get; set; }
    public string? KeyExists { get; set; }
}

// Dapper read models (internal — không trả về client)
public class MemberBusinessDbDto
{
    public string? MemberCard { get; set; }
    public string? MemberStore { get; set; }
    public string? StoreNo { get; set; }
    public string? CardLevel { get; set; }
    public bool Blocked { get; set; }
}

public class MemberBusinessItemDb
{
    public string? MemberCard { get; set; }
    public string? Month { get; set; }
    public string? CardLevel { get; set; }
    public string? ItemNo { get; set; }
    public string? Uom { get; set; }
    public decimal MaxValue { get; set; }
    public decimal UsedValue { get; set; }
    public decimal RemnValue { get; set; }
}
