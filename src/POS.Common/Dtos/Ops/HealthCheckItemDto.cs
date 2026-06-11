namespace POS.Common.Dtos.Ops;

/// <summary>
/// Kết quả check kết nối 1 thành phần hạ tầng (Redis/RabbitMQ/SQL)
/// — trả về từ api/common/CheckConnection.
/// </summary>
public class HealthCheckItemDto
{
    /// <summary>Tên thành phần, vd "Redis", "RabbitMQ", "SQL:CentralMD".</summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>Đích kết nối (host/database) — KHÔNG chứa credentials.</summary>
    public string Target { get; set; } = string.Empty;

    public bool Ok { get; set; }

    public long LatencyMs { get; set; }

    public string Message { get; set; } = string.Empty;
}
