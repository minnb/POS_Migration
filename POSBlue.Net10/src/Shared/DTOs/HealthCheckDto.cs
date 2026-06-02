namespace VCM.POSBLUE.Shared.DTOs;

/// <summary>
/// Kết quả health-check (giữ nguyên từ HealthCheckModel cũ).
/// </summary>
public class HealthCheckDto
{
    public string? IPServer { get; set; }
    public bool DbConnection { get; set; }
    public string? Version { get; set; }
}
