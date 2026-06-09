using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.WinCustomer;

public class SavingAccumulationValue
{
    public string? OrderNo { get; set; }
    public string? Value { get; set; }
}

public class SavingAccumulationDto
{
    public string? TenderType { get; set; }
    public string? OrderNo { get; set; }
    public string? StoreNo { get; set; }
    public string? MemberCard { get; set; }
    public decimal TenderAmount { get; set; }
    public string? CusType { get; set; }
    public decimal Rounding1 { get; set; }
    public decimal Rounding2 { get; set; }
    public string? OperationId { get; set; }
    public string? Response { get; set; }
}

public class WinCustomerResponse
{
    public int Status { get; set; }
    public string? Description { get; set; }
    public string? Message { get; set; }
    public List<object>? TechMsg { get; set; }
    public WinCustomerDataResponse? Data { get; set; }
}

public class WinCustomerDataResponse
{
    public string? TraceId { get; set; }
}

public class WinPayAccumulationRequest
{
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public string RefCode { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string? OperationId { get; set; }
}

public class WinPayAccumulationDto
{
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public DateTime OrderDate { get; set; }
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public string RefCode { get; set; } = string.Empty;
    [Required]
    public string TenderType { get; set; } = string.Empty;
    public int TotalAmount { get; set; }
    public int PaymentAmount { get; set; }
    public string? OperationId { get; set; }
    public bool IsCheckItem { get; set; }
}

public class WinPayAccumulationData : WinPayAccumulationDto
{
    public int Amount { get; set; }
    public string? Condition { get; set; }
    public bool IsSync { get; set; }
    public string? Day { get; set; }
    public string? Month { get; set; }
    public string? TraceId { get; set; }
    public string? Response { get; set; }
    public string? Version { get; set; }
    public DateTime CrtDate { get; set; }
}

public class ConditionAccumulation
{
    public string? Condition { get; set; }
    public int Priority { get; set; }
    public bool IsCheckAmount { get; set; }
    public bool IsCheckItem { get; set; }
    public bool IsDay { get; set; }
    public bool IsMonth { get; set; }
    public int Amount { get; set; }
    public int Qty { get; set; }
    public string? AccumulationType { get; set; }
    public decimal AccumulationValue { get; set; }
    public string? Description { get; set; }
    public string? DayOfWeek { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public bool Blocked { get; set; }
    public DateTime CrtDate { get; set; }
    public DateTime ChgDate { get; set; }
    public List<ConditionAccumulationStore>? ConditionStore { get; set; }
}

public class ConditionAccumulationStore
{
    public string? Condition { get; set; }
    public string? StoreNo { get; set; }
    public bool Blocked { get; set; }
    public DateTime CrtDate { get; set; }
}

public class DayOfWeekData
{
    public string? Name { get; set; }
    public decimal Value { get; set; }
}

public class CountDataAccumulation
{
    public string? PhoneNumber { get; set; }
    public string? Condition { get; set; }
    public int Qty { get; set; }
}
