using System.Net.Http;
using System.Net;
using System.Web.Http;
using System.Linq;
using System.Net.Sockets;
using System.Web;
using TCX.API.Common.Models;
using Newtonsoft.Json;

namespace VCM.POSBLUE.API.Controllers
{
    public class BaseController : ApiController
    {
        public BaseController()
        {
        }

        public HttpResponseMessage NewExceptionModels()
        {
            var exception = ModelState.Values.First().Errors[0].Exception;
            string messErr;
            if (exception != null)
            {
                messErr = exception.Message ?? "";
            }
            else
            {
                messErr = ModelState.Values.First().Errors[0].ErrorMessage;
            }

            return Request.CreateResponse(HttpStatusCode.Conflict, new ResultResponse
            {
                Data = null,
                Message = messErr,
                Status = HttpStatusCode.Conflict,
                MessageTechnical = JsonConvert.SerializeObject(exception),
            });
        }
        public HttpResponseMessage ExceptionModels()
        {
            var exception = ModelState.Values.First().Errors[0].Exception;
            string messErr;
            if (exception != null)
            {
                messErr = exception.Message ?? "";
            }
            else
            {
                messErr = ModelState.Values.First().Errors[0].ErrorMessage;
            }
            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
            {
                Data = null,
                Message = messErr,
                Status = HttpStatusCode.BadRequest
            });
        }
        public string GetPosNo()
        {
            return GetIpServer();
        }
        public string GetIpServer()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            return addr.LastOrDefault()?.ToString();
        }
        public string GetIpAddressClient()
        {
            try
            {
                var ipServer = GetIpServer();
                var httpContext = HttpContext.Current;
                var ipAddressClient = httpContext.Request.Headers["X-FORWARDED-FOR"];
                if (!string.IsNullOrEmpty(ipAddressClient))
                {
                    ipServer += "@" + ipAddressClient;
                }
                return ipServer;
            }
            catch
            {
                return "localhost";
            }
        }
    }
}