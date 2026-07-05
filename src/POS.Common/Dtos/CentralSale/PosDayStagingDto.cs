namespace POS.Common.Dtos.CentralSale;

// Ported from src/legacy/VCM.BLUEPOS.Model/StoreActivities/SaleBusinessStoreModel.cs (rút gọn theo UI mới)
public sealed class PosDayStagingDto
{
    public string PosTerminal { get; set; } = string.Empty;
    public string? Placement { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? CloseTime { get; set; }
    public DateTime? LastSaleTime { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TienMat { get; set; }
    public int CustomerCount { get; set; }
    public int TotalQuantity { get; set; }
}
