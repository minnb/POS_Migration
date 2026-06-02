using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Keystone gọi HTTP API đối tác — thay ApiCallHelper (System.Net.HttpClient thủ công) cũ,
/// dùng IHttpClientFactory. Được mọi partner service (CX, WinLife, WinCustomer, Gift, Winpay...) dùng chung.
/// </summary>
public interface IApiCallClient
{
    Task<ApiCallResult> SendAsync(ApiCallRequest request);
}

/// <summary>Tham số gọi API (mirror constructor ApiCallHelper cũ).</summary>
public sealed class ApiCallRequest
{
    public string Host { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public Dictionary<string, string>? Headers { get; set; }
    public string? TokenType { get; set; }
    public string? AccessToken { get; set; }
    public string WebMethod { get; set; } = "POST";
    public string? JsonData { get; set; }
    public int Timeout { get; set; } = 30;
    public string? ProxyHttp { get; set; }
    public string? BypassList { get; set; }
}

/// <summary>Kết quả gọi API (thay cặp ref responseTime/responseErr cũ).</summary>
public sealed class ApiCallResult
{
    public string Response { get; set; } = "";
    public long ResponseTimeMs { get; set; }
    public ServerErrorResponses Error { get; set; } = new();
}
