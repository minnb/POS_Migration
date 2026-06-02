using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TCX.API.Common.Shared
{
    public static class HttpClientProvider
    {
        private static readonly ConcurrentDictionary<string, Lazy<HttpClient>> _clients = new ConcurrentDictionary<string, Lazy<HttpClient>>();
        public static HttpClient GetClient(string proxy = "", int timeoutSeconds = 30, string host = "")
        {
            var key = $"{proxy}_{timeoutSeconds}_{host?.ToLowerInvariant()}";
            return _clients.GetOrAdd(key, _ => new Lazy<HttpClient>(() =>
            {
                var handler = new HttpClientHandler
                {
                    UseProxy = !string.IsNullOrEmpty(proxy),
                    Proxy = string.IsNullOrEmpty(proxy) ? null : new WebProxy(proxy)
                };

                var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
                };

                if (!string.IsNullOrEmpty(host))
                {
                    var uri = new Uri(host);
                    client.BaseAddress = uri;
                    var sp = ServicePointManager.FindServicePoint(uri);
                    sp.ConnectionLimit = timeoutSeconds * 6;
                    sp.ConnectionLeaseTimeout = 60000;
                }

                return client;
            })).Value;
        }
        public static HttpClient GetClientV2(string proxy = "", int timeoutSeconds = 30, string host = "")
        {
            var key = $"{proxy}_{timeoutSeconds}_{host?.ToLowerInvariant()}";
            return _clients.GetOrAdd(key, _ => new Lazy<HttpClient>(() =>
            {
                var handler = new HttpClientHandler
                {
                    UseProxy = !string.IsNullOrEmpty(proxy),
                    Proxy = string.IsNullOrEmpty(proxy) ? null : new WebProxy(proxy)
                };

                var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds + 2)
                };

                if (!string.IsNullOrEmpty(host))
                {
                    var uri = new Uri(host);
                    client.BaseAddress = uri;
                    var sp = ServicePointManager.FindServicePoint(uri);
                    sp.ConnectionLimit = timeoutSeconds * 2;
                    sp.ConnectionLeaseTimeout = 60000;
                }
                return client;
            })).Value;
        }
        public static string GetHttpStatusCodeMessage(int statusCode)
        {
            try
            {
                return ((HttpStatusCode)statusCode).ToString() + $"_{statusCode}";
            }
            catch
            {
                return statusCode.ToString();
            }
        }
    }
}
