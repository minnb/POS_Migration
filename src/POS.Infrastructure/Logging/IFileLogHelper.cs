namespace POS.Infrastructure.Logging;

/// <summary>
/// Fallback file-based logger — dùng khi Serilog/Elasticsearch không available.
/// Giữ nguyên method signature như FileHelper cũ để không cần đổi caller.
/// </summary>
public interface IFileLogHelper
{
    void WriteLogs(string message);
    void WriteExpLogs(string function, Exception ex);
}
