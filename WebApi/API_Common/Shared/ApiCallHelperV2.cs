using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Loyalty.CX;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;

namespace TCX.API.Common.Shared
{
    public class ApiCallHelperV2
    {
        public string Host { get; set; }
        public string Enpoint { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string TokenType { get; set; }
        public string AccessToken { get; set; }
        public string WebMethod { get; set; }
        public string JsonData { get; set; }
        public int Timeout { get; set; }
        public string ProxyHttp { get; set; }
        public ApiCallHelperV2(
            string host,
            string apiAdress,
            Dictionary<string, string> headers,
            string tokenType,
            string accessToken,
            string webMethod,
            string jsonData = null,
            int timeout = 30,
            string proxyHttp = ""
            )
        {
            this.Host = host;
            this.Enpoint = apiAdress;
            this.Headers = headers;
            this.TokenType = tokenType;
            this.AccessToken = accessToken;
            this.WebMethod = webMethod;
            this.JsonData = jsonData;
            this.ProxyHttp = proxyHttp;
            this.Timeout = timeout;
        }
        private async Task<HttpResponseMessage> CallApi(HttpClient client, string method, HttpRequestMessage request)
        {
            switch (method)
            {
                case "GET":
                    return await client.SendAsync(request);
                case "POST":
                    return await client.SendAsync(request);

            }
            return new HttpResponseMessage();
        }

        private static readonly AsyncPolicy _policy = Policy.Handle<TaskCanceledException>().Or<HttpRequestException>()
                                                            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        public async Task<Tuple<string, long, ServerErrorResponses>> HttpClientWithApi()
        {
            long responseTime = 0;
            string request_id = "";
            string result;
            var responseErr = new ServerErrorResponses()
            {
                DataRequest = JsonData
            };
            try
            {
                var st1 = new Stopwatch();
                st1.Start();
                var client = HttpClientProvider.GetClientV2(ProxyHttp, Timeout, Host);

                var request = new HttpRequestMessage(new HttpMethod(WebMethod), Host + Enpoint);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (Headers != null && Headers.Count > 0)
                {
                    foreach (var item in Headers)
                    {
                        if (request.Content != null && IsContentHeader(item.Key))
                        {
                            request.Content.Headers.TryAddWithoutValidation(item.Key, item.Value);
                        }
                        else
                        {
                            request.Headers.TryAddWithoutValidation(item.Key, item.Value);
                        }
                        if(item.Key == "X-API-REQUEST")
                        {
                            request_id = item.Value;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(AccessToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TokenType, AccessToken);
                }
                if (!string.IsNullOrEmpty(JsonData))
                {
                    request.Content = new StringContent(JsonData, Encoding.UTF8, "application/json");
                }

                //var response = await CallApi(client, WebMethod, request);
                var response =  await _policy.ExecuteAsync(async () => {
                    return await CallApi(client, WebMethod, request);
                });

                responseErr.StatusCode = (int)response.StatusCode;
                responseErr.DataRequest = JsonData;
                responseErr.DataResponse = JsonConvert.SerializeObject(response);
                responseErr.RequestId = request_id;

                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    result = HttpStatusCode.Conflict.ToString();
                    FileHelper.WriteLogs(Host + " ==> Conflict: " + response.Content.ReadAsStringAsync().Result);
                }
                else if(response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    responseErr = new ServerErrorResponses()
                    {
                        StatusCode = (int)response.StatusCode,
                        DataRequest = JsonData,
                        DataResponse = JsonConvert.SerializeObject(response)
                    };
                    result = response.StatusCode.ToString();
                }
                else if (response.StatusCode == HttpStatusCode.BadGateway
                    || response.StatusCode == HttpStatusCode.Forbidden
                    || response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    responseErr = new ServerErrorResponses()
                    {
                        StatusCode = (int)response.StatusCode,
                        DataRequest = JsonData,
                        DataResponse = JsonConvert.SerializeObject(response)
                    };
                    FileHelper.WriteLogs($"HttpClientWithApi.Capillary.Response: " + JsonConvert.SerializeObject(response.Content.ReadAsStringAsync()));
                    response = await CallApi(client, WebMethod, request);
                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        result = response.Content.ReadAsStringAsync().Result;
                    }
                    else if (response.StatusCode == HttpStatusCode.BadGateway
                            || response.StatusCode == HttpStatusCode.Forbidden
                            || response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        responseErr = new ServerErrorResponses()
                        {
                            StatusCode = (int)response.StatusCode,
                            DataRequest = JsonData,
                            DataResponse = JsonConvert.SerializeObject(response)
                        };
                    }
                    result = response.StatusCode.ToString();
                }
                else
                {
                    result = "";
                    responseErr = new ServerErrorResponses()
                    {
                        StatusCode = (int)response.StatusCode,
                        DataRequest = JsonData,
                        DataResponse = JsonConvert.SerializeObject(response.Content),
                        RequestId = request_id
                    };
                    FileHelper.WriteLogs($"HttpClientWithApi.Error: {response.StatusCode} " + JsonConvert.SerializeObject(responseErr));
                }

                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var timeoutResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent($"RequestId:{request_id} timed out. {Enpoint}_{JsonData}")
                };
                return new Tuple<string, long, ServerErrorResponses>($"408_requestTimedOut {Timeout} second. RequestId:{request_id}",
                                                                    responseTime, new ServerErrorResponses()
                                                                    {
                                                                        DataResponse = ex.Message.ToString() ?? "",
                                                                        DataRequest =JsonConvert.SerializeObject(timeoutResponse)
                                                                    });
            }
            catch (WebException ex)
            {
                result = ex.Message.ToString() ?? "";
                if (string.IsNullOrEmpty(result))
                {
                    using (WebResponse response = ex.Response)
                    {
                        if (response != null)
                        {
                            HttpWebResponse httpResponse = (HttpWebResponse)response;
                            using (var data = response.GetResponseStream())
                            {
                                using (var reader = new StreamReader(data))
                                {
                                    result = reader.ReadToEnd();
                                }
                            }
                        }
                    }
                }
                result = $"{Enpoint} 408_requestTimedOut: {Timeout}_requestId:{request_id} body: {JsonData}" + result;
                StringHelper.Loggingz(result);
            }
            return new Tuple<string, long, ServerErrorResponses>(result, responseTime, responseErr); ;
        }
        public async Task<Tuple<HttpResponseMessage, long, ServerErrorResponses>> GetHttpResponseMessage()
        {
            long responseTime = 0;
            var errorResponse = new ServerErrorResponses { DataRequest = JsonData };
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var client = HttpClientProvider.GetClient(ProxyHttp, Timeout, Host);
                var request = new HttpRequestMessage(new HttpMethod(WebMethod), Host + Enpoint);

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(JsonData))
                {
                    request.Content = new StringContent(JsonData, Encoding.UTF8, "application/json");
                }

                if (Headers != null)
                {
                    foreach (var header in Headers)
                    {
                        if (IsContentHeader(header.Key))
                        {
                            request.Content = new StringContent("", Encoding.UTF8, "application/json");
                            request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                        else
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                // Authorization
                if (!string.IsNullOrEmpty(AccessToken))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", AccessToken);
                }

                var response = await client.SendAsync(request);
                stopwatch.Stop();
                responseTime = stopwatch.ElapsedMilliseconds;

                return Tuple.Create(response, responseTime, null as ServerErrorResponses);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var timeoutMsg = $"Request timed out. {Enpoint}_{JsonData}";
                var timeoutResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent(timeoutMsg)
                };

                return Tuple.Create(
                    timeoutResponse,
                    responseTime,
                    new ServerErrorResponses
                    {
                        DataResponse = ex.Message,
                        DataRequest = JsonConvert.SerializeObject(timeoutResponse)
                    }
                );
            }
            catch (WebException ex)
            {
                string resultMsg = ex.Message ?? "";
                if (string.IsNullOrEmpty(resultMsg) && ex.Response is HttpWebResponse httpResponse)
                {
                    using (var stream = httpResponse.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            resultMsg = reader.ReadToEnd();
                        }
                    }                   
                }

                resultMsg = $"{Enpoint} 408_requestTimedOut: {Timeout}_request body: {JsonData} {resultMsg}";
                return Tuple.Create(
                    new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                    {
                        Content = new StringContent(resultMsg)
                    },
                    responseTime,
                    new ServerErrorResponses
                    {
                        DataResponse = ex.Message,
                        DataRequest = null
                    }
                );
            }
        }
        private static bool IsContentHeader(string headerKey)
        {
            var contentHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Content-Type",
                "Content-Length",
                "Content-Disposition",
                "Content-Encoding",
                "Content-Language",
                "Content-Location",
                "Content-MD5",
                "Content-Range"
            };
            return contentHeaders.Contains(headerKey);
        }
    }
}
