using System.ComponentModel.DataAnnotations;
using POS.Common.Validation;

namespace POS.Common.Dtos.SAP;

public class CreateVoucherModel
{
    [Required]
    public string VoucherNumber { get; set; } = string.Empty;
    public decimal Value { get; set; }
    [Required]
    public string From_Date { get; set; } = string.Empty;
    [Required]
    public string Expiry_Date { get; set; } = string.Empty;
    [Required]
    public string SiteCode { get; set; } = string.Empty;
    public string BonusBuy { get; set; } = string.Empty;
    public string Article_No { get; set; } = string.Empty;
    [Required]
    public string POSTerminal { get; set; } = string.Empty;
    public string OrderNo { get; set; } = string.Empty;
    public string VoucherType { get; set; } = string.Empty;

}

public class VoucherUpdateModel
{
    public string OrderNo { get; set; } = string.Empty;
    public double TotalBill { get; set; } = 0;
    public string SiteCode { get; set; } = string.Empty;
    public string POSTerminal { get; set; } = string.Empty;
    public bool IsVoucher { get; set; } = true;
    public bool IsRetry { get; set; } = false;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<VoucherUpdateSerial>? ListSeriNo { get; set; }
}

public class VoucherUpdateSerial
{
    public string CompanyCode { get; set; } = string.Empty;
    public string Partner { get; set; } = string.Empty;
    public string voucherNumber { get; set; } = string.Empty;
    public bool isVoucher { get; set; }
    public double value { get; set; }
    public string articleNo { get; set; } = string.Empty;
    public string articleType { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
}

public class VoucherUpdateRequest
{
    public string CompanyCode { get; set; } = string.Empty;
    [Required]
    public string VoucherNumber { get; set; } = string.Empty;
    public string ArticleNo    { get; set; } = string.Empty;
    public string ArticleType  { get; set; } = string.Empty;
    [Required]
    [StringRange("SOLD", "RDM", "EXP", ErrorMessage = "Trạng thái không hợp lệ (SOLD/RDM/EXP)")]
    public string Status       { get; set; } = "RDM";
    [Required]
    [StringLength(6, MinimumLength = 4)]
    public string SiteCode     { get; set; } = string.Empty;
    [Required]
    public string POSTerminal  { get; set; } = string.Empty;
    public string OrderNo      { get; set; } = string.Empty;
}
