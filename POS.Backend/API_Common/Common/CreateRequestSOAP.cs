using System.Net;

namespace TCX.API.Common.Common
{
    public class CreateRequestSOAP
    {
        public static HttpWebRequest CreateWebRequestSOAP(string endPoint, string user, string passWord)
        {
            //ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(endPoint);
            webRequest.Headers.Add(@"SOAPAction:http://sap.com/xi/WebService/soap1.1");
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            webRequest.Credentials = new NetworkCredential(user, passWord);
            return webRequest;
        }
    }
}
