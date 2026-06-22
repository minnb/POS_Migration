namespace POS.Common.Dtos.CentralSale;

public class DataRawJsonSummaryDto
{
    public int TotalToday    { get; set; }
    public int TotalInPeriod { get; set; }
    public int TotalSuccess  { get; set; }
    public int TotalFailed   { get; set; }
    public DateTime? LastCrtDate { get; set; }
}
