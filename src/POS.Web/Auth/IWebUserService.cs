namespace POS.Web.Auth;

public interface IWebUserService
{
    Task<DashboardUser?> ValidateLoginAsync(string username, string password,
        CancellationToken ct = default);
    Task<DashboardUser?> GetByUsernameAsync(string username,
        CancellationToken ct = default);
    IReadOnlyList<string> GetStoreCodes(DashboardUser user);
}
