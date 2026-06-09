using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.Loyalty.ProgramPoints;

public class ProgramPointsDto
{
    public int ProgramId { get; set; }
    public string? ProgramName { get; set; }
    public string? ProgramType { get; set; }
    public string? CumulativeType { get; set; }
    public string? ClubCode { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<ProgramPointsItemDto>? Items { get; set; }
    public List<ProgramPointsStoreDto>? Stores { get; set; }
}

public class ProgramPointsItemDto
{
    public int ProgramId { get; set; }
    public string? ItemNo { get; set; }
    public string? Uom { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal LimitedQty { get; set; }
}

public class ProgramPointsStoreDto
{
    public int ProgramId { get; set; }
    public string? StoreNo { get; set; }
    public int LimitedQty { get; set; }
}

public class ProgramPointsRemn
{
    public string? ClubCode { get; set; }
    public string? MemberCard { get; set; }
    public decimal PreviousPoint { get; set; }
    public decimal CommitPoints { get; set; }
    public decimal AvailablePoints { get; set; }
}

public class ProgramPointsRemnItem
{
    public string? ClubCode { get; set; }
    public string? StoreNo { get; set; }
    public string? ItemNo { get; set; }
    public string? Uom { get; set; }
    public decimal LimitedQty { get; set; }
    public decimal UsedQty { get; set; }
    public int Rate { get; set; }
}

public class ProgramPointsSetup
{
    public int ProgramId { get; set; }
    public string? CumulativeType { get; set; }
    public string? ClubCode { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? Description { get; set; }
    public bool Blocked { get; set; }
    public DateTime CrtDate { get; set; }
    public DateTime ChgDate { get; set; }
}

public class ProgramPointsSetupItem
{
    public int ProgramId { get; set; }
    public string? Type { get; set; }
    public string? ItemNo { get; set; }
    public string? ItemName { get; set; }
    public string? Uom { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal LimitedQty { get; set; }
    public bool Blocked { get; set; }
}

public class ProgramPointsSetupStore
{
    public int ProgramId { get; set; }
    public string? StoreNo { get; set; }
    public int LimitedQty { get; set; }
    public bool Blocked { get; set; }
}

public class ProgramPointsTransaction
{
    public string? StoreNo { get; set; }
    public string? PosNo { get; set; }
    public string? Type { get; set; }
    public int ProgramId { get; set; }
    public string? ClubCode { get; set; }
    public string? MemberCard { get; set; }
    public string? OrderNo { get; set; }
    public string? ReturnedOrderNo { get; set; }
    public int TransactionType { get; set; }
    public decimal TotalPoint { get; set; }
    public DateTime CrtDate { get; set; }
}

public class ProgramPointsTransactionDto
{
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string MemberCard { get; set; } = string.Empty;
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    [Required]
    public int TransactionType { get; set; }
    public string? ReturnedOrderNo { get; set; }
    public List<ProgramPointsTransactionItem>? Items { get; set; }
}

public class ProgramPointsTransactionItem
{
    [Required]
    public int LineNo { get; set; }
    [Required]
    public int ProgramId { get; set; }
    [Required]
    public string ItemNo { get; set; } = string.Empty;
    [Required]
    public string Uom { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    [Required]
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal TotalPoint { get; set; }
}

public class ProgramPointsTransactionItemResponse
{
    public int LineNo { get; set; }
    public int ProgramId { get; set; }
    public string? ProgramType { get; set; }
    public string? ItemNo { get; set; }
    public string? Uom { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal TotalPoint { get; set; }
}

public class ProgramPointsTransactionSuccess
{
    public string? ProgramName { get; set; }
    public string? ClubCode { get; set; }
    public decimal EarnPoint { get; set; }
    public decimal RedeemPoint { get; set; }
    public decimal AvailablePoints { get; set; }
    public List<ProgramPointsTransactionItemResponse>? Items { get; set; }
}
