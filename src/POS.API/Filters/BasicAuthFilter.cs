using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using POS.Shared.Models;

namespace POS.API.Filters;

/// <summary>
/// Giữ nguyên cơ chế BasicAuth như API cũ — POS client không đổi config.
/// Header: Authorization: Basic base64(username:password)
/// </summary>
public class BasicAuthFilter : IAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    public BasicAuthFilter(IConfiguration configuration)
    {
        _username = configuration["BasicAuth:Username"] ?? string.Empty;
        _password = configuration["BasicAuth:Password"] ?? string.Empty;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();

        if (authHeader == null || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            Deny(context);
            return;
        }

        try
        {
            var base64Credentials = authHeader["Basic ".Length..].Trim();
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
            var separatorIndex = credentials.IndexOf(':');
            if (separatorIndex < 0) { Deny(context); return; }

            var username = credentials[..separatorIndex];
            var password = credentials[(separatorIndex + 1)..];

            if (username != _username || password != _password)
                Deny(context);
        }
        catch
        {
            Deny(context);
        }
    }

    private static void Deny(AuthorizationFilterContext context)
        => context.Result = new ObjectResult(new ResultResponse
        {
            Status = (int)HttpStatusCode.Unauthorized,
            Message = "Authorization has been denied for this request"
        })
        { StatusCode = (int)HttpStatusCode.Unauthorized };
}
