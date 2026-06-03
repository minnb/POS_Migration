using System;
using System.Collections.Concurrent;
using System.Net;
using System.Web;
using System.Web.Http.Filters;

public class RateLimitMiddleware : ActionFilterAttribute
{
    private static readonly ConcurrentDictionary<string, RequestBucket> RequestBuckets = new ConcurrentDictionary<string, RequestBucket>();
    private static readonly int MaxRequests = 100;
    private static readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(1);

    public override void OnActionExecuting(System.Web.Http.Controllers.HttpActionContext actionContext)
    {
        var clientId = GetClientId(actionContext);
        var currentTime = DateTime.UtcNow;

        var bucket = RequestBuckets.GetOrAdd(clientId, new RequestBucket(currentTime));

        if (bucket.IsRateLimited(currentTime, MaxRequests, TimeWindow))
        {
            actionContext.Response = new System.Net.Http.HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new System.Net.Http.StringContent("Rate limit exceeded. Please try again later.")
            };
        }
        else
        {
            base.OnActionExecuting(actionContext);
        }
    }

    private string GetClientId(System.Web.Http.Controllers.HttpActionContext actionContext)
    {
        return HttpContext.Current.Request.UserHostAddress;
    }
}

public class RequestBucket
{
    private int _requestCount;
    private DateTime _lastRequestTime;

    public RequestBucket(DateTime firstRequestTime)
    {
        _requestCount = 0;
        _lastRequestTime = firstRequestTime;
    }

    public bool IsRateLimited(DateTime currentTime, int maxRequests, TimeSpan timeWindow)
    {
        if (_lastRequestTime + timeWindow < currentTime)
        {
            _lastRequestTime = currentTime;
            _requestCount = 0;
        }

        _requestCount++;
        return _requestCount > maxRequests;
    }
}
