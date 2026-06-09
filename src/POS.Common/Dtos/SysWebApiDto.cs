namespace POS.Common.Dtos;

public static class AppGlobals
{
    public static string? Environment { get; set; }
    public static string? WebApiSystem { get; set; }
    public static string? ConnStrLoyalty { get; set; }
    public static string? ConnStrCentralMD { get; set; }
    public static string? RedisActive { get; set; }
}

public class SysWebApiDto : SysWebApi
{
    public List<SysWebApiRoute>? SysWebApiRoute { get; set; }
}

public class SysWebApi
{
    public string? AppCode { get; set; }
    public string? Host { get; set; }
    public string? Version { get; set; }
    public string? Authorization { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? PublicKey { get; set; }
    public string? PrivateKey { get; set; }
    public bool Blocked { get; set; }
    public string? HttpProxy { get; set; }
    public string? Bypasslist { get; set; }
    public string? Description { get; set; }
}

public class SysWebApiRoute
{
    public string? AppCode { get; set; }
    public string? Name { get; set; }
    public string? Route { get; set; }
    public string? Description { get; set; }
    public bool Blocked { get; set; }
    public string? Version { get; set; }
    public string? Notes { get; set; }
}
