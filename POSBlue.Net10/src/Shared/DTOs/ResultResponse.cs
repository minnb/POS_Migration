using System.Net;

namespace VCM.POSBLUE.Shared.DTOs;

/// <summary>
/// Response model chuẩn trả về cho POS client.
/// Giữ NGUYÊN cấu trúc field (Status, Message, Data, MessageTechnical) như bản .NET Framework cũ
/// (TCX.API.Common.Models.ResultResponse) để không thay đổi contract với client.
/// </summary>
public class ResultResponse
{
    public HttpStatusCode Status { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
    public string? MessageTechnical { get; set; }
}
