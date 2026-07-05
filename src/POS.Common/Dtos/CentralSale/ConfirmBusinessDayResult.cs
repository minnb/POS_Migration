namespace POS.Common.Dtos.CentralSale;

public sealed class ConfirmBusinessDayResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? NewBusinessDate { get; set; }
}
