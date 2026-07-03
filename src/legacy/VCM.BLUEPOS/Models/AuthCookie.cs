using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json;
using VCM.BLUEPOS.Account;
using VCM.BLUEPOS.Model.Account;
using VCM.BLUEPOS.Models.Account;


namespace VCM.BLUEPOS.Models
{
    public class AuthCookie
    {
        public static void UserLogin<T>(T user, HttpContextBase curentHttpContext, double hour, string signInTkn = "")
        {
            var loginToken = new FormsAuthenticationTicket(
                1,
                signInTkn == "" ? Constants.AppCookies : signInTkn,
                DateTime.Now,
                DateTime.Now.AddHours(hour), true, JsonConvert.SerializeObject(user));
            SetAuthCookie(curentHttpContext, loginToken, signInTkn == "" ? Constants.AppCookies : signInTkn);
        }
        public static bool CheckLogin(HttpContextBase curentHttpContext)
        {
            var loginTokenCookie = curentHttpContext.Request.Cookies[Constants.AppCookies];
            if (loginTokenCookie != null && !string.IsNullOrEmpty(loginTokenCookie.Value))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static void SetAuthCookie(HttpContextBase httpContext, FormsAuthenticationTicket authenticationTicket, string cookieName)
        {
            var domainName = httpContext.Request.Url.Host;
            var encryptedTicket = FormsAuthentication.Encrypt(authenticationTicket);
            var cookie = new HttpCookie(cookieName, encryptedTicket);
            cookie.HttpOnly = true;
            cookie.Expires = authenticationTicket.Expiration;
            cookie.Domain = domainName;
            httpContext.Response.Cookies.Add(cookie);
        }
        public static ADUserModel CurrentUser(HttpContextBase curentHttpContext)
        {
            var loginTokenCookie = curentHttpContext.Request.Cookies[Constants.AppCookies];
            if (loginTokenCookie != null)
            {
                try
                {
                    var token = FormsAuthentication.Decrypt(loginTokenCookie.Value);
                    return JsonConvert.DeserializeObject<ADUserModel>(token.UserData);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;
        }
        public static void SignOut(HttpContextBase httpContext)
        {
            RemoveCacheMenu();
            var cookie = new HttpCookie(Constants.AppCookies);
            DateTime nowDateTime = DateTime.Now;
            cookie.Expires = nowDateTime.AddDays(-1);
            httpContext.Response.Cookies.Add(cookie);
        }
        public static bool CheckCacheMenu()
        {
            try
            {
                var result = true;
                ObjectCache cache = MemoryCache.Default;
                var checkCache = cache.Get(Constants.CacheListMenu);
                if (checkCache == null)
                    result = false;
                return result;
            }
            catch
            {
                return false;
            }
        }
        public static void RemoveCacheMenu()
        {
            try
            {
                ObjectCache cache = MemoryCache.Default;
                var checkCache = cache.Get(Constants.CacheListMenu);
                if (checkCache != null)
                    cache.Remove(Constants.CacheListMenu);
            }
            catch
            {
            }
        }
        public static List<MenuModel> GetCacheMenu(string userName)
        {
            try
            {
                ObjectCache cache = MemoryCache.Default;
                var checkCache = cache.Get(Constants.CacheListMenu);
                if (checkCache != null)
                {
                    return (List<MenuModel>)checkCache;
                }
                else
                {
                    //AuthenBLO _authenBLO = new AuthenBLO();

                    AccountBLO _accountBLO = new AccountBLO();
                    CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
                    cacheItemPolicy.AbsoluteExpiration = DateTime.Now.AddHours(4);
                    //var listMenu = _authenBLO.LoadMenuByUser(userName);

                    var listMenu = _accountBLO.LoadMenuByUser(userName);
                    cache.Add(Constants.CacheListMenu, listMenu, cacheItemPolicy);
                }
            }
            catch
            {
            }
            return null;
        }

    }
}