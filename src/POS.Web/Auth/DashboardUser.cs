namespace POS.Web.Auth;

public sealed class DashboardUser
{
    public int     Id           { get; set; }
    public string  Username     { get; set; } = string.Empty;
    public string  PasswordHash { get; set; } = string.Empty;
    public string  FullName     { get; set; } = string.Empty;
    public string  Role         { get; set; } = string.Empty;
    public string? StoreCodes   { get; set; }
    public bool    IsActive     { get; set; }
    public string? PinHash      { get; set; }
}
