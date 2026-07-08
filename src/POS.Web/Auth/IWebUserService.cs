namespace POS.Web.Auth;

public sealed record PinCheckResult(bool Ok, string? Error, bool Locked);

public interface IWebUserService
{
    Task<DashboardUser?> ValidateLoginAsync(string username, string password,
        CancellationToken ct = default);
    Task<DashboardUser?> GetByUsernameAsync(string username,
        CancellationToken ct = default);
    IReadOnlyList<string> GetStoreCodes(DashboardUser user);

    Task<IReadOnlyList<DashboardUser>> GetAllAsync(CancellationToken ct = default);
    Task<bool> CreateAsync(DashboardUser user, string password, CancellationToken ct = default);
    Task<bool> UpdateAsync(DashboardUser user, string? newPassword, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> ActivateAsync(int id, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, int excludeId = 0, CancellationToken ct = default);

    // PIN gate cho SQL Console — PIN riêng theo từng tài khoản (DashboardUsers.PinHash).
    Task<PinCheckResult> VerifyPinAsync(string username, string pin, CancellationToken ct = default);
    Task<bool> SetPinAsync(string username, string newPin, CancellationToken ct = default);
}
