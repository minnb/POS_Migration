namespace POS.Common.Dtos.CentralSale;

public sealed class ShiftNumberSummaryDto
{
    public int ShiftNumber { get; set; }
    public int TotalOccurrences { get; set; }
    public int ClosedCount { get; set; }
    public int OpenCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public int DiscrepancyCount { get; set; }
    public decimal TotalChenLech { get; set; }

    public decimal AvgRevenue => TotalOccurrences > 0 ? TotalRevenue / TotalOccurrences : 0;
    public decimal AvgTransactions => TotalOccurrences > 0 ? (decimal)TotalTransactions / TotalOccurrences : 0;
}
