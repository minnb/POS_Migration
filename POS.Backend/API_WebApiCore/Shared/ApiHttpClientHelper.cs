using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;

namespace TCX.WebApiCore.Shared
{
    public class ApiHttpClientHelper
    {
        public string Host { get; set; }
        public string Endpoints { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string TokenType { get; set; }
        public string AccessToken { get; set; }
        public string WebMethod { get; set; }
        public string JsonData { get; set; }
        public int Timeout { get; set; }
        public string ProxyHttp { get; set; }
        public ApiHttpClientHelper(
            string host,
            string endpoints,
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
            this.Endpoints = endpoints;
            this.Headers = headers;
            this.TokenType = tokenType;
            this.AccessToken = accessToken;
            this.WebMethod = webMethod;
            this.JsonData = jsonData;
            this.ProxyHttp = proxyHttp;
            this.Timeout = timeout;
        }
        private async Task<HttpResponseMessage> CallApi(HttpClient client, string method)
        {
            switch (method)
            {
                case "POST":
                    HttpContent httpContentPOST = new StringContent(JsonData);
                    httpContentPOST.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return await client.PostAsync(Endpoints, httpContentPOST);
                case "PUT":
                    HttpContent httpContentPUT = new StringContent(JsonData);
                    httpContentPUT.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return await client.PutAsync(Endpoints, httpContentPUT);
                case "GET":
                    return await client.GetAsync(Host + Endpoints);
            }
            return new HttpResponseMessage();
        }
        public async Task<(string, long, int)> HttpClientWithApiAsync()
        {
            string result = string.Empty;
            var st1 = new Stopwatch();
            try
            {
                var client = HttpClientProvider.GetClient(ProxyHttp, Timeout, Host.TrimEnd('/'));
                if (Headers != null && Headers.Count > 0)
                {
                    foreach (var item in Headers)
                    {
                        if (client.DefaultRequestHeaders.Contains(item.Key))
                        {
                            client.DefaultRequestHeaders.Remove(item.Key);
                        }
                        client.DefaultRequestHeaders.Add(item.Key, item.Value);
                    }
                }
                if (!string.IsNullOrEmpty(AccessToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TokenType, AccessToken);
                }

                st1.Start();
                var response = await CallApi(client, WebMethod);
                st1.Stop();

                result = await response.Content.ReadAsStringAsync();
                return (result, st1.ElapsedMilliseconds, (int)response.StatusCode);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                FileHelper.WriteExpLogs(result, ex);
                return ($"{Host + Endpoints} TaskCanceledException: {ex.Message}", Timeout, (int)HttpStatusCode.RequestTimeout);
            }
            catch (WebException ex)
            {
                result += ex.Message;
                FileHelper.WriteExpLogs(result, ex);
                return (result, Timeout, (int)HttpStatusCode.ServiceUnavailable);
            }
            catch (Exception ex)
            {
                result += ex.Message;
                FileHelper.WriteExpLogs(result, ex);
                return (result, Timeout, (int)HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
