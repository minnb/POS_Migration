namespace POS.Common.Dtos.CentralMD;

public sealed class ProductLockItemDto
{
    public string?   ItemNo        { get; set; }
    public string?   ItemName      { get; set; }
    public string?   UnitOfMeasure { get; set; }
    public bool      IsLocked      { get; set; }   // true=Đang bị khóa, false=Đang hoạt động
    public DateTime? UpdatedDate   { get; set; }
    public int       Total         { get; set; }   // pagination total (SP inject mọi row)
}

public sealed class ProductLockFilter
{
    public string StoreNo   { get; set; } = string.Empty;
    public int    Status    { get; set; } = -1;    // -1=Tất cả, 0=Đang hoạt động, 1=Đang bị khóa
    public string ItemNo    { get; set; } = string.Empty;
    public string ItemName  { get; set; } = string.Empty;
    public int    PageSize   { get; set; } = 10;
    public int    PageNumber { get; set; } = 0;
}

public sealed class ProductLockSaveDto
{
    public string       StoreNo    { get; set; } = string.Empty;
    public List<string> ItemNos    { get; set; } = [];
    public bool         TargetLock { get; set; }   // true=khóa, false=mở khóa
    public string       Actor      { get; set; } = string.Empty;
}
