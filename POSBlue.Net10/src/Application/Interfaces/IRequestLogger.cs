namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Abstraction cho việc ghi log request/response/exception ra Elasticsearch (Kibana).
/// Thay thế việc khởi tạo trực tiếp KibanaService (new) trong code cũ.
/// Implementation nằm ở tầng Infrastructure.
/// </summary>
public interface IRequestLogger
{
    void LogRequest(string webApi, string posNo, string endPoint, string bodyJson);
    void LogResponse(string webApi, string posNo, long timeResponse, string endPoint, string bodyJson);
    void LogException(string webApi, string posNo, long timeResponse, string endPoint, string bodyJson);
}
