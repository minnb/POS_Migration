using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TCX.WebApiCore;

public abstract class MessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        long responseTime;
        var requestInfo = string.Format("{0}", request.RequestUri);
        
        if(request.Method.ToString() == "GET" && requestInfo.IndexOf("?") > 0)
        {
            requestInfo = requestInfo.Substring(0, requestInfo.IndexOf("?"));
        }

        var st1 = new Stopwatch();
        st1.Start();      

        request.Headers.Add("X-RequestId-Header", Guid.NewGuid().ToString());

        var requestMessage = await request.Content.ReadAsByteArrayAsync();
        if (requestInfo.Contains("UploadFileSale") || requestInfo.Contains("UploadFileLogJob"))
        {
            requestMessage = Encoding.UTF8.GetBytes("");
        }

        // Create Activity early for request logging (Activity.Current will be set)
        Activity activity = null;

        // Check for incoming traceparent header
        string traceParent = null;
        if (request.Headers.Contains("traceparent"))
        {
            traceParent = request.Headers.GetValues("traceparent").FirstOrDefault();
        }


        // Fallback if ActivitySource didn't create activity
        if (activity == null)
        {
            activity = new Activity("HTTP_Request");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();
        }

        // Store in request properties for TraceContextFilter to find
        request.Properties["Activity"] = activity;

        string xApi = HttpContext.Current.Request.Headers["X-API"];
        string userHostAddress = HttpContext.Current.Request.Headers["X-FORWARDED-FOR"];
        if (string.IsNullOrEmpty(userHostAddress))
        {
            userHostAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            if (userHostAddress == "::1") userHostAddress = "127.0.0.1";
        }
        if (!string.IsNullOrEmpty(xApi)) 
        {
            userHostAddress += $" with {xApi}";
        }
        await IncommingMessageAsync(userHostAddress, requestInfo, requestMessage);

        var response = await base.SendAsync(request, cancellationToken);
        byte[] responseMessage;
        if (request.RequestUri.ToString().ToUpper().Contains("Upload".ToUpper()) 
            || request.RequestUri.ToString().ToUpper().Contains("Download".ToUpper())
            || response.Content.Headers.ContentType.MediaType == "application/x-zip-compressed")
        {
            string sentence = "Download-Upload";
            responseMessage = Encoding.UTF8.GetBytes(sentence.ToCharArray());
        }
        else
        {
            if (response.IsSuccessStatusCode)
                responseMessage = await response.Content.ReadAsByteArrayAsync();
            else
                responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
        }

        responseTime = st1.ElapsedMilliseconds;
        st1.Stop();

        await OutgoingMessageAsync(requestInfo, responseMessage, responseTime, userHostAddress ?? "");
        return response;
    }

    protected abstract Task IncommingMessageAsync(string method, string requestInfo, byte[] message);
    protected abstract Task OutgoingMessageAsync(string requestInfo, byte[] message, long responseTime, string userHostAddress);
}

public class MessageLoggingHandler : MessageHandler
{
    private readonly KibanaService _kibana;
    public MessageLoggingHandler()
    {
        _kibana = new KibanaService();
    }
    protected override async Task IncommingMessageAsync(string method, string requestInfo, byte[] message)
    {
        await Task.Run(() => _kibana.LogRequest(requestInfo, method, "", Encoding.UTF8.GetString(message)));
    }
    protected override async Task OutgoingMessageAsync(string requestInfo, byte[] message, long responseTime, string userHostAddress)
    {
        await Task.Run(() => _kibana.LogResponse(requestInfo, userHostAddress, responseTime, "", Encoding.UTF8.GetString(message)));
    }
}