namespace POS.Common.Dtos.Redis;

public class RedisServerStatusDto
{
    public bool IsOnline { get; set; }
    public long PingMs { get; set; }
    public string Role { get; set; } = "unknown";
    public string Target { get; set; } = string.Empty;
    public int DatabaseIndex { get; set; }
    public string UsedMemoryHuman { get; set; } = "—";
    public int ConnectedClients { get; set; }
    public long TotalKeys { get; set; }
    public double? HitRatePercent { get; set; }
    public long UptimeSeconds { get; set; }
    public string? ErrorMessage { get; set; }
}
