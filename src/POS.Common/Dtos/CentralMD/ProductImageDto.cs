namespace POS.Common.Dtos.CentralMD;

public sealed class ProductImageDto
{
    public string ItemNo      { get; set; } = string.Empty;
    public string Uom         { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
}
