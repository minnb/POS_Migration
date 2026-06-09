using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace POS.Infrastructure.Logging;

public sealed class KibanaService(ILogger<KibanaService> logger) : IKibanaService
{
    // Structured field names — giữ nguyên để Kibana dashboard không bị vỡ.
    // Trong Elastic.Serilog.Sinks (ECS format), các field này nằm trong labels.{field}.
    // Nếu dashboard cũ dùng field ở top-level, cần update Kibana query từ
    // "WebApi" → "labels.WebApi", "PosNo" → "labels.PosNo", v.v.

    private static string HostName => Environment.MachineName;

    public void LogRequest(string endpoint, string posNo, string requestBody)
    {
        var (traceId, spanId) = GetTraceIds();
        _ = Task.Run(() =>
        {
            try
            {
                using (LogContext.PushProperty("HttpContext", "Request"))
                using (LogContext.PushProperty("WebApi", endpoint))
                using (LogContext.PushProperty("PosNo", posNo))
                using (LogContext.PushProperty("trace_id", traceId))
                using (LogContext.PushProperty("span_id", spanId))
                {
                    logger.LogWarning("Request {Endpoint} payload {RequestBody}", endpoint, requestBody);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in KibanaService.LogRequest fire-and-forget");
            }
        });
    }

    public void LogResponse(string endpoint, string posNo, long responseTimeMs, string note, string responseBody)
    {
        var (traceId, spanId) = GetTraceIds();
        _ = Task.Run(() =>
        {
            try
            {
                using (LogContext.PushProperty("HttpContext", "Response"))
                using (LogContext.PushProperty("WebApi", endpoint))
                using (LogContext.PushProperty("PosNo", posNo ?? HostName))
                using (LogContext.PushProperty("ResponseTime", responseTimeMs))
                using (LogContext.PushProperty("DeveloperMessage", note))
                using (LogContext.PushProperty("trace_id", traceId))
                using (LogContext.PushProperty("span_id", spanId))
                {
                    logger.LogWarning("{Endpoint} response {ResponseTimeMs}ms: {ResponseBody}",
                        endpoint, responseTimeMs, responseBody);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in KibanaService.LogResponse fire-and-forget");
            }
        });
    }

    public void LogException(string endpoint, string posNo, int errorCode, string note, string errorDetail)
    {
        var (traceId, spanId) = GetTraceIds();
        _ = Task.Run(() =>
        {
            try
            {
                using (LogContext.PushProperty("HttpContext", "Exception"))
                using (LogContext.PushProperty("WebApi", endpoint))
                using (LogContext.PushProperty("PosNo", posNo ?? HostName))
                using (LogContext.PushProperty("ErrorCode", errorCode))
                using (LogContext.PushProperty("DeveloperMessage", note))
                using (LogContext.PushProperty("trace_id", traceId))
                using (LogContext.PushProperty("span_id", spanId))
                {
                    logger.LogError("{Endpoint} exception [{ErrorCode}] {Note}: {ErrorDetail}",
                        endpoint, errorCode, note, errorDetail);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in KibanaService.LogException fire-and-forget");
            }
        });
    }

    public void LogInfo(string endpoint, string posNo, string message)
    {
        var (traceId, spanId) = GetTraceIds();
        _ = Task.Run(() =>
        {
            try
            {
                using (LogContext.PushProperty("HttpContext", "Info"))
                using (LogContext.PushProperty("WebApi", endpoint))
                using (LogContext.PushProperty("PosNo", posNo ?? HostName))
                using (LogContext.PushProperty("trace_id", traceId))
                using (LogContext.PushProperty("span_id", spanId))
                {
                    logger.LogInformation("{Endpoint} {Message}", endpoint, message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in KibanaService.LogInfo fire-and-forget");
            }
        });
    }

    private static (string TraceId, string SpanId) GetTraceIds()
    {
        var activity = Activity.Current;
        return (activity?.TraceId.ToString() ?? string.Empty,
                activity?.SpanId.ToString() ?? string.Empty);
    }
}
