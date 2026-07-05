namespace POS.Infrastructure.Logging;

/// <summary>
/// Ghi log ra file text — fallback khi Elasticsearch không available.
/// Giữ nguyên format và đường dẫn như FileHelper cũ để ops tool không bị vỡ.
/// </summary>
public sealed class FileLogHelper(string baseDirectory) : IFileLogHelper
{
    // Logs/debug/log-yyyyMMdd.txt
    public void WriteLogs(string message)
    {
        try
        {
            var dir = Path.Combine(baseDirectory, "debug");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"log-{DateTime.Now:yyyyMMdd}.txt");
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff} : {message}{Environment.NewLine}";
            File.AppendAllText(file, line);
        }
        catch { /* silent — logging must never throw */ }
    }

    // Logs/Exception/log-yyyyMMdd.txt — ghi đầy đủ type + message + stack trace + inner exception.
    public void WriteExpLogs(string function, Exception ex)
    {
        try
        {
            var dir = Path.Combine(baseDirectory, "Exception");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"log-{DateTime.Now:yyyyMMdd}.txt");
            // Dùng ex.ToString() thay vì JsonConvert.SerializeObject(ex): serialize Exception bằng
            // JSON dễ ném lỗi/thiếu dữ liệu (vd HttpRequestException + SocketException inner) →
            // bị catch{} nuốt → file log rỗng. ToString() luôn có message + stack + inner, không ném.
            var detail = ex?.ToString() ?? "(null exception)";
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff} : {function}===>{detail}{Environment.NewLine}";
            File.AppendAllText(file, line);
        }
        catch { /* silent — logging must never throw */ }
    }
}
