using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Net;
using PLG.Common;
using System.Configuration;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Helpers;
using VCM.BLUEPOS.Account;
using VCM.BLUEPOS.Business.ServiceLog;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Models.Account;
using VCM.BLUEPOS.Model.Account;
using VCM.BLUEPOS.Model.ServiceLog;


namespace PLG.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccount _accountDAL; 
        private readonly IAccountBLO _accountBLO;
        private readonly IServiceLogBLO _serviceLogDAL;

        //private LoginResponse responseAd { get; set; }
        //public object DefaultAuthenticationTypes { get; private set; }

        string SSODomain = ConfigurationManager.AppSettings["SSODomain"];
        string Domain = ConfigurationManager.AppSettings["Domain"];
        public AccountController(IAccount data, IAccountBLO accountBLO, IServiceLogBLO servicelog)
        {
            _accountDAL = data;
            _accountBLO = accountBLO;
            _serviceLogDAL = servicelog;
        }

        public ActionResult Login()
        {
            return View("~/Views/Account/Login.cshtml");
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel req)
        {
            var notify = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "OK",
                Data = null
            };

            try
            {
                if (req.LoginType == "LOCAL")
                {
                    var result = _accountBLO.LoginLocal(req);
                    if (result.Item1.Equals("OK"))
                    {
                        var listMenu = new List<MenuModel>();
                        ObjectCache cache = MemoryCache.Default;
                        if (cache.Contains(Constants.CacheListMenu))
                        {
                            listMenu = (List<MenuModel>)cache.Get(Constants.CacheListMenu);
                        }
                        else
                        {
                            CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
                            cacheItemPolicy.AbsoluteExpiration = DateTime.Now.AddHours(4);
                            listMenu = _accountBLO.LoadMenuByUser(req.UserName);
                            cache.Add(Constants.CacheListMenu, listMenu, cacheItemPolicy);
                        }

                        AuthCookie.UserLogin<VCM.BLUEPOS.Model.Account.ADUserModel>(result.Item2, this.HttpContext, 43200); // 30 day
                    }
                    else
                    {
                        notify = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = result.Item1                            
                        };
                    }
                }
                else // tài khoản AD
                {
                    var result = _accountDAL.LoginAD(req);

                    if (result.Item1.Equals(ADConnectionStatus.LoginADSuccess))
                    {
                        var checkUser = _accountBLO.CheckUser(req);
                        if (!checkUser.Item1)
                        {
                            notify = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = $"{checkUser.Item2}"
                            };
                        }
                        else
                        {
                            var listMenu = new List<MenuModel>();
                            ObjectCache cache = MemoryCache.Default;
                            if (cache.Contains(Constants.CacheListMenu))
                            {
                                listMenu = (List<MenuModel>)cache.Get(Constants.CacheListMenu);
                            }
                            else
                            {
                                CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
                                cacheItemPolicy.AbsoluteExpiration = DateTime.Now.AddHours(4);
                                listMenu = _accountBLO.LoadMenuByUser(req.UserName);
                                cache.Add(Constants.CacheListMenu, listMenu, cacheItemPolicy);
                            }
                            AuthCookie.UserLogin<VCM.BLUEPOS.Model.Account.ADUserModel>(result.Item2, this.HttpContext, 43200); // 30 day
                        }
                    }
                    else
                    {
                        notify = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Đăng nhập tài khoản AD {req.UserName} thất bại"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                notify = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi hệ thống {ex.Message}"
                };
            }
            return Json(notify);
        }

        // 03/06/2025,tungnt8
        public ActionResult LoginWithMS365()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("index", "Home");
            }
            var ssoLoginUrl = $"{SSODomain}/auth/sso/login?returnUrl={Domain}/Account/ADCallback";
            return Redirect(ssoLoginUrl);
        }

        public async Task<ActionResult> ADCallback(string token = "")
        {
            var modelLog = new StackTraceLogModel();
            var controller = Request.RequestContext.RouteData.Values["controller"].ToString().ToLower();
            var action = Request.RequestContext.RouteData.Values["action"].ToString().ToLower();

            try
            {
                var modelInfo = new VCM.BLUEPOS.Model.Account.ADUserModel();
                if (string.IsNullOrEmpty(token))
                {
                    modelLog = new StackTraceLogModel
                    {
                        UserName = base.User.ToString(),
                        Description = $"{SSODomain}/auth/sso/login?returnUrl={Domain}/Account/ADCallback",
                        Controller = controller,
                        Action = action,
                        ActionType = "Login",
                        JSONModel = string.Empty,
                        MessageError = $"{SSODomain}/auth/sso/login?returnUrl={Domain}/Account/ADCallback",
                        URL = base.Url.ToString(),
                        Source = "WEB"
                    };
                    _serviceLogDAL.WriteStackTraceLog(modelLog);
                    return RedirectToAction("Login", "Account");
                }
                var payload = DecodeJWTHelpers.GetJwtPayload(token);
                if (string.IsNullOrEmpty(payload))
                {
                    modelLog = new StackTraceLogModel
                    {
                        UserName = base.User.ToString(),
                        Description = $"{SSODomain}/auth/sso/login?returnUrl={Domain}/Account/ADCallback",
                        Controller = controller,
                        Action = action,
                        ActionType = "Login",
                        JSONModel = string.Empty,
                        MessageError = $"Token is null",
                        URL = base.Url.ToString(),
                        Source = "WEB"
                    };
                    _serviceLogDAL.WriteStackTraceLog(modelLog);
                    return RedirectToAction("Login", "Account");
                }

                var loginInfo = JsonConvert.DeserializeObject<AzureADModel>(payload);
                var userName = loginInfo.UserPrincipalName.Split('@')[0];
                var isSSO = false;
                if (userName != null)
                {
                    isSSO = true;
                }
                var checkUser = _accountBLO.CheckUserExistV2(userName);
                if (checkUser != null) // login SSO
                {
                    var Info = new VCM.BLUEPOS.Model.Account.ADUserModel
                    {
                        UserName = userName.ToLower(),
                        DisplayName = loginInfo.DisplayName,
                        FullName = loginInfo.UserPrincipalName,
                        Email = loginInfo.Mail,
                        Phone = loginInfo.MobilePhone,
                        GivenName = loginInfo.GivenName,
                        JobTitle = loginInfo.JobTitle,
                        OfficeLocation = loginInfo.OfficeLocation,
                        IdSSO = loginInfo.Id,
                        exp = loginInfo.exp,
                        iss = loginInfo.iss,
                        aud = loginInfo.aud,
                        IsSSO = isSSO
                    };
                    modelInfo = Info;
                }

                var modelJSON = new List<ADUserModel> { modelInfo };
                modelLog = new StackTraceLogModel
                {
                    UserName = modelInfo.UserName,
                    Description = $"{SSODomain}/auth/sso/login?returnUrl={Domain}/Account/ADCallback",
                    Controller = controller,
                    Action = action,
                    ActionType = "Login",
                    JSONModel = modelJSON == null ? "" : JsonConvert.SerializeObject(modelJSON),
                    MessageError = $"Login susscess",
                    URL = base.Url.ToString(),
                    Source = "WEB"
                };
                _serviceLogDAL.WriteStackTraceLog(modelLog);
                AuthCookie.UserLogin<VCM.BLUEPOS.Model.Account.ADUserModel>(modelInfo, this.HttpContext, 43200); // 30 day
            }
            catch (Exception ex)
            {
                modelLog = new StackTraceLogModel
                {
                    UserName = base.User.ToString(),
                    Description = $"{SSODomain}/auth/sso/login?returnUrl={Domain}/Account/ADCallback",
                    Controller = controller,
                    Action = action,
                    ActionType = "Login",
                    JSONModel = string.Empty,
                    MessageError = ex.Message,
                    URL = base.Url.ToString(),
                    Source = "WEB"
                };
                _serviceLogDAL.WriteStackTraceLog(modelLog);
                return RedirectToAction("Login", "Account");
            }
            return RedirectToAction("index", "Home");
        }

        [AllowAnonymous]
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Logout()
        {
            var LoginUser = AuthCookie.CurrentUser(this.HttpContext);
            if (LoginUser != null)
            {
                if (LoginUser.IsSSO == true)
                {
                    var ssoLogoutUrl = $"{SSODomain}/auth/sso/logout?postLogoutRedirectUri={Domain}/Account/ADLogoutCallback";
                    return Redirect(ssoLogoutUrl);
                }
            }
            AuthCookie.SignOut(this.HttpContext);
            return RedirectToAction("Login", "Account");
        }
        public ActionResult ADLogoutCallback()
        {
            AuthCookie.SignOut(this.HttpContext);
            return RedirectToAction("Login", "Account");
        }
        public ActionResult Error()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ChangePassWord(ChangePassWordModel req)
        {
            var notify = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "OK",
                Data = null
            };

            try
            {
                if (req.LoginType == "LOCAL")
                {
                    var result = _accountBLO.ChangePassWord(req);
                    if (result.Item1.Equals("OK"))
                    {
                        notify = new ResultResponse
                        {
                            Status = HttpStatusCode.OK,
                            Message = $"OK"
                        };
                    }
                    else
                    {
                        notify = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = result.Item1
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                notify = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi hệ thống {ex.Message}"
                };
            }
            return Json(notify);
        }

        public ActionResult UpdateChangePassWord(UpdateChangePassWordModel req)
        {
            var notify = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "OK",
                Data = null
            };

            try
            {
                var typeLogin = _accountBLO.CheckTypeLogin(req); 

                if (typeLogin.Item1 == "LOCAL")
                {
                    var checkPassOld = _accountBLO.CheckPassWordOld(req);
                    if (checkPassOld.Item1 == "FALSE")
                    {
                        notify = new ResultResponse
                        {
                            Status = HttpStatusCode.ExpectationFailed,
                            Message = "FALSE",
                            Data = null
                        };
                    }

                    if (string.IsNullOrEmpty(req.PassWordNew1))
                    {
                        notify = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Nhập mật khẩu mới"
                        };
                    }

                    if (string.IsNullOrEmpty(req.PassWordNew2))
                    {
                        notify = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Nhập lại mật khẩu mới"
                        };
                    }

                    if (req.PassWordNew2.Trim() != req.PassWordNew1.Trim())
                    {
                        notify = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Mật khẩu mới không đúng. Vui lòng nhập lại mật khẩu mới"
                        };
                    }
                    _accountBLO.UpdateChangePassWord(req);
                }
            }
            catch (Exception ex)
            {
                notify = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi hệ thống {ex.Message}"
                };
            }
            return Json(notify);
        }




    }
}