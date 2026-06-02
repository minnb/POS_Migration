
using Confluent.Kafka;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TCX.API.Common.Helpers;

namespace TCX.WebApiCore.Shared
{
    public class RestSharpApiHelper
    {
        public string AppCode { get; set; }
        public string ApiAddress { get; set; }
        public string ApiRoute { get; set; }
        public string WebMethod { get; set; }
        public object JsonData { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public int SetTimeOut { get; set; }
        public string HttpProxy { get; set; }
        public int PortProxy { get; set; }

        private static RestClient _client;
        private static readonly object _lock = new object();
        public RestSharpApiHelper() 
        {
            
        }
        public RestSharpApiHelper(
            string appCode,
            string apiAdress,
            string apiRoute,
            string webMethod,
            Dictionary<string, string> headers = null,
            Dictionary<string, string> parameters = null,
            object jsonData = null,
            int setTimeOut = 30,
            string httpProxy = null,
            int portProxy = 0)
        {
            this.AppCode = appCode;
            this.ApiAddress = apiAdress;
            this.ApiRoute = apiRoute;
            this.WebMethod = webMethod;
            this.Headers = headers;
            this.Parameters = parameters;
            this.JsonData = jsonData;
            this.SetTimeOut = setTimeOut;
            this.HttpProxy = httpProxy;
            this.PortProxy = portProxy;
            InitializeClient();
        }
        private void InitializeClient()
        {
            if (_client == null)
            {
                lock (_lock)
                {
                    if (_client == null)
                    {
                        var options = new RestClientOptions(ApiAddress)
                        {
                            Timeout = TimeSpan.FromSeconds(SetTimeOut)
                        };

                        if (!string.IsNullOrEmpty(HttpProxy))
                        {
                            WebProxy myproxy = new WebProxy(HttpProxy);
                            options.Proxy = myproxy;
                            options.ThrowOnAnyError = true;
                        }
                        _client = new RestClient(options);
                    }
                }
            }
        }
        public string InteractWithApi(ref HttpStatusCode statusCode, ref long processTime)
        {
            var st1 = Stopwatch.StartNew();
            st1.Start();
            string result = string.Empty;
            long milliseconds;
            try
            {
                RestRequest request = new RestRequest(ApiRoute, GetMethod());
                if (Headers != null)
                {
                    foreach (var p in Headers)
                    {
                        request.AddOrUpdateHeader(p.Key, p.Value);
                    }
                }
                if (JsonData != null)
                {
                    request.AddJsonBody(JsonData);
                }
                if (Parameters != null)
                {
                    foreach (var p in Parameters)
                    {
                        if (p.Key == "application/xml")
                        {
                            request.AddParameter("application/xml", p.Value, ParameterType.RequestBody);
                        }
                        else
                        {
                            request.AddParameter(p.Key, p.Value);
                        }
                    }
                }

                var rp = _client.Execute(request);
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                processTime = milliseconds;
                statusCode = rp != null ? rp.StatusCode : HttpStatusCode.BadGateway;
                if(rp != null)
                {
                    if (rp.StatusCode == HttpStatusCode.OK)
                    {
                        result = rp.Content ?? HttpStatusCode.OK.ToString();
                    }
                    else
                    {
                        result = rp.Content;
                    }
                }
                else
                {
                    statusCode = HttpStatusCode.BadGateway;
                    result = "No response from server.";
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                processTime = milliseconds;
                var timeoutResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent("Request timed out.")
                };
                FileHelper.WriteLogs($"InteractWithApi.TaskCanceledException Request timed out: {ApiAddress}/{ApiRoute}: {JsonConvert.SerializeObject(ex)}");
                return $"408_RequestTimeout_{ApiAddress}";
            }
            catch (WebException ex)
            {
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                processTime = milliseconds;
                using (WebResponse response = ex.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    using (Stream data = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(data))
                        {
                            result = reader.ReadToEnd();
                        }
                    }
                    FileHelper.WriteLogs($"InteractWithApi.WebException: {ApiAddress}/{ApiRoute}: {JsonConvert.SerializeObject(ex)}");
                }
            }
            catch (Exception ex)
            {
                st1.Stop();
                processTime = st1.ElapsedMilliseconds;
                statusCode = HttpStatusCode.InternalServerError;

                FileHelper.WriteLogs($"Unexpected error: {ApiAddress}/{ApiRoute}. Error: {ex}");
                result = "Unexpected error occurred.";
            }

            finally
            {
                st1.Stop();
            }
            return result;
        }
        public async Task<(string, HttpStatusCode, long)> InteractWithApiAsync()
        {
            var st1 = Stopwatch.StartNew();
            st1.Start();
            string result = string.Empty;
            long milliseconds;
            HttpStatusCode statusCode;
            try
            {
                RestRequest request = new RestRequest(ApiRoute, GetMethod());
                request.AddHeader("Content-Type", "application/json");
                if (Headers != null)
                {
                    foreach (var p in Headers)
                    {
                        request.AddHeader(p.Key, p.Value);
                    }
                }
                if (JsonData != null)
                {
                    //string rawJson = StringHelper.ObjectToStringLowercase(JsonData);
                    request.AddParameter("application/json", JsonData, ParameterType.RequestBody);
                }
                if (Parameters != null)
                {
                    foreach (var p in Parameters)
                    {
                        if (p.Key == "application/xml")
                        {
                            request.AddParameter("application/xml", p.Value, ParameterType.RequestBody);
                        }
                        else
                        {
                            request.AddParameter(p.Key, p.Value);
                        }
                    }
                }
                
                //FileHelper.WriteLogs(ToCurl(_client, request));
                var rp = await _client.ExecuteAsync(request);
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                statusCode = rp != null ? rp.StatusCode : HttpStatusCode.BadGateway;
                if (rp != null)
                {
                    if (rp.StatusCode == HttpStatusCode.OK)
                    {
                        result = rp.Content ?? HttpStatusCode.OK.ToString();
                    }
                    else
                    {
                        result = rp.Content;
                    }
                }
                else
                {
                    statusCode = HttpStatusCode.BadGateway;
                    result = "No response from server.";
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                var timeoutResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent("Request timed out.")
                };
                FileHelper.WriteLogs($"InteractWithApi.TaskCanceledException Request timed out: {ApiAddress}/{ApiRoute}: {JsonConvert.SerializeObject(ex)}");
                return ($"408_RequestTimeout_{ApiAddress}", HttpStatusCode.RequestTimeout, 0);
            }
            catch (WebException ex)
            {
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                using (WebResponse response = ex.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    using (Stream data = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(data))
                        {
                            result = reader.ReadToEnd();
                        }
                    }
                    FileHelper.WriteLogs($"InteractWithApi.WebException: {ApiAddress}/{ApiRoute}: {JsonConvert.SerializeObject(ex)}");
                }
            }
            catch (Exception ex)
            {
                st1.Stop();
                statusCode = HttpStatusCode.InternalServerError;
                FileHelper.WriteLogs($"Unexpected error: {ApiAddress}/{ApiRoute}. Error: {ex}");
                result = "Unexpected error occurred.";
            }

            finally
            {
                st1.Stop();
            }
            return (result, HttpStatusCode.OK, st1.ElapsedMilliseconds);
        }
        private Method GetMethod()
        {
            var method = new Method();
            switch (WebMethod.ToUpper())
            {
                case "POST":
                    method = Method.Post;
                    break;
                case "DELETE":
                    method = Method.Delete;
                    break;
                case "PUT":
                    method = Method.Put;
                    break;
                case "GET":
                    method = Method.Get;
                    break;
                case "PATCH":
                    method = Method.Patch;
                    break;
                default:
                    Console.WriteLine("Default case");
                    break;
            }
            return method;
        }
        public static string ToCurl(IRestClient client, RestRequest request)
        {
            var method = request.Method.ToString();
            var uri = client.BuildUri(request);
            var curl = $"curl --location --request {method} '{uri}'";

            // 1. Thêm Headers
            foreach (var header in request.Parameters.Where(p => p.Type == ParameterType.HttpHeader))
            {
                curl += $" \\\n--header '{header.Name}: {header.Value}'";
            }

            // 2. Thêm Body (JSON)
            var body = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
            if (body != null)
            {
                curl += $" \\\n--data-raw '{body.Value}'";
            }

            return curl;
        }
    }
}
