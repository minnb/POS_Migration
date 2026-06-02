using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;

namespace TCX.WebApiCore
{

    public class ApiHelper
    {
        public string ApiAddress { get; set; }
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public string WebMethod { get; set; }
        public string JsonData { get; set; }
        public bool HasHeaders { get; set; }
        public string ProxyHttp { get; set; }
        public string[] BypassList { get; set; }
        public string PosNo { get; set; }

        private readonly KibanaService _kibana;
        public ApiHelper(
            string apiAdress,
            string tokenType,
            string accessToken,
            string webMethod,
            string jsonData = null,
            bool hasHeaders = false,
            string proxyHttp = "",
            string[] bypassList = null,
            string posNo = ""
            )
        {
            this.ApiAddress = apiAdress;
            this.TokenType = tokenType;
            this.AccessToken = accessToken;
            this.WebMethod = webMethod;
            this.JsonData = jsonData;
            this.HasHeaders = hasHeaders;
            this.ProxyHttp = proxyHttp;
            this.BypassList = bypassList;
            _kibana = new KibanaService();
            this.PosNo = posNo;
        }
        public string InteractWithApi()
        {
            string result = string.Empty;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiAddress);
                request.ContentType = "application/json";
                request.PreAuthenticate = true;
                request.Timeout = 45000;

                if (!string.IsNullOrEmpty(ProxyHttp))
                {
                    WebProxy proxy = new WebProxy(ProxyHttp, false, BypassList);
                    request.Proxy = proxy;
                }

                if (HasHeaders)
                {
                    if (!string.IsNullOrEmpty(TokenType))
                    {
                        request.Headers.Add("Authorization", TokenType + " " + AccessToken);
                    }
                    else
                    {
                        request.Headers.Add("Authorization", AccessToken);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(TokenType))
                    {
                        request.Headers.Add(TokenType, AccessToken);
                    }
                }

                Console.WriteLine(request.Headers);
                request.Method = WebMethod;

                long milliseconds = 0;
                var st1 = new Stopwatch();
                st1.Start();

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

                if (Response.StatusCode == HttpStatusCode.OK)
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

                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                _kibana.LogResponse(ApiAddress, PosNo, milliseconds, "", result);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var timeoutResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent("Request timed out.")
                };
                return $"408_RequestTimeout_{ApiAddress}";
            }
            catch (WebException ex)
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
                result += HostHelper.GetServerName() + " - " + ex.Message;
                FileHelper.WriteExpLogs(result, ex);
            }
            return result;
        }
        public HttpWebResponse InteractWithApiResponse()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiAddress);
                request.ContentType = "application/json";
                request.PreAuthenticate = true;
                request.Timeout = 30000;

                if (!string.IsNullOrEmpty(ProxyHttp))
                {
                    WebProxy proxy = new WebProxy(ProxyHttp, false, BypassList);
                    request.Proxy = proxy;
                }

                if (HasHeaders)
                {
                    if (!string.IsNullOrEmpty(TokenType))
                    {
                        request.Headers.Add("Authorization", TokenType + " " + AccessToken);
                    }
                    else
                    {
                        request.Headers.Add("Authorization", AccessToken);
                    }

                }
                else
                {
                    if (!string.IsNullOrEmpty(TokenType))
                    {
                        request.Headers.Add(TokenType, AccessToken);
                    }
                }

                Console.WriteLine(request.Headers);
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
                return (HttpWebResponse)request.GetResponse();
            }
            catch
            {
                return null;
            }
        }
    }

}