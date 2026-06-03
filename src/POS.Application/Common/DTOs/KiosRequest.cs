namespace POS.Application.Common.DTOs;

public class KiosInsertSalePOSRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? SaleJson { get; set; }
}

public class KiosCheckOrderResponse
{
    public string? OrderNo { get; set; }
    public string? PosNo { get; set; }
    public int StepProcess { get; set; }
    public string? AppCode { get; set; }
    public DateTime OrderDate { get; set; }
    public string? CustomerName { get; set; }
    public string? CSN { get; set; }
    public string? MemberCardNo { get; set; }
}

public class KiosInsertSaleRequest
{
    public string? StoreNo { get; set; }
    public string? Type { get; set; }
    public string? Json { get; set; }
}

public class SyncSaleObject
{
    public string? Type { get; set; }
    public object? Data { get; set; }
}
