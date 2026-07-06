namespace POS.Common.Dtos.CentralMD;

public sealed class ProductDetailDto
{
    public string ItemNo             { get; set; } = string.Empty;
    public string ItemName           { get; set; } = string.Empty;
    public string ItemNameFull       { get; set; } = string.Empty;
    public string BaseUnitOfMeasure  { get; set; } = string.Empty;
    public string SalesUnitOfMeasure { get; set; } = string.Empty;
    public string ItemFamilyCode     { get; set; } = string.Empty;
    public string TaxGroupCode       { get; set; } = string.Empty;
    public byte   Blocked            { get; set; }
    public byte   BlockedVINID       { get; set; }
    public List<BarcodeRowDto> Barcodes { get; set; } = [];
    public string? ImageBase64       { get; set; }
}
