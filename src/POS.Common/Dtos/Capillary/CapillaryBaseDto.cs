namespace POS.Common.Dtos.Capillary;

public class CardLevelDto
{
    public string? AppCode { get; set; }
    public string? Level { get; set; }
    public string? ToLevel { get; set; }
    public bool Blocked { get; set; }
    public string? Description { get; set; }
}

public class LoyaltyInfoCapillary
{
    public string? LoyaltyType { get; set; }
}

public class PaginationCapillary
{
    public int limit { get; set; }
    public int offset { get; set; }
    public int total { get; set; }
}

public class StatusCodeCapillary
{
    public string? Success { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
    public string? Total { get; set; }
    public string? Success_count { get; set; }
}

public class ItemStatusCapillary
{
    public string? Success { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
}

public class ErrorCapillary
{
    public bool Status { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
}

public class IdentifiersCapillary
{
    public string? Type { get; set; }
    public string? Value { get; set; }
}

public class RegisteredBy
{
    public string? Code { get; set; }
    public string? Name { get; set; }
}

public class FieldCapillary
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}

public class Custom_fields
{
    public List<FieldCapillary>? Field { get; set; }
}

public class Extended_fields
{
    public List<FieldCapillary>? Field { get; set; }
}

public class Iden_mode_field
{
    public string? Iden_mode { get; set; }
}
