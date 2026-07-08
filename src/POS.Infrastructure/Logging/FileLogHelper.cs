namespace POS.Infrastructure.Logging;

/// <summary>
/// Ghi log ra file text — fallback khi Elasticsearch không available.
/// Giữ nguyên format và đường dẫn như FileHelper cũ để ops tool không bị vỡ.
/// </summary>
public sealed class FileLogHelper(string baseDirectory, int retentionDays = 30) : IFileLogHelper
{
    private readonly object _debugLock = new();
    private readonly object _expLock = new();

    // Interlocked.Exchange trên ticks — không cần single-flight tuyệt đối, 2 sweep trùng nhau
    // vô hại (xóa file đã xóa chỉ bị catch{} nuốt trong CleanupOldFiles).
    private long _lastCleanupTicks = DateTime.MinValue.Ticks;

    // Logs/debug/log-yyyyMMdd.txt
    public void WriteLogs(string message)
    {
        RunRetentionSweepIfDue();
        try
        {
            var dir = Path.Combine(baseDirectory, "debug");
            var file = Path.Combine(dir, $"log-{DateTime.Now:yyyyMMdd}.txt");
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff} : {message}{Environment.NewLine}";
            lock (_debugLock)
            {
                Directory.CreateDirectory(dir);
                File.AppendAllText(file, line);
            }
        }
        catch { /* silent — logging must never throw */ }
    }

    // Logs/Exception/log-yyyyMMdd.txt — ghi đầy đủ type + message + stack trace + inner exception.
    public void WriteExpLogs(string function, Exception ex)
    {
        RunRetentionSweepIfDue();
        try
        {
            var dir = Path.Combine(baseDirectory, "Exception");
            var file = Path.Combine(dir, $"log-{DateTime.Now:yyyyMMdd}.txt");
            // Dùng ex.ToString() thay vì JsonConvert.SerializeObject(ex): serialize Exception bằng
            // JSON dễ ném lỗi/thiếu dữ liệu (vd HttpRequestException + SocketException inner) →
            // bị catch{} nuốt → file log rỗng. ToString() luôn có message + stack + inner, không ném.
            var detail = ex?.ToString() ?? "(null exception)";
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff} : {function}===>{detail}{Environment.NewLine}";
            lock (_expLock)
            {
                Directory.CreateDirectory(dir);
                File.AppendAllText(file, line);
            }
        }
        catch { /* silent — logging must never throw */ }
    }

    // Sweep cơ hội: chạy tối đa 1 lần/24h, kích hoạt bởi lần ghi log kế tiếp (không cần worker
    // riêng — xem CLAUDE.md/plan: FileLogHelper chạy trong cả POS.Api/POS.Web/POS.Worker, mỗi
    // process tự dọn thư mục log của chính nó).
    private void RunRetentionSweepIfDue()
    {
        if (retentionDays <= 0) return;

        var nowTicks = DateTime.UtcNow.Ticks;
        var lastTicks = Interlocked.Read(ref _lastCleanupTicks);
        if (TimeSpan.FromTicks(nowTicks - lastTicks) < TimeSpan.FromHours(24)) return;
        Interlocked.Exchange(ref _lastCleanupTicks, nowTicks);

        CleanupOldFiles("debug");
        CleanupOldFiles("Exception");
    }

    private void CleanupOldFiles(string subfolder)
    {
        try
        {
            var dir = Path.Combine(baseDirectory, subfolder);
            if (!Directory.Exists(dir)) return;

            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            foreach (var file in Directory.EnumerateFiles(dir, "log-*.txt"))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff) File.Delete(file);
                }
                catch { /* 1 file lỗi không phá sweep */ }
            }
        }
        catch { /* silent — logging must never throw */ }
    }
}
