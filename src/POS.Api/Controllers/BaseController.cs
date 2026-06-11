using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using POS.Common;
using POS.Common.Dtos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;

namespace POS.Api.Controllers;

/// <summary>
/// Base controller — thay ApiController + BaseController cũ (Web API 2).
/// Tất cả controller nghiệp vụ kế thừa class này.
///
/// Auth policy: thêm [Authorize(AuthenticationSchemes = "BasicAuth")] trực tiếp vào
/// controller/action nào đặt dưới route api/v2/... — khớp behavior cũ của
/// BasicAuthenticationAttribute (chỉ enforce khi path chứa "api/v2").
/// CommonController (api/common) và các route public không cần [Authorize].
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    // ── Response helpers ─────────────────────────────────────────────────

    /// <summary>Thay Request.CreateResponse(HttpStatusCode.OK, ...) cũ.</summary>
    protected IActionResult OkResult(object? data, string message = "Success")
        => Ok(new ResultResponse
        {
            Message = message,
            Status = HttpStatusCode.OK,
            Data = data,
            MessageTechnical = string.Empty
        });

    /// <summary>Thay Request.CreateResponse(HttpStatusCode.BadRequest, ...) với Exception.</summary>
    protected IActionResult BadRequestResult(Exception ex)
        => BadRequest(new ResultResponse
        {
            Message = ex.Message,
            Status = HttpStatusCode.BadRequest,
            Data = null,
            MessageTechnical = JsonConvert.SerializeObject(ex)
        });

    /// <summary>Thay Request.CreateResponse(HttpStatusCode.BadRequest, ...) với message string.</summary>
    protected IActionResult BadRequestResult(string message, string technical = "")
        => BadRequest(new ResultResponse
        {
            Message = message,
            Status = HttpStatusCode.BadRequest,
            Data = null,
            MessageTechnical = technical
        });

    // ── ModelState helpers (migrate từ ExceptionModels / NewExceptionModels) ──

    /// <summary>
    /// Thay ExceptionModels() cũ — trả 400 BadRequest từ ModelState đầu tiên có lỗi.
    /// </summary>
    protected IActionResult ExceptionModels()
    {
        var error = ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault();
        var msg = error?.Exception?.Message ?? error?.ErrorMessage ?? "Model không hợp lệ";
        return BadRequest(new ResultResponse
        {
            Message = msg,
            Status = HttpStatusCode.BadRequest
        });
    }

    /// <summary>
    /// Thay NewExceptionModels() cũ — trả 409 Conflict từ ModelState đầu tiên có lỗi.
    /// </summary>
    protected IActionResult NewExceptionModels()
    {
        var error = ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault();
        var msg = error?.Exception?.Message ?? error?.ErrorMessage ?? "Model không hợp lệ";
        return Conflict(new ResultResponse
        {
            Message = msg,
            Status = HttpStatusCode.Conflict,
            MessageTechnical = JsonConvert.SerializeObject(error?.Exception)
        });
    }

    // ── IP / PosNo helpers ────────────────────────────────────────────────

    /// <summary>Thay GetPosNo() cũ — trả server IP, dùng làm posNo khi POS không gửi.</summary>
    protected string GetPosNo() => GetIpServer();

    /// <summary>Thay GetIpServer() cũ — lấy IPv4 của máy chủ qua DNS.</summary>
    protected string GetIpServer()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList
                .LastOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                ?.ToString() ?? "127.0.0.1";
        }
        catch { return "127.0.0.1"; }
    }

    /// <summary>
    /// Thay GetIpAddressClient() cũ.
    /// Trả về "serverIp@clientIp" hoặc "serverIp" nếu không có X-FORWARDED-FOR.
    /// </summary>
    protected string GetIpAddressClient()
    {
        try
        {
            var ipServer = GetIpServer();
            var forwarded = HttpContext.Request.Headers["X-FORWARDED-FOR"].ToString();
            return string.IsNullOrEmpty(forwarded)
                ? ipServer
                : $"{ipServer}@{forwarded}";
        }
        catch { return "localhost"; }
    }
    public AuthDto? GetAuthData()
    {
        try
        {
            var authHeader = Request.Headers.Authorization.ToString();
            var authHeaderValue = AuthenticationHeaderValue.Parse(authHeader);
            var credentials = Encoding.UTF8
                .GetString(Convert.FromBase64String(authHeaderValue.Parameter ?? "0:0:0"))
                .Split(':');

            return new AuthDto()
            {
                AppCode = credentials[0],
                StoreNo = credentials[1],
                PosNo = credentials[2]
            };
        }
        catch
        {
            return null;
        }
    }
}
