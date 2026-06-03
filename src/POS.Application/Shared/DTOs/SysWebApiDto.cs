namespace POS.Application.Shared.DTOs;

public class SysWebApiDto
{
    public string AppCode { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Authorization { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HttpProxy { get; set; }
    public string? Bypasslist { get; set; }
    public List<SysWebApiRouteDto> Routes { get; set; } = new();

    public SysWebApiRouteDto? GetRoute(string name)
        => Routes.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}

public class SysWebApiRouteDto
{
    public string AppCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
