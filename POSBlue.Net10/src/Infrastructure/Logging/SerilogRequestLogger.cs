using System.Diagnostics;
using Serilog;
using VCM.POSBLUE.Application.Interfaces;

namespace VCM.POSBLUE.Infrastructure.Logging;

/// <summary>
/// Triển khai IRequestLogger bằng Serilog → Elasticsearch (giữ NGUYÊN format log của KibanaService cũ:
/// các property {WebApi} {PosNo} {ResponseTime} {HttpContext} {trace_id} {span_id} để dashboard Kibana không vỡ).
/// Fire-and-forget như bản cũ để không chặn request.
/// </summary>
public sealed class SerilogRequestLogger : IRequestLogger
{
    public void LogRequest(string webApi, string posNo, string endPoint, string bodyJson)
    {
        var (traceId, spanId) = GetTrace();
        _ = Task.Run(() =>
        {
            try
            {
                Log.Logger.Warning(
                    string.Format("Request {0} payload {1}", webApi + endPoint, bodyJson)
                        + " {HttpContext} {WebApi} {PosNo} {trace_id} {span_id}",
                    "Request", webApi + endPoint, posNo, traceId, spanId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Fire-and-Forget log");
            }
        });
    }

    public void LogResponse(string webApi, string posNo, long timeResponse, string endPoint, string bodyJson)
    {
        var (traceId, spanId) = GetTrace();
        _ = Task.Run(() =>
        {
            try
            {
                Log.Logger.Warning(
                    string.Format("{0} response: {1}", endPoint, bodyJson)
                        + " {ResponseTime} {WebApi} {PosNo} {HttpContext} {trace_id} {span_id}",
                    timeResponse, webApi + endPoint, posNo, "Response", traceId, spanId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Fire-and-Forget log");
            }
        });
    }

    public void LogException(string webApi, string posNo, long timeResponse, string endPoint, string bodyJson)
    {
        var (traceId, spanId) = GetTrace();
        _ = Task.Run(() =>
        {
            try
            {
                Log.Logger.Error(
                    string.Format("{0} response: {1}", endPoint, bodyJson)
                        + " {ResponseTime} {WebApi} {PosNo} {HttpContext} {trace_id} {span_id}",
                    timeResponse, webApi + endPoint, posNo, "Exception", traceId, spanId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Fire-and-Forget log");
            }
        });
    }

    private static (string traceId, string spanId) GetTrace()
        => (Activity.Current?.TraceId.ToString() ?? "", Activity.Current?.SpanId.ToString() ?? "");
}
