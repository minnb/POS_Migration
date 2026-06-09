namespace POS.Infrastructure.Logging;

/// <summary>
/// Structured logging service → Elasticsearch qua Serilog.
/// Giữ nguyên method signature như cũ để không cần đổi caller trong Controller/Service.
/// </summary>
public interface IKibanaService
{
    void LogRequest(string endpoint, string posNo, string requestBody);
    void LogResponse(string endpoint, string posNo, long responseTimeMs, string note, string responseBody);
    void LogException(string endpoint, string posNo, int errorCode, string note, string errorDetail);
    void LogInfo(string endpoint, string posNo, string message);
}
