
using System;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Sockets;
using VCM.BLUEPOS.Helpers;
using Newtonsoft.Json;
using VCM.BLUEPOS.Common;

//using VCM.BLUEPOS.Model.Authen;

using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Account;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Business.Common;
//using VCM.BLUEPOS.Business.Authen;
using VCM.BLUEPOS.Common.Helpers;
using VCM.BLUEPOS.Models;
//using VCM.BLUEPOS.Models.Login;
using VCM.BLUEPOS.Models.Account;
using VCM.BLUEPOS.Model.Account;


namespace PLG.Controllers
{
    public class BaseController : Controller
    {
        //AuthenBLO autheBLO = new AuthenBLO();

        AccountBLO accountBLO = new AccountBLO();
        public ADUserModel LoginUser;

        public static List<ControllerDataModel> ListController { get; set; }

        //private readonly ILogin _loginBLO;
        private readonly IAccount _accountBLO;

        public BaseController()
        {
            this.LoginUser = new ADUserModel();
            ListController = GetListController();
            //autheBLO = new AuthenBLO();
            accountBLO = new AccountBLO();
        }

        //public BaseController(ILogin loginBLO)
        //{
        //    _loginBLO = loginBLO;
        //}

        public BaseController(IAccount accountBLO)
        {
            _accountBLO = accountBLO;
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //var host = Dns.GetHostEntry(Dns.GetHostName());
            //var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            //var ipServer = addr.LastOrDefault()?.ToString();

            //var browser = filterContext?.RequestContext.HttpContext.Request.Browser;
            //var browserPublicKey = browser.TagWriter.AssemblyQualifiedName;//System.Web.UI.HtmlTextWriter, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a

           // LogsFile.WriteLogFile(ipServer, browserPublicKey, "IPSrv:", ipServer);

            try
            {                
               // LogsFile.WriteLogFile(ipServer, browserPublicKey, "browser:", browser.Browser);
                var context = filterContext?.RequestContext.HttpContext;
                //var jsonContext = JsonConvert.SerializeObject(context);
                //LogsFile.WriteLogFile(ipServer, browserPublicKey, "context:", jsonContext);

                var method = context.Request?.HttpMethod.ToLower();
                //LogsFile.WriteLogFile(ipServer, browserPublicKey, "method:", method);


                var controller = context.Request?.RequestContext.RouteData.Values["controller"]?.ToString().ToLower();
                //LogsFile.WriteLogFile(ipServer, browserPublicKey, "controller:", controller);

                var action = context.Request?.RequestContext.RouteData.Values["action"]?.ToString().ToLower();
                //LogsFile.WriteLogFile(ipServer, browserPublicKey, "action:", action);

                var contextFilter = filterContext?.HttpContext;
                //var jsonContextFilter = JsonConvert.SerializeObject(contextFilter);

                //LogsFile.WriteLogFile(ipServer, browserPublicKey, "contextFilter:", jsonContextFilter);

                var checkLogin = AuthCookie.CheckLogin(contextFilter);
                
                //LogsFile.WriteLogFile(ipServer, browserPublicKey, "checkLogin:", "" + checkLogin);

                if (!checkLogin)
                {
                    var check = filterContext.RequestContext.HttpContext.Request.UrlReferrer;
                    var url = check == null ? string.Empty : filterContext.RequestContext.HttpContext.Request.UrlReferrer.AbsoluteUri;

                    if (url.IndexOf("?ref") > -1)
                    {
                        url = url.Substring(url.IndexOf("?ref"));
                        filterContext.Result = new RedirectResult(new UrlHelper(contextFilter.Request.RequestContext).RouteUrl("Login") + url);
                    }
                    else
                    {
                        filterContext.Result = new RedirectResult(new UrlHelper(contextFilter.Request.RequestContext).RouteUrl("Login") + "?ref=" + HttpUtility.UrlEncode(url));
                    }
                }
                else
                {
                    this.LoginUser = AuthCookie.CurrentUser(contextFilter);

                    //LogsFile.WriteLogFile(ipServer, browserPublicKey, "LoginUser:", "" + JsonConvert.SerializeObject(this.LoginUser));

                    if (this.LoginUser == null || string.IsNullOrEmpty(this.LoginUser.UserName))
                    {
                        filterContext.Result = new RedirectResult(new UrlHelper(contextFilter.Request.RequestContext).RouteUrl("Login"));
                    }                   
                    else
                    {
                        var actionAuthorize = GetListParentAuthorize().FirstOrDefault(d => d.Controller.ToLower() == controller && d.Action.ToLower() == action);
                        
                        //LogsFile.WriteLogFile(ipServer, browserPublicKey, "actionAuthorize:", "" + JsonConvert.SerializeObject(actionAuthorize));
                       
                        if (actionAuthorize != null)
                        {
                            //var listMenu = autheBLO.LoadMenuByUser(this.LoginUser.UserName);
                            //LogsFile.WriteLogFile(ipServer, browserPublicKey, $"listMenuByUser:{this.LoginUser.UserName}", "" + JsonConvert.SerializeObject(listMenu));

                            if (!string.IsNullOrEmpty(actionAuthorize.Controller) && !string.IsNullOrEmpty(actionAuthorize.Action))
                            {
                                //var checkUser = autheBLO.CheckUser(new LoginViewModel { UserName = this.LoginUser.UserName });

                                var checkUser = accountBLO.CheckUser(new LoginViewModel { UserName = this.LoginUser.UserName });

                                if (!checkUser.Item1)
                                {
                                    filterContext.Result = new RedirectResult(new UrlHelper(contextFilter.Request.RequestContext).RouteUrl("Login"));
                                }
                                else
                                {
                                    //var listMenu = autheBLO.LoadMenuByUser(this.LoginUser.UserName);
                                    var listMenu = accountBLO.LoadMenuByUser(this.LoginUser.UserName);
                                    MenuPermissionModel.ListMenuPermission = listMenu;

                                    if (controller != "home" && action != "index")
                                    {
                                        if (!listMenu.Any(d => d.Controller == actionAuthorize.Controller && d.Action == actionAuthorize.Action))
                                        {
                                            filterContext.Result = new RedirectResult(new UrlHelper(filterContext.HttpContext.Request.RequestContext).RouteUrl("Error"));
                                        }
                                    }

                                }
                            }
                           
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //LogsFile.WriteLogFile(ipServer, "System.Web.UI.HtmlTextWriter, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "Error:" + ipServer, $" Browser:{browser}" + JsonConvert.SerializeObject(ex));
            }
        }

        public static List<ControllerDataModel> GetListController()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var result = asm.GetTypes()
                    .Where(type => typeof(Controller).IsAssignableFrom(type))
                    .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
                    .Where(m => !m.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true).Any())
                    .Select(x => new ControllerDataModel
                    {
                        Controller = x.DeclaringType.Name.Replace("Controller", string.Empty),
                        ControllerDisplayName = x.DeclaringType.GetCustomAttributes().Where(a => a.GetType().Name == "DisplayName").Select(y => (y as DisplayName).Name).FirstOrDefault(),
                        ControllerAttributes = string.Join(",", x.DeclaringType.GetCustomAttributes().Select(a => a.GetType().Name.Replace("Attribute", ""))),
                        Action = x.Name,
                        ActionDisplayName = x.GetCustomAttributes().Where(a => a.GetType().Name == "DisplayName").Select(y => (y as DisplayName).Name).FirstOrDefault(),
                        ReturnType = x.ReturnType.Name,
                        ListParent = x.GetCustomAttributes().Where(a => a.GetType().Name == "ParentAuthorize").Select(y => (y as ParentAuthorize).Parents.FirstOrDefault()).ToList(),
                        Attributes = string.Join(",", x.GetCustomAttributes().Select(a => a.GetType().Name.Replace("Attribute", "")))
                    })
                    .OrderBy(x => x.Controller).ThenBy(x => x.Action).ToList();

                return result;
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach(var loadexcept in  ex.LoaderExceptions)
                {
                    string strError = loadexcept.ToString();
                }
                return null;
            }
        }
        public static List<ControllerDataModel> GetListParentAuthorize()
        {
            var data = ListController;
            data = data.Where(x => x.Attributes.Contains("DisplayName")).ToList();
            return data;
        }


    }
}