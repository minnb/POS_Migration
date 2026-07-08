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

    /// <summary>
    /// Overload structured — truyền thẳng Exception (giữ stack trace/inner exception, thay vì chỉ
    /// ex.Message) + context object tùy chọn (destructure qua {@Context}, tự động mask field nhạy
    /// cảm — xem SensitiveDataMasker). Dùng khi cần log chi tiết tham số đầu vào để phân tích lỗi.
    /// </summary>
    void LogException(string endpoint, string posNo, int errorCode, string note, Exception ex,
        IReadOnlyDictionary<string, object?>? context = null);

    void LogInfo(string endpoint, string posNo, string message);
}
