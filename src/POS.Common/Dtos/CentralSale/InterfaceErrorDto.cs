namespace POS.Common.Dtos.CentralSale;

public sealed class InterfaceErrorDto
{
    public int ErrorID { get; set; }
    public string? UserName { get; set; }
    public int? ErrorNumber { get; set; }
    public int? ErrorState { get; set; }
    public int? ErrorSeverity { get; set; }
    public int? ErrorLine { get; set; }
    public string? ErrorProcedure { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ErrorDateTime { get; set; }
}

public sealed class InterfaceErrorSummaryDto
{
    public int TotalErrors { get; set; }
    public int TodayErrors { get; set; }
    public int UniqueProcedures { get; set; }
    public DateTime? LastErrorAt { get; set; }
}
