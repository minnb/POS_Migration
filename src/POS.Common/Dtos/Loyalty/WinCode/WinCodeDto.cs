namespace POS.Common.Dtos.Loyalty.WinCode;

public class WinCodeCustomerDto
{
    public Guid ID { get; set; }
    public string? WinCode { get; set; }
    public string? MemberCard { get; set; }
    public string? Csn { get; set; }
    public int QuantityRecieptedSum { get; set; }
    public int QuantityReciepted { get; set; }
    public string? OrderNo { get; set; }
    public string? StoreNo { get; set; }
    public string? PosNo { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime TransDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class WinCodeStoreDto
{
    public Guid ID { get; set; }
    public string? ProgramCode { get; set; }
    public string? StoreNo { get; set; }
    public bool Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? Pkey { get; set; }
    public long Counter { get; set; }
}

public class WinCodeHeaderDto
{
    public Guid ID { get; set; }
    public string? ProgramCode { get; set; }
    public string? WinCode { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int Quantity { get; set; }
    public bool Status { get; set; }
    public string? DiscountType { get; set; }
    public string? ApplyType { get; set; }
    public string? Pkey { get; set; }
    public long Counter { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public class WinCodeMemoryDto
{
    public string? ProgramCode { get; set; }
    public string? WinCode { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int Quantity { get; set; }
    public bool Status { get; set; }
    public List<WinCodeStoreMemoryDto>? ListStore { get; set; }
}

public class WinCodeStoreMemoryDto
{
    public string? ProgramCode { get; set; }
    public string? StoreNo { get; set; }
    public bool Status { get; set; }
    public string? WinCode { get; set; }
    public int Quantity { get; set; }
    public string? DiscountType { get; set; }
    public string? ApplyType { get; set; }
}

public class WincodeResult
{
    public string? WinCode { get; set; }
    public string? ProgramCode { get; set; }
    public int? Quantity { get; set; }
    public string? DiscountType { get; set; }
    public string? MerchantId { get; set; }
}
