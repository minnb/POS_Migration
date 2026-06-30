namespace POS.Common.Dtos.CentralMD;

public sealed class ProductListItemDto
{
    public int     ID                  { get; set; }
    public string? ItemNo              { get; set; }
    public string? ItemNo_PLG          { get; set; }
    public string? ItemName1           { get; set; }
    public string? ItemName2           { get; set; }
    public string? SalesUnit           { get; set; }
    public string? BarcodeNo           { get; set; }
    public string? BarcodeUnit         { get; set; }
    public string? TaxGroupCode        { get; set; }
    public string? ParentCode          { get; set; }
    public string? Size                { get; set; }
    public string? LastDateModifiedStr { get; set; }
    public int     Total               { get; set; }
}

public sealed class ProductListFilter
{
    public string ItemNo    { get; set; } = string.Empty;
    public string ItemName  { get; set; } = string.Empty;
    public string BarcodeNo { get; set; } = string.Empty;
    public string TaxCode   { get; set; } = string.Empty;
    public int    PageSize   { get; set; } = 10;
    public int    PageNumber { get; set; } = 0;
}

public sealed class PosVatCodeDto
{
    // Tên cột: xác nhận với DBA nếu SP trả tên khác.
    public string? Code        { get; set; }
    public string? Description { get; set; }
}
