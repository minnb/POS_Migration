namespace POS.Application.Loyalty.DTOs.Capillary;

// ── Status / Error ──────────────────────────────────────────────────────────

public class CapStatusCode
{
    public string? Success { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
    public string? Total { get; set; }
    public string? Success_count { get; set; }
}

public class CapItemStatus
{
    public string? Success { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
}

public class CapErrorResponse
{
    public int CreatedId { get; set; }
    public object? Data { get; set; }
    public IList<object>? Errors { get; set; }
    public IList<object>? Warnings { get; set; }
}

public class CapServerError
{
    public int StatusCode { get; set; }
    public string? DataRequest { get; set; }
    public string? DataResponse { get; set; }
    public string? RequestId { get; set; }
}

// ── Identifiers / Fields ────────────────────────────────────────────────────

public class CapIdentifier
{
    public string? Type { get; set; }
    public string? Value { get; set; }
}

public class CapRegisteredStore
{
    public string? Code { get; set; }
    public string? Name { get; set; }
}

public class CapFieldItem
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}

public class CapCustomFields
{
    public List<CapFieldItem> Field { get; set; } = new();
}

public class CapExtendedFields
{
    public List<CapFieldItem> Field { get; set; } = new();
}

public class CapIdenModeField
{
    public string? Iden_mode { get; set; }
}
