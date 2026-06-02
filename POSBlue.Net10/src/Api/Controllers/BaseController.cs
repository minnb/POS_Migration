using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// Base controller cho toàn bộ API. Thay thế ApiController (System.Web.Http) cũ.
/// Cung cấp helper trả response chuẩn ResultResponse và lấy IP — giữ NGUYÊN hành vi BaseController cũ
/// nhưng dùng HttpContext của ASP.NET Core (không còn HttpContext.Current).
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Trả response theo đúng cấu trúc cũ: HTTP status = status, body = ResultResponse { Status, Message, Data }.
    /// Tương đương Request.CreateResponse(status, new ResultResponse{...}) của bản .NET Framework.
    /// </summary>
    protected IActionResult ApiResult(HttpStatusCode status, string? message, object? data, string? messageTechnical = null)
    {
        var body = new ResultResponse
        {
            Status = status,
            Message = message,
            Data = data,
            MessageTechnical = messageTechnical,
        };
        return StatusCode((int)status, body);
    }

    /// <summary>IP của server (giữ logic cũ: địa chỉ IPv4 cuối cùng của host).</summary>
    protected static string? GetIpServer()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        return addr.LastOrDefault()?.ToString();
    }

    /// <summary>IP server @ client (đọc header X-FORWARDED-FOR từ HttpContext hiện tại).</summary>
    protected string GetIpAddressClient()
    {
        try
        {
            var ipServer = GetIpServer();
            var ipAddressClient = HttpContext.Request.Headers["X-FORWARDED-FOR"].ToString();
            if (!string.IsNullOrEmpty(ipAddressClient))
            {
                ipServer += "@" + ipAddressClient;
            }
            return ipServer ?? "localhost";
        }
        catch
        {
            return "localhost";
        }
    }
}
