using Newtonsoft.Json;
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
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;

namespace TCX.API.Common.Shared
{
    public class ApiCallHelper
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
        public string BypassList { get; set; }
        public ApiCallHelper(
            string host,
            string apiAdress,
            Dictionary<string, string> headers,
            string tokenType,
            string accessToken,
            string webMethod,
            string jsonData = null,
            int timeout = 30,
            string proxyHttp = "",
            string bypassList = ""
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
            this.BypassList = bypassList;
            this.Timeout = timeout;
        }
        private HttpResponseMessage CallApi(HttpClient client, string method)
        {
            switch (method)
            {
                case "POST":
                    using (var httpContentPOST = new StringContent(JsonData))
                    {
                        httpContentPOST.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        return client.PostAsync(Enpoint, httpContentPOST).ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                case "PUT":
                    HttpContent httpContentPUT = new StringContent(JsonData);
                    httpContentPUT.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return client.PutAsync(Enpoint, httpContentPUT).ConfigureAwait(false).GetAwaiter().GetResult();              
                case "GET":
                    return client.GetAsync(Host + Enpoint).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            return new HttpResponseMessage();
        }
        private async Task<HttpResponseMessage> CallApiAsync(HttpClient client, string method)
        {
            switch (method)
            {
                case "POST":
                    using (var httpContentPOST = new StringContent(JsonData))
                    {
                        httpContentPOST.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        return await client.PostAsync(Enpoint, httpContentPOST);
                    }
                case "PUT":
                    HttpContent httpContentPUT = new StringContent(JsonData);
                    httpContentPUT.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return await client.PutAsync(Enpoint, httpContentPUT);
                case "GET":
                    return await client.GetAsync(Host + Enpoint);
            }
            return new HttpResponseMessage();
        }
        public string HttpClientWithApi(ref long responseTime, ref ServerErrorResponses responseErr)
        {
            responseTime = 0;
            string result;
            try
            {
                var client = HttpClientProvider.GetClient(ProxyHttp, Timeout + 5, Host.TrimEnd('/'));
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

                var st1 = new Stopwatch();
                st1.Start();
                
                var response = CallApi(client, WebMethod);
                StringHelper.Loggingz($"HttpClientWithApi.Response: {response.Content.ReadAsStringAsync().Result}");
                
                responseErr = new ServerErrorResponses()
                {
                    StatusCode = (int)response.StatusCode,
                    DataRequest = JsonData,
                    DataResponse = JsonConvert.SerializeObject(response)
                };

                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    result = HttpStatusCode.Conflict.ToString();
                    FileHelper.WriteLogs(Host + " ==> Conflict: " + response.Content.ReadAsStringAsync().Result);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return JsonConvert.SerializeObject(new ServerErrorResponses()
                    {
                        StatusCode = (int)response.StatusCode,
                        DataRequest = null,
                        DataResponse = $"Unauthorized: lỗi xác thực Webapi Capillary"
                    });
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

                    response = CallApi(client, WebMethod);
                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        result = response.Content.ReadAsStringAsync().Result;
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return JsonConvert.SerializeObject(new ServerErrorResponses()
                        {
                            StatusCode = (int)response.StatusCode,
                            DataRequest = null,
                            DataResponse = $"Unauthorized: lỗi xác thực Webapi Capillary"
                        });
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
                        DataResponse = JsonConvert.SerializeObject(response.Content)
                    };
                    FileHelper.WriteLogs($"{Host+Enpoint} error httpStatus: {response.StatusCode} " + JsonConvert.SerializeObject(responseErr));
                }

                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var timeoutResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent($"request timed out. {Enpoint}_{JsonData}")
                };
                return $"408_requestTimedOut {Timeout} second. {Enpoint}_{JsonData}";
            }
            catch (WebException ex)
            {
                FileHelper.WriteLogs("HttpClientWithApi.WebException:" + JsonConvert.SerializeObject(ex));
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
                result = $"{Enpoint}_timeOut: {Timeout}_request: {JsonData}" + result;
            }
            return result;
        }
        private async Task<HttpResponseMessage> CallApiClouldAsync(HttpClient client, string method)
        {
            switch (method)
            {
                case "POST":
                    using (var httpContentPOST = new StringContent(JsonData, Encoding.UTF8, "application/json"))
                    {
                        return await client.PostAsync(Enpoint, httpContentPOST);
                    }
                case "PUT":
                    HttpContent httpContentPUT = new StringContent(JsonData);
                    return await client.PutAsync(Enpoint, httpContentPUT);
                case "GET":
                    return await client.GetAsync(Host + Enpoint);
            }
            return new HttpResponseMessage();
        }
        public async Task<Tuple<string, long, ServerErrorResponses>> HttpClientWithApiClould()
        {
            string result;
            ServerErrorResponses responseErr = null;
            long responseTime = 0;
            try
            {
                var client = HttpClientProvider.GetClient(ProxyHttp, Timeout + 5, Host.TrimEnd('/'));
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
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                
                var st1 = new Stopwatch();
                st1.Start();

                var response = await CallApiClouldAsync(client, WebMethod);
                StringHelper.Loggingz($"HttpClientWithApiClould.Response: {response.Content.ReadAsStringAsync().Result}");

                responseErr = new ServerErrorResponses()
                {
                    StatusCode = (int)response.StatusCode,
                    DataRequest = JsonData,
                    DataResponse = JsonConvert.SerializeObject(response)
                };

                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    result = HttpStatusCode.Conflict.ToString();
                    FileHelper.WriteLogs(Host + " ==> Conflict: " + response.Content.ReadAsStringAsync().Result);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new Tuple<string, long, ServerErrorResponses>(JsonConvert.SerializeObject(new ServerErrorResponses()
                    {
                        StatusCode = (int)response.StatusCode,
                        DataRequest = null,
                        DataResponse = $"Unauthorized: lỗi xác thực Webapi Capillary"
                    }), 0 , null);
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

                    response = CallApi(client, WebMethod);
                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        result = response.Content.ReadAsStringAsync().Result;
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return new Tuple<string, long, ServerErrorResponses>(JsonConvert.SerializeObject(new ServerErrorResponses()
                        {
                            StatusCode = (int)response.StatusCode,
                            DataRequest = null,
                            DataResponse = $"Unauthorized: lỗi xác thực Webapi Capillary"
                        }), 0, null);
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
                        DataResponse = JsonConvert.SerializeObject(response.Content)
                    };
                    FileHelper.WriteLogs($"{Host + Enpoint} error httpStatus: {response.StatusCode} " + JsonConvert.SerializeObject(responseErr));
                }

                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var timeoutResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent($"request timed out. {Enpoint}_{JsonData}")
                };
                return new Tuple<string, long, ServerErrorResponses>($"408_requestTimedOut {Timeout} second. {Enpoint}_{JsonData}", 0, responseErr) ;
            }
            catch (WebException ex)
            {
                FileHelper.WriteLogs("HttpClientWithApi.WebException:" + JsonConvert.SerializeObject(ex));
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
                result = $"{Enpoint}_timeOut: {Timeout}_request: {JsonData}" + result;
            }
            return new Tuple<string, long, ServerErrorResponses>(result, responseTime, responseErr);
        }
        public async Task<Tuple<string, long, object>> HttpClientWithApiAsync()
        {
            long responseTime = 0;
            ServerErrorResponses responseErr = new ServerErrorResponses();
            string result;
            try
            {
                var client = HttpClientProvider.GetClient(ProxyHttp, Timeout + 2, Host.TrimEnd('/'));
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
                var st1 = new Stopwatch();
                st1.Start();
                var response = await CallApiAsync(client, WebMethod);

                responseErr = new ServerErrorResponses()
                {
                    StatusCode = (int)response.StatusCode,
                    DataRequest = JsonData,
                    DataResponse = JsonConvert.SerializeObject(response.Content.ReadAsStringAsync().Result)
                };

                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                    responseErr = null;
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    result = HttpStatusCode.Conflict.ToString();
                    FileHelper.WriteLogs(Host + " ==> Conflict: " + response.Content.ReadAsStringAsync().Result);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    responseErr = new ServerErrorResponses()
                    {
                        StatusCode = (int)response.StatusCode,
                        DataRequest = null,
                        DataResponse = $"Unauthorized: lỗi xác thực Webapi Capillary"
                    };
                    return new Tuple<string, long, object>(JsonConvert.SerializeObject(responseErr), responseTime, responseErr);
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

                    response = await CallApiAsync(client, WebMethod);
                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        result = response.Content.ReadAsStringAsync().Result;
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        responseErr = new ServerErrorResponses()
                        {
                            StatusCode = (int)response.StatusCode,
                            DataRequest = null,
                            DataResponse = $"Unauthorized: lỗi xác thực Webapi Capillary"
                        };
                        return new Tuple<string, long, object>(JsonConvert.SerializeObject(responseErr), responseTime, responseErr);
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
                        DataResponse = JsonConvert.SerializeObject(response.Content.ReadAsStringAsync().Result)
                    };
                    FileHelper.WriteLogs($"{Host+Enpoint} HttpClientWithApiAsync.Error: {response.StatusCode} " + JsonConvert.SerializeObject(responseErr));
                }

                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var timeoutResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent($"request timed out. {Enpoint}_{JsonData}")
                };
                return new Tuple<string, long, object>($"408_requestTimedOut {Timeout} second. {Enpoint}_{JsonData}", responseTime, responseErr);
            }
            catch (WebException ex)
            {
                FileHelper.WriteLogs("HttpClientWithApiAsync.WebException:" + JsonConvert.SerializeObject(ex));
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
                result = $"{Enpoint}_timeOut: {Timeout}_request: {JsonData}" + result;
            }
            return new Tuple<string, long, object>(result, responseTime, responseErr);
        }
        public string HttpClientWithApiOld(ref long responseTime, ref ServerErrorResponses responseErr)
        {
            string result = string.Empty;
            responseTime = 0;
            var proxiedHttpClientHandler = new HttpClientHandler
            {
                UseProxy = !string.IsNullOrEmpty(ProxyHttp)
            };

            if (proxiedHttpClientHandler.UseProxy)
            {
                proxiedHttpClientHandler.Proxy = new WebProxy(ProxyHttp);
            }

            using (var client = new HttpClient(proxiedHttpClientHandler))
            {
                try
                {
                    var st1 = new Stopwatch();
                    st1.Start();

                    client.Timeout = TimeSpan.FromSeconds(Timeout);
                    client.BaseAddress = new Uri(Host);
                    if (Headers != null && Headers.Count > 0)
                    {
                        foreach (var item in Headers)
                        {
                            client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }
                    if (!string.IsNullOrEmpty(AccessToken))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", AccessToken);
                    }

                    var response = CallApi(client, WebMethod);
                    responseErr = new ServerErrorResponses()
                    {
                        StatusCode = (int)response.StatusCode,
                        DataRequest = JsonData,
                        DataResponse = JsonConvert.SerializeObject(response)
                    };

                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        result = response.Content.ReadAsStringAsync().Result;
                    }
                    else if (response.StatusCode == HttpStatusCode.Conflict)
                    {
                        result = HttpStatusCode.Conflict.ToString();
                        FileHelper.WriteLogs(Host + " ==> Conflict: " + response.Content.ReadAsStringAsync().Result);
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return JsonConvert.SerializeObject(new ServerErrorResponses()
                        {
                            StatusCode = (int)response.StatusCode,
                            DataRequest = null,
                            DataResponse = $"Unauthorized: lỗi xác thực Webapi Capillary"
                        });
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

                        response = CallApi(client, WebMethod);
                        if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                        {
                            result = response.Content.ReadAsStringAsync().Result;
                        }
                        else if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            return JsonConvert.SerializeObject(new ServerErrorResponses()
                            {
                                StatusCode = (int)response.StatusCode,
                                DataRequest = null,
                                DataResponse = $"Unauthorized: lỗi xác thực Webapi Capillary"
                            });
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
                            DataResponse = JsonConvert.SerializeObject(response.Content)
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
                        Content = new StringContent($"request timed out. {Enpoint}_{JsonData}")
                    };
                    return $"408_requestTimedOut {Timeout} second. {Enpoint}_{JsonData}";
                }
                catch (WebException ex)
                {
                    FileHelper.WriteLogs("HttpClientWithApi.WebException:" + JsonConvert.SerializeObject(ex));
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
                    result = $"{Enpoint}_timeOut: {Timeout}_request: {JsonData}" + result;
                }
            }
            return result;
        }
        public string InteractWithApi(ref long responseTime)
        {
            string result = string.Empty;
            responseTime = 0;
            try
            {
                var st1 = new Stopwatch();
                st1.Start();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Host + Enpoint);
                request.ContentType = "application/json";
                request.PreAuthenticate = true;
                request.Timeout = Timeout;

                if (!string.IsNullOrEmpty(ProxyHttp) && BypassList.IndexOf(';') > 0)
                {
                    var bypass = BypassList.Split(';').ToArray();
                    WebProxy proxy = new WebProxy(ProxyHttp, false, bypass);
                    request.Proxy = proxy;
                }

                if(Headers!= null && Headers.Count > 0)
                {
                    foreach(var item in Headers)
                    {
                        request.Headers.Add(item.Key, item.Value);
                    }
                }

                if (!string.IsNullOrEmpty(AccessToken))
                {
                    request.Headers.Add("Authorization", AccessToken);
                }

                request.Method = WebMethod;

                if (WebMethod.ToUpper() == "POST" || WebMethod.ToUpper() == "PUT")
                {
                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        streamWriter.Write(JsonData);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                }

                HttpWebResponse Response = null;
                Response = (HttpWebResponse)request.GetResponse();

                if (Response.StatusCode == HttpStatusCode.OK || Response.StatusCode == HttpStatusCode.Created)
                {
                    using (Stream stream = Response.GetResponseStream())
                    {
                        StreamReader streamReader = new StreamReader(stream, System.Text.Encoding.UTF8);
                        result = streamReader.ReadToEnd();
                    }
                }
                else
                {
                    result = string.Empty;
                }

                responseTime = st1.ElapsedMilliseconds;
                st1.Stop();
            }
            catch (WebException ex)
            {
                FileHelper.WriteLogs("InteractWithApi WebException:" + JsonConvert.SerializeObject(ex));
                result = ex.Message.ToString() ??"";
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
            }
            return result;
        }
    }
}
