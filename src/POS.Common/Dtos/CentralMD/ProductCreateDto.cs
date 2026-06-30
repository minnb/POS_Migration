namespace POS.Common.Dtos.CentralMD;

public sealed class ProductCreateDto
{
    public string ItemName           { get; set; } = string.Empty;
    public string ItemNameFull       { get; set; } = string.Empty;
    public string BaseUnitOfMeasure  { get; set; } = string.Empty;
    public string SalesUnitOfMeasure { get; set; } = string.Empty;
    public string ItemFamilyCode     { get; set; } = string.Empty;
    public string TaxGroupCode       { get; set; } = string.Empty;  // "" = no tax
    public byte   Blocked            { get; set; } = 0;             // 0=Active, 1=Inactive
    public byte   BlockedVINID       { get; set; } = 0;             // 0=Không tích điểm, 1=Tích điểm
    public List<BarcodeRowDto> Barcodes { get; set; } = [];
    public string CreatedUser        { get; set; } = string.Empty;
}

public sealed class BarcodeRowDto
{
    public string BarcodeNo         { get; set; } = string.Empty;
    public string UnitOfMeasureCode { get; set; } = string.Empty;
}

// Dropdown loại hàng từ dbo.ArticleType
// Xác nhận tên cột với DBA nếu khác "Code"
public sealed class ArticleTypeDto
{
    public string? Code { get; set; }
}

// Dropdown đơn vị đo từ dbo.UnitOfMeasure
// Xác nhận tên cột với DBA nếu khác "Code"
public sealed class UnitOfMeasureDto
{
    public string? Code { get; set; }
}
