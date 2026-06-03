namespace POS.Application.Common.DTOs;

public class POSDocumentNoResponse
{
    public string? StoreNo { get; set; }
    public string? POSTerminal { get; set; }
    public string? LastNumber { get; set; }
    public DateTime? LastDateTime { get; set; }
    public string? DocumentType { get; set; }
}
