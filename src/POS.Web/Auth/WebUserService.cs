using Dapper;
using Newtonsoft.Json;
using POS.Infrastructure.Database;
using POS.Infrastructure.Logging;

namespace POS.Web.Auth;

public sealed class WebUserService(
    CentralMDConnectionFactory dbFactory,
    IFileLogHelper fileLogHelper
) : IWebUserService
{
    public async Task<DashboardUser?> ValidateLoginAsync(string username, string password,
        CancellationToken ct = default)
    {
        try
        {
            using var conn = await dbFactory.CreateOpenConnectionAsync(ct);
            var user = await conn.QueryFirstOrDefaultAsync<DashboardUser>(
                "SELECT * FROM DashboardUsers WHERE Username = @Username AND IsActive = 1",
                new { Username = username });

            if (user is null) return null;
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
            return user;
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("WebUserService.ValidateLoginAsync", ex);
            return null;
        }
    }

    public async Task<DashboardUser?> GetByUsernameAsync(string username,
        CancellationToken ct = default)
    {
        try
        {
            using var conn = await dbFactory.CreateOpenConnectionAsync(ct);
            return await conn.QueryFirstOrDefaultAsync<DashboardUser>(
                "SELECT * FROM DashboardUsers WHERE Username = @Username AND IsActive = 1",
                new { Username = username });
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("WebUserService.GetByUsernameAsync", ex);
            return null;
        }
    }

    public IReadOnlyList<string> GetStoreCodes(DashboardUser user)
    {
        if (string.IsNullOrWhiteSpace(user.StoreCodes)) return [];
        return JsonConvert.DeserializeObject<List<string>>(user.StoreCodes) ?? [];
    }
}
