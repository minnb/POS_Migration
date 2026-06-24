namespace POS.Common.Dtos.CentralSale;

public class DataRawJsonLogDto
{
    public Guid    Id            { get; set; }
    public string? TransactionId { get; set; }
    public string? DataType      { get; set; }
    public string? Message       { get; set; }
    public bool    Flag          { get; set; }
    public string? ErrorMessage  { get; set; }
    public string? Source        { get; set; }
    public DateTime CrtDate      { get; set; }
}
