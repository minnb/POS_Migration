namespace POS.Common.Dtos.CentralSale;

public class RevenueDailyDto
{
    public DateTime SaleDate { get; set; }
    public decimal NetRevenue { get; set; }
    public int SaleCount { get; set; }
    public int ReturnCount { get; set; }
}

public class RevenueHourlyDto
{
    public int Hour { get; set; }
    public decimal NetRevenue { get; set; }
    public int TransactionCount { get; set; }
}

public class RevenueSummaryDto
{
    public decimal TodayRevenue { get; set; }
    public int TodayOrders { get; set; }
    public int TodayReturns { get; set; }
}
