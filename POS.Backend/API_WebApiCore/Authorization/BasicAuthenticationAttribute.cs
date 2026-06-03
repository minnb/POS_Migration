using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System;
using System.Net.Http;
using System.Linq;
using TCX.API.Common.Shared;
using TCX.API.Common.Helpers;
using System.Threading.Tasks;
using System.Web;
using TCX.API.Common.Utils;

namespace TCX.WebApiCore
{
    public class BasicAuthenticationAttribute : AuthorizationFilterAttribute
    {
        private readonly MemoryCacheService _memoryCacheService;
        private readonly KibanaService _kibanaService;
        public BasicAuthenticationAttribute()
        {
            _memoryCacheService = new MemoryCacheService();
            _kibanaService = new KibanaService();
        }
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var authHeader = actionContext.Request.Headers.Authorization;
            string xApi = HttpContext.Current.Request.Headers["X-API"];
            if (!string.IsNullOrEmpty(xApi))
            {
                var xApiValue = _memoryCacheService.GetPOSDataSetup("X-API");
                if (xApi.ToUpper() != EncryptionUtil.GetMd5Hash(xApiValue.Value).ToUpper())
                {
                    Task.Run(() => _kibanaService.LogRequest("Unauthorized-X-API", "X-API", "", actionContext.Request.RequestUri.AbsolutePath + "@" + xApi));
                }
            }

            if (actionContext.Request.RequestUri.AbsolutePath.IndexOf("api/v2") > 0)
            {
                if (authHeader != null)
                {
                    var authenticationToken = actionContext.Request.Headers.Authorization.Parameter;
                    var decodedAuthenticationToken = EncryptionHelper.Base64Decode(authenticationToken); 
                    var usernamePasswordArray = StringHelper.ConverStringToArray(decodedAuthenticationToken, ':'); 
                    if(usernamePasswordArray.Count() > 0)
                    {
                        //// Replace this with your own system of security / means of validating credentials
                        var isValid = _memoryCacheService.GetSysWebApiUser().Where(x => x.UserName == usernamePasswordArray[0] && x.Password == usernamePasswordArray[1]).FirstOrDefault();
                        if (isValid != null)
                        {
                            var principal = new GenericPrincipal(new GenericIdentity(usernamePasswordArray[0]), null);
                            Thread.CurrentPrincipal = principal;
                            return;
                        }
                    }
                }
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized, "API Error 401: Unauthorized");
            }
        }
    }
}