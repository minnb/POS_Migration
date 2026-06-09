using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace POS.Api.Middleware;

/// <summary>
/// Thay thế BasicAuthenticationAttribute của Web API 2.
/// Chỉ enforce trên /api/v2/* — khớp behavior cũ (WebApiConfig.cs).
/// Sau khi migrate Application layer: thay bằng validation từ IMemoryCache["SysWebApiUser"].
/// </summary>
public sealed class BasicAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Chỉ enforce auth trên /api/v2/* — route khác bỏ qua (NoResult = không chặn)
        if (!Request.Path.StartsWithSegments("/api/v2", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!Request.Headers.TryGetValue("Authorization", out var headerValues))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));

        if (!AuthenticationHeaderValue.TryParse(headerValues.ToString(), out var authHeader)
            || !"Basic".Equals(authHeader.Scheme, StringComparison.OrdinalIgnoreCase)
            || authHeader.Parameter is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header"));
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Malformed Basic credentials"));
        }

        var colonIdx = decoded.IndexOf(':');
        if (colonIdx < 0)
            return Task.FromResult(AuthenticateResult.Fail("Malformed Basic credentials"));

        var username = decoded[..colonIdx];
        var password = decoded[(colonIdx + 1)..];

        if (!IsValidUser(username, password))
            return Task.FromResult(AuthenticateResult.Fail("Invalid credentials"));

        var claims = new[] { new Claim(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers.WWWAuthenticate = "Basic realm=\"POS API\"";
        return Task.CompletedTask;
    }

    private bool IsValidUser(string username, string password)
    {
        var cfgUser = configuration["BasicAuth:Username"] ?? string.Empty;
        var cfgPass = configuration["BasicAuth:Password"] ?? string.Empty;
        return !string.IsNullOrEmpty(cfgUser)
            && string.Equals(username, cfgUser, StringComparison.Ordinal)
            && string.Equals(password, cfgPass, StringComparison.Ordinal);
    }
}
