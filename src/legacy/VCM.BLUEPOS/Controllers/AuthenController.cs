using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Web.Mvc;
using System.Net;
using PLG.Common;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Model.Authen;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Models.Login;
using VCM.BLUEPOS.Common;


namespace PLG.Controllers
{
    public class AuthenController : Controller
    {
        private readonly ILogin _loginDAL;
        private IAuthenBLO _authenBLO { get; set; }
        public AuthenController(ILogin data, IAuthenBLO authenBLO)
        {
            _loginDAL = data;
            _authenBLO = authenBLO;
        }
        public ActionResult Login()
        {
            return View();
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
                    var result = _authenBLO.LoginLocal(req);
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
                            listMenu = _authenBLO.LoadMenuByUser(req.UserName);
                            cache.Add(Constants.CacheListMenu, listMenu, cacheItemPolicy);
                        }
                        AuthCookie.UserLogin<ADUserModel>(result.Item2, this.HttpContext, 43200); // 30 day
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
                    var result = _loginDAL.LoginAD(req);
                    if (result.Item1.Equals(ADConnectionStatus.LoginADSuccess))
                    {
                        var checkUser = _authenBLO.CheckUser(req);
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
                                listMenu = _authenBLO.LoadMenuByUser(req.UserName);
                                cache.Add(Constants.CacheListMenu, listMenu, cacheItemPolicy);
                            }
                            AuthCookie.UserLogin<ADUserModel>(result.Item2, this.HttpContext, 43200); // 30 day
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

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Logout()
        {
            AuthCookie.SignOut(HttpContext);
            return RedirectToAction("Login","Account"); // 06/06/2025,tungnt8
            //return RedirectToAction("Login");
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
                    var result = _authenBLO.ChangePassWord(req);
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
                var typeLogin = _authenBLO.CheckTypeLogin(req); 
                if (typeLogin.Item1 == "LOCAL")
                {
                    var checkPassOld = _authenBLO.CheckPassWordOld(req);
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
                    _authenBLO.UpdateChangePassWord(req);
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