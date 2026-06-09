using Newtonsoft.Json;

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

    // Logs/Exception/log-yyyyMMdd.txt
    public void WriteExpLogs(string function, Exception ex)
    {
        try
        {
            var dir = Path.Combine(baseDirectory, "Exception");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"log-{DateTime.Now:yyyyMMdd}.txt");
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff} : {function}===>{JsonConvert.SerializeObject(ex)}{Environment.NewLine}";
            File.AppendAllText(file, line);
        }
        catch { }
    }
}
