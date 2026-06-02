using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Http.Controllers;
using TCX.API.Common.Models;

namespace VCM.POSBLUE.API.Models
{
    public class AuthenAPI
    {
        static readonly string PLGUser = WebConfigurationManager.AppSettings["PLGUser"];
        static readonly string PLGPass = WebConfigurationManager.AppSettings["PLGPassword"];
        public static HttpResponseMessage AuthorizationBasic(HttpActionContext filterContext)
        {
            var header = filterContext.Request.Headers;
            if (header.Authorization == null)
            {
                return filterContext.Request.CreateResponse(HttpStatusCode.Unauthorized, new ResultResponse
                {
                    Message = $"Authorization has been denied for this request",
                    Status = HttpStatusCode.Unauthorized
                });
            }
            var checkAuthen = header.Authorization.Parameter;

            var byteArray = Encoding.ASCII.GetBytes($"{PLGUser}:{PLGPass}");
            var authenBasic = Convert.ToBase64String(byteArray);
            if (checkAuthen != authenBasic)
            {
                return filterContext.Request.CreateResponse(HttpStatusCode.Unauthorized, new ResultResponse
                {
                    Message = $"Authorization has been denied for this request",
                    Status = HttpStatusCode.Unauthorized
                });
            }

            return filterContext.Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
            {
                Message = $"Authorization Success",
                Status = HttpStatusCode.OK
            });
        }
    }
}