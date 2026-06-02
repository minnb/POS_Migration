namespace VCM.POSBLUE.Shared.DTOs;

// Các read model dùng cho MemoryCacheService (giữ nguyên field từ bản .NET Framework cũ).

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

public class StoreSetup
{
    public string? StoreNo { get; set; }
    public string? Code { get; set; }
    public string? Value { get; set; }
    public bool Status { get; set; }
}

public class SysWebApi
{
    public string? AppCode { get; set; }
    public string? Host { get; set; }
    public string? Version { get; set; }
    public string? Authorization { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? PublicKey { get; set; }
    public string? PrivateKey { get; set; }
    public bool Blocked { get; set; }
    public string? HttpProxy { get; set; }
    public string? Bypasslist { get; set; }
    public string? Description { get; set; }
}

public class SysWebApiDto : SysWebApi
{
    public List<SysWebApiRoute>? SysWebApiRoute { get; set; }
}

public class SysWebApiRoute
{
    public string? AppCode { get; set; }
    public string? Name { get; set; }
    public string? Route { get; set; }
    public string? Description { get; set; }
    public bool Blocked { get; set; }
    public string? Version { get; set; }
    public string? Notes { get; set; }
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

public class OfferStaffSetupDto
{
    public string? ClubCode { get; set; }
    public DateTime ApplyFrom { get; set; }
    public DateTime ValidBefore { get; set; }
    public bool IsMonth { get; set; }
    public bool Blocked { get; set; }
    public decimal InitQuota { get; set; }
    public string? Description { get; set; }
}

public class NotifyConfigDto
{
    public string? AppCode { get; set; }
    public string? ActionType { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public string? MessageFormat { get; set; }
    public bool Blocked { get; set; }
    public bool IsOffline { get; set; }
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

public class CardLevelDto
{
    public string? AppCode { get; set; }
    public string? Level { get; set; }
    public string? ToLevel { get; set; }
    public bool Blocked { get; set; }
    public string? Description { get; set; }
}

public class StagingDBConfigDto
{
    public string? Action { get; set; }
    public string? Execution { get; set; }
    public string? Description { get; set; }
    public bool Blocked { get; set; }
}

public class MemoryCacheConfig
{
    public string? Code { get; set; }
    public string? Desc { get; set; }
    public string? MemoryCacheType { get; set; }
    public bool Blocked { get; set; }
    public string? ListStore { get; set; }
}

public class StoreMappingModel
{
    public int ID { get; set; }
    public string? OldTerminalID { get; set; }
    public string? OldStoreID { get; set; }
    public string? OldVinPayTerminalID { get; set; }
    public string? OldVinPayStoreID { get; set; }
    public string? NewTerminalID { get; set; }
    public string? NewStoreID { get; set; }
    public DateTime? CreatedDate { get; set; }
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
}

public class WinCodeStoreDto
{
    public Guid ID { get; set; }
    public string? ProgramCode { get; set; }
    public string? StoreNo { get; set; }
    public bool Status { get; set; }
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
