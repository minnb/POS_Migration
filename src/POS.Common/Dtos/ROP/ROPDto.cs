using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.ROP;

public class GenOspQRLogin
{
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
}

public class GenOspQRLoginROP
{
    public string? Site { get; set; }
    public string? PosTerminal { get; set; }
}

public class InsertVoucherROP
{
    public string? SiteCode { get; set; }
    public string? POSTerminal { get; set; }
    public string? ArticleType { get; set; }
    public IList<InsertVoucherDataROP>? Vouchers { get; set; }
}

public class InsertVoucherDataROP
{
    public string? VoucherCode { get; set; }
    public decimal VoucherValue { get; set; }
    public int VoucherPercent { get; set; }
    public string? OrderNumber { get; set; }
    public string? BonusBuy { get; set; }
    public string? SAPCode { get; set; }
    public int StatusId { get; set; }
    public string? ValidFrom { get; set; }
    public string? ValidTo { get; set; }
}

public class RspVoucherInfoROP
{
    public string? SAPCode { get; set; }
    public string? NameCode { get; set; }
    public string? SerialNumber { get; set; }
    public string? VoucherCode { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.Now;
    public DateTime ValidTo { get; set; } = DateTime.Now;
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

public class RspVoucherInfoNullROP
{
    public string? SAPCode { get; set; }
    public string? NameCode { get; set; }
    public string? SerialNumber { get; set; }
    public string? VoucherCode { get; set; }
    public DateTime? ValidFrom { get; set; } = DateTime.Now;
    public DateTime? ValidTo { get; set; } = DateTime.Now;
    public decimal VoucherValue { get; set; }
    public decimal VoucherPercent { get; set; }
    public string? BonusBuy { get; set; }
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? StorageSite { get; set; }
    public string? VoucherTypeName { get; set; }
    public string? CompanyCode { get; set; }
}

public class CheckVouchersROP
{
    public string[]? VoucherNumbers { get; set; }
    public string? Site { get; set; }
    public string? PosTerminal { get; set; }
    public string? OrderNumber { get; set; }
}

public class UpdateVoucherStatusROP : CheckVouchersROP
{
    public int Status { get; set; }
}

public class ResponseAppWincare
{
    public int ResponseCode { get; set; }
    public object? Data { get; set; }
    public string[]? TechnicalMessage { get; set; }
    public string? Message { get; set; }
}

public class ResponseROP
{
    public int ResponseCode { get; set; }
    public object? Data { get; set; }
    public string[]? TechnicalMessage { get; set; }
    public string? Message { get; set; }
}

public class ResponseDataStringROP
{
    public int ResponseCode { get; set; }
    public string? Data { get; set; }
    public string[]? TechnicalMessage { get; set; }
    public string? Message { get; set; }
}

public class TokenData
{
    public string? Token { get; set; }
    public string? TokenType { get; set; }
}

public class DataResponseROP
{
    public string? Status { get; set; }
    public string? Message { get; set; }
}

public class StaffPointGetInfoBarcode
{
    public int CapiId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? PhoneNumber { get; set; }
}
