using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Threading.Tasks;
using PLG.Controllers;
using PLG.Common;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Store;
using VCM.BLUEPOS.Model.Authen;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Models.Login;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Role;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Model.Enums;
using VCM.BLUEPOS.Common;


namespace PLG.Controllers
{
    public class RoleController : BaseController
    {
        private IAuthenBLO _authenBLO { get; set; }
        private ILogin _loginDAL;
        private IStoreBLO _storeBLO;

        public RoleController(IAuthenBLO data, ILogin loginData, IStoreBLO storeBLO)
        {
            _authenBLO = data;
            _loginDAL = loginData;
            _storeBLO = storeBLO;
        }

        [DisplayName("Nhóm quyền")]
        public ActionResult RoleGroup()
        {
            var userName = base.LoginUser.UserName;
            ViewBag.ListMenu = _authenBLO.LoadMenu();
            return View();
        }

        [HttpPost]
        public ActionResult LoadDataRole()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var roleCode = Request?.Form["RoleCode"];
                var roleName = Request?.Form["RoleName"];
                var status = Request?.Form["Status"] == "-" ? "" : Request?.Form["Status"];

                var recordsTotal = 0;
                var result = _authenBLO.LoadDataRole(roleCode, roleName, status, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<RoleModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<RoleModel>());
            }
        }

        [HttpPost]
        public ActionResult LoadMenuRole(string RoleCode)
        {
            try
            {
                var createUserName = base.LoginUser.UserName;
                var result = _authenBLO.LoadMenuRole(RoleCode);
                return Json(result);
            }
            catch
            {
                return Json(new List<MenuRoleModel>());
            }
        }

        [HttpPost]
        public ActionResult UpdateRole(string RoleCode, string RoleName, string Status, string ListMenu)
        {
            try
            {
                var createUserName = base.LoginUser.UserName;
                var listIDMenu = new List<ListIDMenuRequest>();
                var listMenu = JsonConvert.DeserializeObject<List<MenuModel>>(ListMenu);

                var listMenuParent = listMenu.Where(d => d.ParentMenu != null).Select(a => a.ParentMenu).Distinct().ToList();
                var listMenuParentC1 = listMenu.Where(d => d.ParentMenuC1 != null).Select(a => a.ParentMenuC1).Distinct().ToList();

                if (listMenu.Count > 0)
                {
                    listIDMenu.AddRange(listMenu.Select(a => new ListIDMenuRequest { ID = a.ID }).ToList());//Add Menu C3 
                }

                if (listMenuParent.Count > 0)
                {
                    listIDMenu.AddRange(listMenuParent.Select(a => new ListIDMenuRequest { ID = a.Value }).ToList());//Add Menu C2 
                }

                if (listMenuParentC1.Count > 0)
                {
                    listIDMenu.AddRange(listMenuParentC1.Select(a => new ListIDMenuRequest { ID = a.Value }).ToList());//Add MenuParent C1
                }

                var listIDMenuDis = listIDMenu.Select(a => a.ID).Distinct().Select(d => new ListIDMenuRequest { ID = d }).ToList();
                var stt = Status == "1" ? true : false;
                RoleModel roleModel = new RoleModel
                {
                    RoleCode = RoleCode,
                    RoleName = RoleName,
                    Status = stt,
                    CreatedUser = createUserName,
                    CreatedDate = DateTime.Now,
                    LastupdateDate = DateTime.Now,
                    LastUpdateUser = createUserName
                };
                var result = _authenBLO.UpdateRole(roleModel, listIDMenuDis);
                return Json(result);
            }
            catch
            {
                return Json(false);
            }
        }

        [DisplayName("Phân quyền user")]
        [ParentAuthorize("PermUser")]
        public ActionResult PermUser()
        {
            ViewBag.ListRole = _authenBLO.LoadRole();
            ViewBag.ListStore = _storeBLO.ListStorePartner();
            return View();
        }

        [HttpPost]
        public ActionResult LoadDataPermission()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var userName = Request?.Form["UserName"];
                var storeNo = Request?.Form["StoreNo"];
                var status = Request?.Form["Status"] == "-" ? "" : Request?.Form["Status"];

                var typeLogin = Request?.Form["LoginType"] == "-" ? "" : Request?.Form["LoginType"];
                var roleCode = Request?.Form["RoleCode"] == "-" ? "" : Request?.Form["RoleCode"];

                var recordsTotal = 0;
                var result = _authenBLO.LoadUser(typeLogin, roleCode, userName, storeNo, status, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<AdminUserModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(default(List<AdminUserModel>));
            }
        }

        [HttpPost]
        public ActionResult LoadUserByUserName(string UserName)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = ""
            };

            try
            {
                var createUserName = base.LoginUser.UserName;
                var recordsTotal = 0;
                var data = _authenBLO.LoadUser("", "", UserName, "", "", out recordsTotal, 0, 1).FirstOrDefault();
                if (data != null)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = "Success",
                        Data = data
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = "Success",
                        Data = data
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
            }

            return Json(result);
        }

        // 30/06/2025
        [HttpPost]
        public ActionResult LoadUserByUserNameV2(string UserName)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = ""
            };

            try
            {
                var createUserName = base.LoginUser.UserName;
                var recordsTotal = 0;
                var data = _authenBLO.LoadUser("", "", UserName, "", "", out recordsTotal, 0, 1).FirstOrDefault();
                if (data != null)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = "Success",
                        Data = data
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = "Success",
                        Data = data
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
            }
            return Json(result);
        }

        [HttpPost]
        public async Task<ActionResult> UpdatePerm(string TypeLogin, string UserName, string RoleCode, string Status, string StoreNo, int IsAllStore)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };

            try
            {
                if (string.IsNullOrEmpty(UserName))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadGateway,
                        Message = $"Vui lòng nhập user đăng nhập",
                    };
                    return Json(result);
                }

                var listUser = UserName.Split(new char[] { ';', ',', '\n', '\r', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                string staffCode = "", fullName = "";
                var tasks = new List<Task>();
                var dataTask = new List<Tuple<bool, ADUserModel>>();
                var listUserSuccess = new List<AdminUserModel>();

                var createUserName = base.LoginUser.UserName;
                var allStore = IsAllStore == 1 ? true : false;
                var _listStore = allStore == true ? string.Empty : StoreNo;

                if (TypeLogin == "AD")
                {
                    foreach (var item in listUser)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            dataTask.Add(_loginDAL.CheckADUser(item));
                        }));
                    }

                    Task taskW = Task.WhenAll(tasks.ToArray());
                    await taskW;

                    var checkUser = dataTask.Where(d => d.Item1 == false).ToList();
                    if (checkUser.Count > 0)
                    {
                        var listNotExitsUser = checkUser.Where(d => !string.IsNullOrEmpty(d.Item2.UserName)).Select(a => a.Item2.UserName).ToList();
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadGateway,
                            Message = $"Tài khoản ({string.Join(",", listNotExitsUser)}) không tồn tại trong hệ thống AD",

                        };
                        return Json(result);
                    }

                    var userIncorect = dataTask.Select(item => new AdminUserModel
                    {
                        TypeLogin = TypeLogin,
                        UserName = item.Item2.UserName,
                        StaffCode = string.IsNullOrEmpty(item.Item2.StaffCode) ? item.Item2.UserName : item.Item2.StaffCode,
                        FullName = string.IsNullOrEmpty(item.Item2.FullName) ? item.Item2.FullNameDep : item.Item2.FullName,
                        RoleCode = RoleCode,
                        IsActive = Status == "1" ? true : false,
                        StoreNo = _listStore,
                        IsAllStore = allStore,
                        CreatedBy = createUserName,
                        CreatedDate = DateTime.Now,
                        UpdatedBy = createUserName,
                        UpdatedDate = DateTime.Now
                    }).ToList();

                    listUserSuccess.AddRange(userIncorect);
                }
                else // LOCAL
                {
                    var checkUser = _authenBLO.GetStaff(listUser);
                    if (checkUser.Item1 == false)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Không tồn tại user {checkUser.Item2} trong hệ thống",
                        };
                        return Json(result);
                    }

                    var userIncorect = checkUser.Item3.Select(item => new AdminUserModel
                    {
                        TypeLogin = TypeLogin,
                        UserName = item.ID,
                        StaffCode = item.ID,
                        FullName = item.FirstName,
                        RoleCode = RoleCode,
                        IsActive = Status == "1" ? true : false,
                        StoreNo = _listStore,
                        IsAllStore = allStore,
                        CreatedBy = createUserName,
                        CreatedDate = DateTime.Now,
                        UpdatedBy = createUserName,
                        UpdatedDate = DateTime.Now
                    }).ToList();

                    listUserSuccess.AddRange(userIncorect);
                }
                if (listUserSuccess.Count == 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy username",
                    };
                    return Json(result);
                }

                var response = _authenBLO.UpdatePermUser(listUserSuccess);

                if (response.Item1 == false)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                };
                return Json(result);
            }
        }

        // 30/06/2025,tungnt8
        [HttpPost]
        public async Task<ActionResult> UpdatePermV2(CreateUserStoreModel req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            try
            {

                if (string.IsNullOrEmpty(req.UserName))
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.warning,
                        Message = "Vui lòng nhập tài khoản"
                    });
                }

                if (req.IsActive == null)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.warning,
                        Message = "Vui lòng chọn trạng thái"
                    });
                }

                //----------------

                var listUser = req.UserName.Split(new char[] { ';', ',','\n', '\r', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();                
                var tasks = new List<Task>();
                var dataTask = new List<Tuple<bool, ADUserModel>>();
                var listUserSuccess = new List<CreateUserStoreModel>();

                //----------------

                if (req.TypeLogin == "AD")
                {
                    foreach (var item in listUser)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            dataTask.Add(_loginDAL.CheckADUser(item));
                        }));
                    }

                    Task taskW = Task.WhenAll(tasks.ToArray());
                    await taskW;
                    var checkUser = dataTask.Where(d => d.Item1 == false).ToList();
                    if (checkUser.Count > 0)
                    {
                        var listNotExitsUser = checkUser.Where(d => !string.IsNullOrEmpty(d.Item2.UserName)).Select(a => a.Item2.UserName).ToList();
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadGateway,
                            Message = $"Tài khoản ({string.Join(",", listNotExitsUser)}) không tồn tại trong hệ thống AD",
                        };
                        return Json(result);
                    }

                    var userIncorect = dataTask.Select(item => new CreateUserStoreModel
                    {
                        TypeLogin = req.TypeLogin,
                        StaffCode = string.IsNullOrEmpty(item.Item2.StaffCode) ? item.Item2.UserName : item.Item2.StaffCode,
                        FullName = string.IsNullOrEmpty(item.Item2.FullName) ? item.Item2.FullNameDep : item.Item2.FullName,
                        RoleCode = req.RoleCode,
                        UserName = req.TypeLogin == "LOCAL" ? item.Item2.StaffCode : item.Item2.UserName,
                        StoreNo = req.StoreNo,
                        IsAutoImportStore = req.IsAutoImportStore,
                        MutilStoreAuto = req.MutilStoreAuto,
                        IsAllStore = req.IsAllStore == "1" ? "1" : "0",
                        IsActive = req.IsActive,
                        CreatedDate = DateTime.Now,
                        CreatedBy = LoginUser.UserName
                    }).ToList();
                    listUserSuccess.AddRange(userIncorect);
                }
                else // Local
                {
                    var checkUser = _authenBLO.GetStaff(listUser);
                    if (checkUser.Item1 == false)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Không tồn tại user {checkUser.Item2} này trong hệ thống. Vui lòng kiểm tra lại",
                        };
                        return Json(result);
                    }

                    var userIncorect = checkUser.Item3.Select(item => new CreateUserStoreModel
                    {
                        TypeLogin = req.TypeLogin,
                        UserName = item.ID,
                        StaffCode = item.ID,
                        FullName = item.FirstName,
                        RoleCode = req.RoleCode,
                        StoreNo = req.StoreNo,
                        IsAutoImportStore = req.IsAutoImportStore,
                        MutilStoreAuto = req.MutilStoreAuto,
                        IsActive = req.IsActive,
                        IsAllStore = req.IsAllStore == "1" ? "1" : "0",
                        CreatedBy = LoginUser.UserName,
                        CreatedDate = DateTime.Now,
                        UpdatedBy = LoginUser.UserName,
                        UpdatedDate = DateTime.Now
                    }).ToList();
                    listUserSuccess.AddRange(userIncorect);
                }

                if (listUserSuccess.Count == 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy thông tin",
                    };
                    return Json(result);
                }

                if (req.IsAllStore != "1")
                {
                    if (!req.IsAutoImportStore)
                    {
                        if (req.StoreNo == null || req.StoreNo == "")
                        {
                            return Json(new ResultResponseModel
                            {
                                Status = ResultEnum.warning,
                                Message = "Vui lòng chọn cửa hàng"
                            });
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(req.MutilStoreAuto))
                        {
                            return Json(new ResultResponseModel
                            {
                                Status = ResultEnum.warning,
                                Message = "Vui lòng nhập cửa hàng"
                            });
                        }
                    }
                }
                var data = await _authenBLO.CreateUserStoreAsync(listUserSuccess);
                return Json(data);
           }
           catch (Exception ex)
           {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                };
                return Json(result);
            }           
        }

        [HttpPost]
        public async Task<JsonResult> UpdateUserStoreV2(UpdateUserStoreModel req)
        {
            if (req.IsAutoImportStore == false)
            {
                if (string.IsNullOrEmpty(req.StoreNo))
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.warning,
                        Message = "Vui lòng chọn cửa hàng"
                    });
                }
            }
            else
            {
                if (string.IsNullOrEmpty(req.MutilStoreAuto))
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.warning, // = 2
                        Message = "Vui lòng nhập danh sách cửa hàng"
                    });
                }
            }
            req.UpdatedBy = LoginUser.UserName;
            var data = await _authenBLO.UpdateUserStoreAsyncV2(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult GetStoreByUser(ViewStoreByUserModel req)
        {
            var data = _authenBLO.GetStoreByUser(req);
            return Json(data);
        }

        [HttpPost]
        public ActionResult ViewPass(string UserName)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = ""
            };
            try
            {
                var recordsTotal = 0;
                var data = _authenBLO.LoadUser("", "", UserName, "", "", out recordsTotal, 0, 1).FirstOrDefault();
                var pass = HashMD5.Decrypt(data.Password, true);
                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = "Success",
                    Data = pass
                };
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = ""
                };
            }

            return Json(result);
        }

        [HttpPost]
        public ActionResult _PermStore(string userName)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK
            };
            try
            {
                var model = new PermStoreRequest
                {
                    UserName = userName
                };

                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $"Success",
                    ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/Role/_PermStore.cshtml", model),
                    Data = model
                };
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.ServiceUnavailable,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult PermStoreLoad()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var userName = Request?.Form["UserName"];
                var storeNo = Request?.Form["StoreNo"];
                var storeName = Request?.Form["StoreName"];

                var recordsTotal = 0;
                var result = _authenBLO.GetSiteNoByStaffCode(userName, storeNo, storeName, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<ParamJsonStoreList> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result.Item3 });
            }
            catch
            {
                return Json(default(List<ParamJsonStoreList>));
            }
        }

        [DisplayName("Quản lý menu")]
        [ParentAuthorize("MngMenu")]
        public ActionResult MngMenu()
        {
            var data = _authenBLO.LoadAllMenu("", "", "").ToList();
            ViewBag.ListMenuCombo = data;
            return View();
        }

        [HttpGet]
        public ActionResult _ListMenu(string menuName, string controllerMenu, string actionMenu)
        {
            var data = _authenBLO.LoadAllMenu(menuName, controllerMenu, actionMenu);
            return PartialView(data);
        }

        [HttpPost]
        public ActionResult UpdateMenu(MenuModel model)
        {
            model.CreatedDate = DateTime.Now;
            model.CreatedUser = base.LoginUser.UserName;
            model.LastupdateDate = DateTime.Now;
            model.LastUpdateUser = base.LoginUser.UserName;

            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                var response = _authenBLO.UpdateMenu(model);
                if (!response.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Thêm menu {model.MenuName} thất bại",
                        Data = null
                    };
                    return Json(result);
                }
                result.Data = response.Item3;

                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
                return Json(result);
            }
        }

        [HttpPost]
        public ActionResult GetMenu(int ID)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };

            try
            {
                var response = _authenBLO.GetMenuByID(ID);
                result.Data = response;
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
                return Json(result);
            }
        }

        [HttpPost]
        public ActionResult DeleteMenu(int typeMenu, int ID)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,  // OK
                Message = "",
                Data = null
            };

            try
            {
                var response = _authenBLO.DeleteMenu(typeMenu, ID);
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
                return Json(result);
            }
        }

        // ----------- PHAN QUYEN USER POS WEB -----------

        //[DisplayName("Phân quyền user POSWeb")]
        //[ParentAuthorize("PermUserPOSWeb")]
        //public ActionResult PermUserPOSWeb()
        //{
        //    ViewBag.ListRole = _authenBLO.LoadRole();
        //    ViewBag.ListStore = _storeBLO.ListStorePartner();
        //    return View();
        //}

        //[HttpPost]
        //public ActionResult GetPermissionListByPosWeb()

        //{
        //    try
        //    {
        //        var draw = Request?.Form["draw"];
        //        var start = Request?.Form["start"];
        //        var length = Request?.Form["length"];
        //        var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
        //        var sortColumnDirection = Request?.Form["order[0][dir]"];
        //        var searchValue = Request?.Form["search[value]"];
        //        var pageSize = length != null ? Convert.ToInt32(length) : 0;
        //        var skip = start != null ? Convert.ToInt32(start) : 0;

        //        var userName = Request?.Form["UserName"];
        //        var storeNo = Request?.Form["StoreNo"];
        //        var status = Request?.Form["Status"] == "-" ? "" : Request?.Form["Status"];
        //        var typeLogin = Request?.Form["LoginType"] == "-" ? "" : Request?.Form["LoginType"];
        //        var recordsTotal = 0;
        //        var result = _authenBLO.LoadUserByPOSWeb(typeLogin, userName, storeNo, status, out recordsTotal, skip, pageSize);
        //        return Json(new DataTablesViewModel<PosWebAdminUserModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

        //    }
        //    catch
        //    {
        //        return View(default(List<PosWebAdminUserModel>));
        //    }
        //}

        //[HttpPost]
        //public async Task<ActionResult> CreatePermUserByPOSWeb(string TypeLogin, string UserName, string RoleCode, string Status, string StoreNo, string PosTerminal, string IsMobile)
        //{
        //    var result = new ResultResponse
        //    {
        //        Status = HttpStatusCode.OK,
        //        Message = "",
        //        Data = null
        //    };

        //    try
        //    {
        //        if (string.IsNullOrEmpty(UserName))
        //        {
        //            result = new ResultResponse
        //            {
        //                Status = HttpStatusCode.BadGateway,
        //                Message = $"Vui lòng nhập user đăng nhập",
        //            };
        //            return Json(result);
        //        }

        //        if (string.IsNullOrEmpty(PosTerminal))
        //        {
        //            result = new ResultResponse
        //            {
        //                Status = HttpStatusCode.BadGateway,
        //                Message = $"Vui lòng chọn máy POS bán hàng",
        //            };
        //            return Json(result);
        //        }

        //        var listUser = UserName.Split(new char[] { ';', ',', '\n', '\r', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
        //        var tasks = new List<Task>();
        //        var dataTask = new List<Tuple<bool, ADUserModel>>();
        //        var listUserSuccess = new List<AdminUserModel>();
        //        var createUserName = base.LoginUser.UserName;
        //        var storeNoSales = StoreNo.Split('"')[0] + StoreNo.Split('"')[1];
        //        var storeNo = StoreNo.Split('"')[0] + StoreNo.Split('"')[1];

        //        if (TypeLogin == "AD")
        //        {
        //            foreach (var item in listUser)
        //            {
        //                tasks.Add(Task.Run(() =>
        //                {
        //                    dataTask.Add(_loginDAL.CheckADUser(item));
        //                }));
        //            }
        //            Task taskW = Task.WhenAll(tasks.ToArray());
        //            await taskW;
        //            var checkUser = dataTask.Where(d => d.Item1 == false).ToList();

        //            if (checkUser.Count > 0)
        //            {
        //                var listNotExitsUser = checkUser.Where(d => !string.IsNullOrEmpty(d.Item2.UserName)).Select(a => a.Item2.UserName).ToList();
        //                result = new ResultResponse
        //                {
        //                    Status = HttpStatusCode.BadGateway,
        //                    Message = $"Tài khoản AD : ({string.Join(",", listNotExitsUser)}) này không tồn tại trong hệ thống",
        //                };
        //                return Json(result);
        //            }

        //            var userIncorect = dataTask.Select(item => new AdminUserModel
        //            {
        //                TypeLogin = TypeLogin,
        //                UserName = item.Item2.UserName,
        //                StaffCode = string.IsNullOrEmpty(item.Item2.StaffCode) ? item.Item2.UserName : item.Item2.StaffCode,
        //                FullName = string.IsNullOrEmpty(item.Item2.FullName) ? item.Item2.FullNameDep : item.Item2.FullName,
        //                IsActive = Status == "1" ? true : false,
        //                IsMobile = IsMobile == "1" ? true : false,
        //                RoleCode = RoleCode,
        //                StoreNo = $"{'['}{'"'}{storeNo}{'"'}{']'}",
        //                PosTerminal = PosTerminal,
        //                StoreSales = $"{'['}{'"'}{storeNoSales}{':'}{PosTerminal.Trim()}{'"'}{']'}",
        //                CreatedBy = createUserName,
        //                CreatedDate = DateTime.Now,
        //                UpdatedBy = createUserName,
        //                UpdatedDate = DateTime.Now
        //            }).ToList();

        //            listUserSuccess.AddRange(userIncorect);
        //        }
        //        else   // Tai khoan LOCAL
        //        {
        //            var checkUser = _authenBLO.GetStaff(listUser);

        //            if (checkUser.Item1 == false)
        //            {
        //                result = new ResultResponse
        //                {
        //                    Status = HttpStatusCode.BadRequest,
        //                    Message = $"Không tồn tại user {checkUser.Item2} này trong hệ thống",
        //                };
        //                return Json(result);
        //            }

        //            var userIncorect = checkUser.Item3.Select(item => new AdminUserModel
        //            {
        //                TypeLogin = TypeLogin,     // = LOCAL
        //                UserName  = item.ID,       // = mã nhân viên
        //                StaffCode = item.ID,
        //                FullName  = item.FirstName,
        //                Password  = item.Password,
        //                IsActive  = Status == "1" ? true : false,
        //                IsMobile = IsMobile == "1" ? true : false,
        //                RoleCode  = RoleCode,
        //                StoreNo   = $"{'['}{'"'}{storeNo}{'"'}{']'}",
        //                PosTerminal = PosTerminal,
        //                StoreSales  = $"{'['}{'"'}{storeNoSales}{':'}{PosTerminal.Trim()}{'"'}{']'}",
        //                CreatedBy   = createUserName,
        //                CreatedDate = DateTime.Now,
        //                UpdatedBy   = createUserName,
        //                UpdatedDate = DateTime.Now
        //            }).ToList();

        //            listUserSuccess.AddRange(userIncorect);
        //        }
        //        if (listUserSuccess.Count == 0)
        //        {
        //            result = new ResultResponse
        //            {
        //                Status = HttpStatusCode.BadRequest,
        //                Message = $"Không tìm thấy username",
        //            };
        //            return Json(result);
        //        }

        //        var response = _authenBLO.CreatePermUserByPOSWeb(listUserSuccess); // DB = PLHWebMobile
        //        if (response.Item1 == false)
        //        {
        //            result = new ResultResponse
        //            {
        //                Status = HttpStatusCode.BadRequest,
        //                Message = $"{response.Item2}",
        //            };
        //            return Json(result);
        //        }
        //        else
        //        {
        //            result = new ResultResponse
        //            {
        //                Status = HttpStatusCode.OK,
        //                Message = $"{response.Item2}",
        //            };
        //            return Json(result);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        result = new ResultResponse
        //        {
        //            Status = HttpStatusCode.InternalServerError,
        //            Message = $"Lỗi {ex.Message}",
        //        };
        //        return Json(result);
        //    }
        //}

        //[HttpPost]
        //public ActionResult UpdateUserAdminByPOSWeb(string TypeLogin, string UserName, string Status, string IsMobile)
        //{
        //    var result = new ResultResponse
        //    {
        //        Status = HttpStatusCode.OK,
        //        Message = ""
        //    };

        //    try
        //    {
        //        var UpdateBy = base.LoginUser.UserName;
        //        var data = _authenBLO.UpdatePermUserAdminByPOSWeb(TypeLogin, UserName, Status, UpdateBy, IsMobile);

        //        if (data != null)
        //        {
        //            result = new ResultResponse
        //            {
        //                Status = HttpStatusCode.OK,
        //                Message = "Cập nhật thành công",
        //                Data = data
        //            };
        //        }
        //        else
        //        {
        //            result = new ResultResponse
        //            {
        //                Status = HttpStatusCode.BadRequest,
        //                Message = "Có lỗi xảy ra",
        //                Data = data
        //            };
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        result = new ResultResponse
        //        {
        //            Status = HttpStatusCode.InternalServerError,
        //            Message = $"Lỗi {ex.Message}",
        //            Data = null
        //        };
        //    }
        //    return Json(result);
        //}

        //[HttpPost]
        //public JsonResult DeleteUserAdminByPOSWeb(DeleteUserAdminByPosWebModel req)
        //{
        //    var data = _authenBLO.DeleteUserAdminByPOSWeb(req);
        //    return Json(data);
        //}













    }
}