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

    public async Task<IReadOnlyList<DashboardUser>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            using var conn = await dbFactory.CreateOpenConnectionAsync(ct);
            var result = await conn.QueryAsync<DashboardUser>(
                "SELECT * FROM DashboardUsers ORDER BY Id");
            return result.ToList();
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("WebUserService.GetAllAsync", ex);
            return [];
        }
    }

    public async Task<bool> CreateAsync(DashboardUser user, string password, CancellationToken ct = default)
    {
        try
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            using var conn = await dbFactory.CreateOpenConnectionAsync(ct);
            var rows = await conn.ExecuteAsync(
                """
                INSERT INTO DashboardUsers (Username, PasswordHash, FullName, Role, StoreCodes, IsActive, CreatedAt, UpdatedAt)
                VALUES (@Username, @PasswordHash, @FullName, @Role, @StoreCodes, @IsActive, GETDATE(), GETDATE())
                """,
                new
                {
                    user.Username,
                    PasswordHash = hash,
                    user.FullName,
                    user.Role,
                    user.StoreCodes,
                    user.IsActive
                });
            return rows > 0;
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("WebUserService.CreateAsync", ex);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(DashboardUser user, string? newPassword, CancellationToken ct = default)
    {
        try
        {
            using var conn = await dbFactory.CreateOpenConnectionAsync(ct);
            int rows;
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                rows = await conn.ExecuteAsync(
                    """
                    UPDATE DashboardUsers
                    SET FullName=@FullName, Role=@Role, StoreCodes=@StoreCodes,
                        IsActive=@IsActive, PasswordHash=@PasswordHash, UpdatedAt=GETDATE()
                    WHERE Id=@Id
                    """,
                    new { user.Id, user.FullName, user.Role, user.StoreCodes, user.IsActive, PasswordHash = hash });
            }
            else
            {
                rows = await conn.ExecuteAsync(
                    """
                    UPDATE DashboardUsers
                    SET FullName=@FullName, Role=@Role, StoreCodes=@StoreCodes,
                        IsActive=@IsActive, UpdatedAt=GETDATE()
                    WHERE Id=@Id
                    """,
                    new { user.Id, user.FullName, user.Role, user.StoreCodes, user.IsActive });
            }
            return rows > 0;
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("WebUserService.UpdateAsync", ex);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            using var conn = await dbFactory.CreateOpenConnectionAsync(ct);
            var rows = await conn.ExecuteAsync(
                "UPDATE DashboardUsers SET IsActive=0, UpdatedAt=GETDATE() WHERE Id=@Id",
                new { Id = id });
            return rows > 0;
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("WebUserService.DeleteAsync", ex);
            return false;
        }
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct = default)
    {
        try
        {
            using var conn = await dbFactory.CreateOpenConnectionAsync(ct);
            var rows = await conn.ExecuteAsync(
                "UPDATE DashboardUsers SET IsActive=1, UpdatedAt=GETDATE() WHERE Id=@Id",
                new { Id = id });
            return rows > 0;
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("WebUserService.ActivateAsync", ex);
            return false;
        }
    }

    public async Task<bool> UsernameExistsAsync(string username, int excludeId = 0, CancellationToken ct = default)
    {
        try
        {
            using var conn = await dbFactory.CreateOpenConnectionAsync(ct);
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM DashboardUsers WHERE Username=@Username AND Id<>@ExcludeId",
                new { Username = username, ExcludeId = excludeId });
            return count > 0;
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("WebUserService.UsernameExistsAsync", ex);
            return false;
        }
    }
}
