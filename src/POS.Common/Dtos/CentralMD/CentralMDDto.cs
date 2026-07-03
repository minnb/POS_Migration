using System.ComponentModel.DataAnnotations;
using POS.Common.Dtos.Loyalty;

namespace POS.Common.Dtos.CentralMD;

public class StoreDto
{
    public string? StoreNo { get; set; }
    public string? No { get; set; }
    public string? ResponsibilityCenter { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? PostCode { get; set; }
    public string? TaxCode { get; set; }
    public string? BusinessAreaNo { get; set; }
    public string? ConnectionString { get; set; }
}

public class StoreListDto
{
    public string  StoreNo           { get; set; } = string.Empty;
    public string? Name              { get; set; }
    public string? Address           { get; set; }
    public string? PhoneNo           { get; set; }
    public string? StyleProfile      { get; set; }
    public string? PrintReceiptLogo  { get; set; }
    public string? BranchNo          { get; set; }
    public DateTime? LastDateModified { get; set; }
    public int     ClosingMethod     { get; set; }
    public bool    IsActive          => ClosingMethod == 0;
}

public class BranchDto
{
    public string No { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class BranchAdminDto
{
    public string No { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? VATRegistrationNo { get; set; }
}

public class BranchCreateDto
{
    public string No { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? VATRegistrationNo { get; set; }
}

public class StoreCreateDto
{
    public string StoreNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? BranchNo { get; set; }
    public int ClosingMethod { get; set; }
}

public class StoreSetup
{
    public string? StoreNo { get; set; }
    public string? Code { get; set; }
    public string? Value { get; set; }
    public bool Status { get; set; }
}

public class SysWebApiConfig
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Prefix { get; set; }
    public string? ConnectionString { get; set; }
    public string? Description { get; set; }
    public string? ProcedureName { get; set; }
    public bool Blocked { get; set; }
}

public class StoreSetConfig : SysWebApiConfig
{
    public string StoreNo { get; set; } = string.Empty;
}

public class SyncTableList
{
    public string? FileName { get; set; }
    public string? TableName { get; set; }
    public string Action { get; set; } = "DELETE-INSERT";
    public string? ProcedureName { get; set; }
    public string? ProcessID { get; set; }
    public object? Data { get; set; }
}

public class CpnVchCodeQuotaRemn
{
    public string? PhoneNumber { get; set; }
    public string? ItemNo { get; set; }
    public DateTime OrderDate { get; set; }
    public int InitQuota { get; set; }
    public int Qty { get; set; }
}

public class CpnVchCodeSendQuota
{
    public string? ItemNo { get; set; }
    public DateTime StartingDate { get; set; }
    public DateTime EndingDate { get; set; }
    public bool IsCheckMember { get; set; }
    public int QtyOfDay { get; set; }
    public int LimitQty { get; set; }
    public int QtyIssue { get; set; }
    public bool Blocked { get; set; }
}

public class CpnVchCodeSendDto
{
    public long ID { get; set; }
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public DateTime? Date { get; set; }
    public string? OrderNo { get; set; }
    public string? ItemNo { get; set; }
    public string? Code { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class CpnVchBOMHeaderDto
{
    public string? ItemNo { get; set; }
    public string? ItemName { get; set; }
    public string? UnitOfMeasure { get; set; }
    public int DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal MaxAmount { get; set; }
    public string? ArticleType { get; set; }
    public decimal ValueOfVoucher { get; set; }
    public DateTime StartingDate { get; set; }
    public DateTime EndingDate { get; set; }
    public byte Blocked { get; set; }
    public string? CouponCode { get; set; }
    public int LimitQty { get; set; }
    public DateTime LastDateModified { get; set; }
    public long? Counter { get; set; }
    public bool? IsCheckItem { get; set; }
    public string? Pkey { get; set; }
    public bool? IsMultiUse { get; set; }
    public int? LimitQtyUsed { get; set; }
    public string? CpnVchType { get; set; }
    public bool? IsCheckAPI { get; set; }
    public string? SaleType { get; set; }
    public string? StoreGroupCode { get; set; }
    public double? MinAmount { get; set; }
    public int? MinValueType { get; set; }
    public int? IsTotalBill { get; set; }
    public int? ActiveAfterDay { get; set; }
    public int? ValidDay { get; set; }
}

public class CpnVchBOMLineDto
{
    public string? ItemNo { get; set; }
    public int LineNo { get; set; }
    public string? LineItemNo { get; set; }
    public string? Description { get; set; }
    public string? UnitOfMeasure { get; set; }
    public long? Counter { get; set; }
    public string? Barcode { get; set; }
    public string? Pkey { get; set; }
}

public class ItemDto
{
    public string? ItemNo { get; set; }
    public string? Uom { get; set; }
    public string? DivisionCode { get; set; }
    public string? TaxGroupCode { get; set; }
    public decimal VATPercent { get; set; }
    public bool IsVAT { get; set; }
}

public class ItemPointsMemberDto
{
    public string? PointsCode { get; set; }
    public string? ItemNo { get; set; }
    public string? Barcode { get; set; }
    public string? ItemName { get; set; }
    public string? Uom { get; set; }
    public int ShelfLife { get; set; }
    public int DaysOfUsed { get; set; }
    public bool Blocked { get; set; }
}

public class LoyaltyRateDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? Code { get; set; }
    public decimal Rate { get; set; }
    public bool Blocked { get; set; }
    public string? Pkey { get; set; }
    public string? CardType { get; set; }
}

public class MMLSchemeHeader
{
    public string? HeaderCode { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal MinAmount { get; set; }
    public bool IsMember { get; set; }
    public bool IsCallAPI { get; set; }
    public string? Ref1 { get; set; }
    public string? Ref2 { get; set; }
    public string? Ref3 { get; set; }
    public string? Ref4 { get; set; }
    public string? Ref5 { get; set; }
}

public enum MMLSchemeItemEnum
{
    WECO = 1,
    MML = 2,
}

public class MMLSchemeItem
{
    public string? HeaderCode { get; set; }
    public string? Code { get; set; }
    public string? ItemNo { get; set; }
    public string? UOM { get; set; }
    public string? CategoryCode { get; set; }
    public bool Enabled { get; set; }
    public string? Ref1 { get; set; }
    public string? Ref2 { get; set; }
    public string? Ref3 { get; set; }
    public string? Ref4 { get; set; }
    public string? Ref5 { get; set; }
}

public enum MMLSchemeResponseEnum
{
    Journey_1 = 1,
    Journey_2 = 2,
    Journey_3 = 3,
    Journey_4 = 4,
}

public class MMLSchemeResponse
{
    public string? HeaderCode { get; set; }
    public string? Code { get; set; }
    public string? Title { get; set; }
    public string? Link { get; set; }
    public bool IsGenQR { get; set; }
    public bool Enabled { get; set; }
    public string? Description { get; set; }
    public string? Ref1 { get; set; }
    public string? Ref2 { get; set; }
    public string? Ref3 { get; set; }
    public string? Ref4 { get; set; }
    public string? Ref5 { get; set; }
}

public class MMLSchemeRequest
{
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string Code { get; set; } = string.Empty;
    public string? MemberCardNo { get; set; }
    public string? UserId => MemberCardNo;
    public DateTime OrderTime { get; set; } = DateTime.Now;
    public bool IsMember { get; set; }
    public List<MMLSchemeItemsRequest>? Items { get; set; }
    public List<PaymentEntryLoyalty>? Payments { get; set; }
}

public class MMLSchemeItemsRequest
{
    public int LineNo { get; set; }
    public string? ItemNo { get; set; }
    public string? ItemName { get; set; }
    public string? UOM { get; set; }
    public string? Barcode { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineAmountIncVAT { get; set; }
    public string? PackId { get; set; }
}
