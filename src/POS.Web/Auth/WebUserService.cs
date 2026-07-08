using Dapper;
using Newtonsoft.Json;
using POS.Infrastructure.Database;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Redis;

namespace POS.Web.Auth;

public sealed class WebUserService(
    CentralMDConnectionFactory dbFactory,
    IFileLogHelper fileLogHelper,
    IRedisService redis
) : IWebUserService
{
    private const int MaxPinAttempts = 5;
    private const int PinLockoutSeconds = 900; // 15 phút


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

    public async Task<PinCheckResult> VerifyPinAsync(string username, string pin, CancellationToken ct = default)
    {
        var attemptsKey = $"SqlConsolePin:Attempts:{username}";
        var attempts = await redis.StringGetAsync<int>(attemptsKey);
        if (attempts >= MaxPinAttempts)
        {
            fileLogHelper.WriteLogs($"[WebUser.PinVerify] user={username} result=LOCKED");
            return new PinCheckResult(false, "Đã khóa do nhập sai quá nhiều lần. Thử lại sau ít phút.", true);
        }

        DashboardUser? user;
        try
        {
            using var conn = await dbFactory.CreateOpenConnectionAsync(ct);
            user = await conn.QueryFirstOrDefaultAsync<DashboardUser>(
                "SELECT * FROM DashboardUsers WHERE Username = @Username AND IsActive = 1",
                new { Username = username });
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("WebUserService.VerifyPinAsync", ex);
            return new PinCheckResult(false, "Không thể kiểm tra PIN — lỗi hệ thống.", false);
        }

        if (string.IsNullOrEmpty(user?.PinHash))
        {
            fileLogHelper.WriteLogs($"[WebUser.PinVerify] user={username} result=NOT_CONFIGURED");
            return new PinCheckResult(false, "PIN chưa được thiết lập cho tài khoản này — liên hệ quản trị hệ thống.", false);
        }

        bool pinMatches;
        try
        {
            pinMatches = BCrypt.Net.BCrypt.Verify(pin, user.PinHash);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("WebUserService.VerifyPinAsync.BCryptVerify", ex);
            return new PinCheckResult(false, "PIN của tài khoản này bị lỗi định dạng — liên hệ quản trị hệ thống để đặt lại.", false);
        }

        if (pinMatches)
        {
            await redis.StringSetAsync(attemptsKey, 0, ttlSeconds: PinLockoutSeconds);
            fileLogHelper.WriteLogs($"[WebUser.PinVerify] user={username} result=OK");
            return new PinCheckResult(true, null, false);
        }

        attempts++;
        await redis.StringSetAsync(attemptsKey, attempts, ttlSeconds: PinLockoutSeconds);
        fileLogHelper.WriteLogs($"[WebUser.PinVerify] user={username} result=FAIL attempts={attempts}");

        var locked = attempts >= MaxPinAttempts;
        var msg = locked
            ? "Sai mã PIN. Đã khóa do nhập sai quá nhiều lần — thử lại sau ít phút."
            : $"Sai mã PIN. Còn {MaxPinAttempts - attempts} lần thử.";
        return new PinCheckResult(false, msg, locked);
    }

    public async Task<bool> SetPinAsync(string username, string newPin, CancellationToken ct = default)
    {
        try
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(newPin);
            using var conn = await dbFactory.CreateOpenConnectionAsync(ct);
            var rows = await conn.ExecuteAsync(
                "UPDATE DashboardUsers SET PinHash = @PinHash WHERE Username = @Username",
                new { PinHash = hash, Username = username });
            if (rows > 0)
                fileLogHelper.WriteLogs($"[WebUser.PinChange] user={username} result=OK");
            return rows > 0;
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("WebUserService.SetPinAsync", ex);
            return false;
        }
    }
}
