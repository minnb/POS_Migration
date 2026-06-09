namespace VCM.POSBLUE.Common.Helpers;

public interface IFileLogHelper
{
    void WriteLogs(string context, string message);
    void WriteExpLogs(string context, Exception ex);
}

public sealed class FileLogHelper : IFileLogHelper
{
    private readonly string _logDirectory;
    private static readonly object _lock = new();

    public FileLogHelper(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(logDirectory);
    }

    public void WriteLogs(string context, string message)
    {
        try
        {
            var filePath = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}] {message}";
            lock (_lock)
            {
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // ghi log thất bại không được ném exception ra ngoài
        }
    }

    public void WriteExpLogs(string context, Exception ex)
    {
        try
        {
            var filePath = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] [{context}] {ex.Message}{Environment.NewLine}{ex.StackTrace}";
            lock (_lock)
            {
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // ghi log thất bại không được ném exception ra ngoài
        }
    }
}
