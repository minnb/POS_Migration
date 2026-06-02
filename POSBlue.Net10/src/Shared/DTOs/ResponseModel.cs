using System.Net;

namespace VCM.POSBLUE.Shared.DTOs;

/// <summary>
/// Generic response model (giữ nguyên từ TCX.API.Common.Response.ResponseModel&lt;T, EStatus&gt;).
/// </summary>
public class ResponseModel<T, EStatus>
{
    public EStatus Status { get; set; } = default!;
    public string? Message { get; set; }
    public T Data { get; set; } = default!;
}

/// <summary>
/// Bản rút gọn dùng cho helper trả response (tương đương ResponseModel&lt;object, Enum&gt; trong code cũ).
/// </summary>
public class ResponseModel
{
    public HttpStatusCode Status { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}
