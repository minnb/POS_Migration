namespace POS.Infrastructure.Logging;

/// <summary>
/// Cấu hình số ngày/dung lượng giữ log, dùng chung cho Serilog file sink (pos-*.log) và
/// FileLogHelper (debug/, Exception/). Bind từ section "LogRetention" của appsettings.
/// Bỏ trống section này giữ nguyên hành vi cũ (14 ngày Serilog, không giới hạn size,
/// FileLogHelper không tự dọn — tương thích ngược mặc định).
/// </summary>
public sealed class LogRetentionOptions
{
    public const string SectionName = "LogRetention";

    /// <summary>Serilog WriteTo.File retainedFileCountLimit cho pos-*.log (RollingInterval.Day).</summary>
    public int SerilogRetainedFileCountLimit { get; set; } = 14;

    /// <summary>Serilog WriteTo.File fileSizeLimitBytes. null = không giới hạn (hành vi cũ).</summary>
    public long? SerilogFileSizeLimitBytes { get; set; }

    /// <summary>Số ngày giữ file .txt của FileLogHelper (debug/, Exception/). &lt;= 0 = tắt cleanup.</summary>
    public int RawLogRetentionDays { get; set; } = 30;
}
