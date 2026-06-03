using Newtonsoft.Json;
using System;
using System.Web;
using System.Web.Security;
using TCX.API.Common.Constants;

namespace VCM.POSBLUE.Model.Authen
{
    public class AuthCookie
    {
        public static void UserSignIn<T>(T user, HttpContextBase curentHttpContext, double minutes, string signInTkn = "")
        {
            var loginToken = new FormsAuthenticationTicket(
                1,
                signInTkn == "" ? LAPConfigResource.AppCookies : signInTkn,
                DateTime.Now,
                DateTime.Now.AddMinutes(minutes),
                true,
                JsonConvert.SerializeObject(user));

            SetAuthCookie(curentHttpContext, loginToken, signInTkn == "" ? LAPConfigResource.AppCookies : signInTkn);
        }

        public static bool CheckLogin(HttpContextBase curentHttpContext)
        {
            var loginTokenCookie = curentHttpContext.Request.Cookies[LAPConfigResource.AppCookies];
            if (loginTokenCookie != null && loginTokenCookie.Value !=string.Empty)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static T CurrentUser<T>(HttpContextBase curentHttpContext) where T : new()
        {
            T userApp = new T();
            var loginTokenCookie = curentHttpContext.Request.Cookies[LAPConfigResource.AppCookies];
            if (loginTokenCookie != null)
            {
                try
                {
                    var token = FormsAuthentication.Decrypt(loginTokenCookie.Value);
                    userApp = JsonConvert.DeserializeObject<T>(token.UserData);
                }
                catch (Exception)
                {
                    userApp = new T();
                }
            }

            return userApp;
        }

        public static void SignOut(HttpContextBase httpContext)
        {
            var cookie = new HttpCookie(LAPConfigResource.AppCookies);
            DateTime nowDateTime = DateTime.Now;
            cookie.Expires = nowDateTime.AddDays(-1);

            //cookie.Domain = ".vinpro.net";

            httpContext.Response.Cookies.Add(cookie);

            //// SET EXPIRE FOR CRM USER
            //var cookie2 = new HttpCookie("SigninToken2");
            //cookie2.Expires = nowDateTime.AddDays(-1);
            //cookie2.Domain = ".vinpro.net";
            //httpContext.Response.Cookies.Add(cookie2);

            //// SET EXPIRE FOR SITE PORTAL
            //var cookie3 = new HttpCookie(ConfigResource.AppCookies);
            //cookie3.Expires = nowDateTime.AddDays(-1);
            //httpContext.Response.Cookies.Add(cookie3);
        }

        public static T GetSession<T>(HttpContextBase context, string key)
        {
            T retSession = default;
            try
            {
                retSession = (T)context.Session[key];
            }
            catch 
            {             
                throw new Exception("Could not convert Session[\"" + key + "\"] to type " + retSession.GetType().Name);
            }

            return retSession;
        }

        public static T GetUserSession<T>(HttpContextBase context, string session) where T : new()
        {
            return GetSession<T>(context, session);
        }

        private static void SetAuthCookie(HttpContextBase httpContext, FormsAuthenticationTicket authenticationTicket, string cookieName)
        {
            var encryptedTicket = FormsAuthentication.Encrypt(authenticationTicket);
            var cookie = new HttpCookie(cookieName, encryptedTicket)
            {
                HttpOnly = true,
                Expires = authenticationTicket.Expiration
            };
            //cookie.Domain = ".vinpro.net";
            httpContext.Response.Cookies.Add(cookie);
        }
        //public void SignOut(HttpContextBase httpContext)
        //{
        //    //Xóa signin cookies
        //    var cookie = new HttpCookie(this.signinToken);
        //    DateTime nowDateTime = DateTime.Now;
        //    cookie.Expires = nowDateTime.AddDays(-1);
        //    httpContext.Response.Cookies.Add(cookie);
        //}
    }
}
