using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Serilog;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Infrastructure.Http;

/// <summary>
/// Triển khai IApiCallClient — port HttpClientWithApi của ApiCallHelper cũ sang async + IHttpClientFactory.
/// Giữ NGUYÊN semantics status-code: OK/Created→body; Conflict→"Conflict"; Unauthorized→err JSON;
/// 502/403/503→retry 1 lần; khác→rỗng + ServerErrorResponses. Hỗ trợ proxy (cache client theo proxy).
/// </summary>
public sealed class ApiCallClient : IApiCallClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly ConcurrentDictionary<string, HttpClient> _proxyClients = new();

    public ApiCallClient(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<ApiCallResult> SendAsync(ApiCallRequest request)
    {
        var result = new ApiCallResult();
        try
        {
            var client = GetClient(request);

            // Headers
            if (request.Headers is { Count: > 0 })
            {
                foreach (var item in request.Headers)
                {
                    client.DefaultRequestHeaders.Remove(item.Key);
                    client.DefaultRequestHeaders.Add(item.Key, item.Value);
                }
            }
            if (!string.IsNullOrEmpty(request.AccessToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(request.TokenType ?? "Bearer", request.AccessToken);

            var sw = Stopwatch.StartNew();
            var response = await CallAsync(client, request);

            result.Error = new ServerErrorResponses
            {
                StatusCode = (int)response.StatusCode,
                DataRequest = request.JsonData,
                DataResponse = JsonConvert.SerializeObject(response),
            };

            if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
            {
                result.Response = await response.Content.ReadAsStringAsync();
            }
            else if (response.StatusCode == HttpStatusCode.Conflict)
            {
                result.Response = HttpStatusCode.Conflict.ToString();
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                result.Response = JsonConvert.SerializeObject(new ServerErrorResponses
                {
                    StatusCode = (int)response.StatusCode,
                    DataResponse = "Unauthorized: lỗi xác thực Webapi Capillary",
                });
                sw.Stop();
                return result;
            }
            else if (response.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.Forbidden or HttpStatusCode.ServiceUnavailable)
            {
                // retry 1 lần
                response = await CallAsync(client, request);
                if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
                    result.Response = await response.Content.ReadAsStringAsync();
                else
                    result.Response = response.StatusCode.ToString();
            }
            else
            {
                result.Response = "";
                result.Error = new ServerErrorResponses
                {
                    StatusCode = (int)response.StatusCode,
                    DataRequest = request.JsonData,
                    DataResponse = JsonConvert.SerializeObject(response.Content),
                };
                Log.Information("{Url} error httpStatus: {Status}", request.Host + request.Endpoint, response.StatusCode);
            }

            sw.Stop();
            result.ResponseTimeMs = sw.ElapsedMilliseconds;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            result.Error = new ServerErrorResponses { StatusCode = (int)HttpStatusCode.RequestTimeout, DataRequest = request.JsonData, DataResponse = "Timeout" };
            result.Response = "";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ApiCallClient.SendAsync {Url}", request.Host + request.Endpoint);
            result.Error = new ServerErrorResponses { StatusCode = 500, DataRequest = request.JsonData, DataResponse = ex.Message };
            result.Response = "";
        }
        return result;
    }

    private HttpClient GetClient(ApiCallRequest request)
    {
        var timeout = TimeSpan.FromSeconds(request.Timeout + 5);
        if (string.IsNullOrEmpty(request.ProxyHttp))
        {
            var client = _httpClientFactory.CreateClient("partner");
            client.Timeout = timeout;
            return client;
        }

        // Proxy-aware client, cache theo proxy để tránh socket exhaustion
        return _proxyClients.GetOrAdd(request.ProxyHttp, proxy =>
        {
            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxy) { BypassProxyOnLocal = true },
                UseProxy = true,
            };
            return new HttpClient(handler) { Timeout = timeout };
        });
    }

    private static async Task<HttpResponseMessage> CallAsync(HttpClient client, ApiCallRequest request)
    {
        switch (request.WebMethod.ToUpperInvariant())
        {
            case "POST":
                using (var content = new StringContent(request.JsonData ?? "", Encoding.UTF8, "application/json"))
                    return await client.PostAsync(request.Host + request.Endpoint, content);
            case "PUT":
                using (var content = new StringContent(request.JsonData ?? "", Encoding.UTF8, "application/json"))
                    return await client.PutAsync(request.Host + request.Endpoint, content);
            case "GET":
                return await client.GetAsync(request.Host + request.Endpoint);
            default:
                return new HttpResponseMessage();
        }
    }
}
