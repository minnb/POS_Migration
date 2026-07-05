namespace POS.Api.Middleware;

/// <summary>
/// Cấu hình cho <see cref="RequestResponseLoggingMiddleware"/>. Section "RequestLogging" trong
/// appsettings. Không có cờ "dùng Kibana hay không" — middleware luôn gọi qua IKibanaService,
/// Elasticsearch sink tự no-op nếu Elasticsearch:Nodes rỗng. `PersistToFile` được đọc riêng, trực
/// tiếp trong SerilogConfiguration.cs (không qua class này) vì cần lúc bootstrap Serilog.
/// </summary>
public sealed class RequestLoggingOptions
{
    public const string SectionName = "RequestLogging";

    public bool Enabled { get; set; }
    public int MaxBodyBytes { get; set; } = 8192;
    public string[] ExcludePaths { get; set; } = ["/health", "/swagger"];
}
