namespace POS.Infrastructure.Cache;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string Mode { get; set; } = "Sentinel"; // "Sentinel" or "Standalone"
    public string[] SentinelHosts { get; set; } = [];
    public string MasterName { get; set; } = "mymaster";
    public string Password { get; set; } = string.Empty;
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
    public int DefaultDatabase { get; set; } = 0;
}
