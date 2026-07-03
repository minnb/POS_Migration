using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Authen;
using VCM.BLUEPOS.Model.Role;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Model.Employee;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Data.EF.Central;



namespace VCM.BLUEPOS.Data.Authen
{
    public interface IAuthenData
    {
        Tuple<string, ADUserModel> LoginLocal(LoginViewModel req);
        Tuple<bool, string> CheckUser(LoginViewModel req);
        SupportInforModel GetSupportInfor();
        List<MenuModel> LoadMenu();
        List<MenuModel> LoadMenuByUser(string userName);
        List<RoleModel> LoadDataRole(string roleCode, string roleName, string status, out int total, int skip, int take);
        List<MenuRoleModel> LoadMenuRole(string roleCode);
        bool UpdateRole(RoleModel roleModel, List<ListIDMenuRequest> lstMenu);
        List<AdminUserModel> LoadUser(string typeLogin, string roleCode, string userName, string storeNo, string status, out int total, int skip, int take);
        List<RoleModel> LoadRole();
        Tuple<bool, string> UpdatePermUser(List<AdminUserModel> listModel);
        List<StoreModel> GetSiteNoByUser(string userName);
        List<MenuModel> LoadAllMenu(string menuName, string controller, string action);
        Tuple<bool, string, MenuModel> UpdateMenu(MenuModel model);
        Tuple<bool, string> DeleteMenu(int typeMenu, int ID);
        MenuModel GetMenuByID(int ID);
        UserRoleModel GetRoleByUser(string userName);
        Tuple<bool, string, List<StaffModel>> GetStaff(List<string> listStaffCode);
        Tuple<bool, string, List<ParamJsonStoreList>> GetSiteNoByStaffCode(string userName, string storeNo, string storeName, out int total, int skip, int take);

        // ------- PHAN QUYEN USER POS WEB --------

        //List<PosWebAdminUserModel> LoadUserByPOSWeb(string typeLogin, string userName, string storeNo, string status, out int total, int skip, int take);
        //Tuple<bool, string> CreatePermUserByPOSWeb(List<AdminUserModel> listModel);
        //ResultResponseModel UpdatePermUserAdminByPOSWeb(string typeLogin, string userName, string status, string UpdateBy, string isMobile);
        //ResultResponseModel DeleteUserAdminByPOSWeb(DeleteUserAdminByPosWebModel req);

        //------------
        Tuple<string, ADUserModel> CheckPassWordOld(UpdateChangePassWordModel req);
        Tuple<string, ADUserModel> ChangePassWord(ChangePassWordModel req);
        Tuple<string, ADUserModel> UpdateChangePassWord(UpdateChangePassWordModel req);
        Tuple<string, ADUserModel> CheckTypeLogin(UpdateChangePassWordModel req);

        //30/06/2025,tungt8
        Task<ResultResponseModel> CreateUserStoreAsync(List<CreateUserStoreModel> req);
        Task<ResultResponseModel> UpdateUserStoreAsyncV2(UpdateUserStoreModel req);
        ResultResponseModel GetStoreByUser(ViewStoreByUserModel req);

    }

    public class AuthenData : IAuthenData
    {
        public Tuple<string, ADUserModel> LoginLocal(LoginViewModel req)
        {
            var result = new ADUserModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = (from a in db.AdminUsers
                              join b in db.Staffs on a.UserName equals b.ID
                              where a.UserName == req.UserName && a.TypeLogin == req.LoginType
                              select new ADUserModel
                              {
                                  StaffCode = a.StaffCode,
                                  Department = "",
                                  FullName = a.FullName,
                                  Role = a.RoleCode,
                                  Email = string.Empty,
                                  MasterCode = string.Empty,
                                  Phone = string.Empty,
                                  Title = string.Empty,
                                  UserName = req.UserName,
                                  SiteCode = b.StoreNo,
                                  PasswordLocal = b.Password,
                                  IsActive = a.IsActive ?? false,
                                  TypeLogin = a.TypeLogin
                              }).FirstOrDefault();

                    if (result == null)
                        return new Tuple<string, ADUserModel>($"Tài khoản {req.UserName} không tồn tại (hoặc chưa được cấp quyền) truy cập trong hệ thống. Vui lòng kiểm tra lại", result);

                    if (!result.IsActive)
                        return new Tuple<string, ADUserModel>($"Tài khoản {req.UserName} đang bị khóa, Vui lòng liên hệ IT", result);

                    if (result.PasswordLocal != req.PassWord)
                        return new Tuple<string, ADUserModel>($"Đăng nhập mật khẩu không đúng", result);

                    return new Tuple<string, ADUserModel>("OK", result);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<string, ADUserModel>(ex.Message, result);
            }
        }
        public Tuple<bool, string> CheckUser(LoginViewModel req)
        {
            var result = new Tuple<bool, string>(true, "Success");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkData = db.AdminUsers.Any(d => d.UserName == req.UserName);

                    if (!checkData)
                        result = new Tuple<bool, string>(false, $"Không tồn tại tài khoản {req.UserName} này trong hệ thống. Vui lòng kiểm tra lại");

                    var checkIsActive = db.AdminUsers.FirstOrDefault(d => d.UserName == req.UserName);

                    if (checkIsActive.IsActive == false)
                        return new Tuple<bool, string>(false, $"Tài khoản {req.UserName} này đang bị khóa, Vui lòng liên hệ bộ phận CNTT");
                    return new Tuple<bool,string>(true, "");

                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, $"{ex.Message}");
            }
            return result;
        }
        public List<MenuModel> LoadMenu()
        {
            var result = new List<MenuModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.Menus.Where(d => d.Status == true).Select(a => new MenuModel
                    {
                        ID = a.ID,
                        MenuName = a.MenuName,
                        Controller = a.Controller,
                        Action = a.Action,
                        Icon = a.Icon,
                        ParentMenu = a.ParentMenu,
                        Orderby = a.Orderby
                    }).ToList();
                }
                catch
                {
                }
                return result;
            }
        }
        public List<MenuModel> LoadMenuByUser(string userName)
        {
            var result = new List<MenuModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = (from a in db.Menus
                              join b in db.MenuRoles on a.ID equals b.MenuID
                              join c in db.AdminUsers on b.RoleCode equals c.RoleCode
                              where c.UserName == userName && a.Status == true
                              select new MenuModel
                              {
                                  ID = a.ID,
                                  MenuName = a.MenuName,
                                  Controller = a.Controller,
                                  Action = a.Action,
                                  Icon = a.Icon,
                                  ParentMenu = a.ParentMenu,
                                  Orderby = a.Orderby
                              }).ToList();
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }
        public List<RoleModel> LoadDataRole(string roleCode, string roleName, string status, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var data = db.Roles.Select(a => new RoleModel
                    {
                        ID = a.ID,
                        RoleCode = a.RoleCode,
                        RoleName = a.RoleName,
                        Status = a.Status,
                        CreatedDate = a.CreatedDate,
                        CreatedUser = a.CreatedUser,
                        LastupdateDate = a.LastupdateDate,
                        LastUpdateUser = a.LastUpdateUser
                    });

                    if (!string.IsNullOrEmpty(roleCode))
                    {
                        data = data.Where(m => m.RoleCode.ToLower().Contains(roleCode.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(roleName))
                    {
                        data = data.Where(m => m.RoleName.ToLower().Contains(roleName.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(status))
                    {
                        var enable = status == "1" ? true : false;
                        data = data.Where(m => m.Status == enable);
                    }

                    total = data.Count();
                    var result = data.OrderByDescending(d => d.CreatedDate).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }
                return new List<RoleModel>();
            }
        }
        public List<MenuRoleModel> LoadMenuRole(string roleCode)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var data = db.MenuRoles.Where(a => a.RoleCode == roleCode)
                                .Select(a => new MenuRoleModel
                                {
                                    ID = a.ID,
                                    MenuID = a.MenuID,
                                    RoleCode = a.RoleCode,
                                    Status = a.Status,
                                    CreatedDate = a.CreatedDate
                                });

                    var result = data.OrderByDescending(d => d.CreatedDate).ToList();
                    return result;
                }
                catch
                {
                }
                return new List<MenuRoleModel>();
            }
        }
        public bool UpdateRole(RoleModel roleModel, List<ListIDMenuRequest> lstMenu)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var dateTime = DateTime.Now;
                    var checkRole = db.Roles.FirstOrDefault(d => d.RoleCode == roleModel.RoleCode);
                    if (checkRole != null)
                    {
                        checkRole.RoleName = roleModel.RoleName;
                        checkRole.Status = roleModel.Status;
                        checkRole.LastupdateDate = dateTime;
                        checkRole.LastUpdateUser = roleModel.LastUpdateUser;
                        db.SaveChanges();

                        if (roleModel.Status == true)
                        {
                            var itemDel = db.MenuRoles.Where(d => d.RoleCode == checkRole.RoleCode);
                            if (itemDel != null)
                            {
                                db.MenuRoles.RemoveRange(itemDel);
                            }
                            foreach (var itemMenu in lstMenu)
                            {
                                var itemInsert = new MenuRole
                                {
                                    MenuID = itemMenu.ID,
                                    RoleCode = checkRole.RoleCode,
                                    Status = true,
                                    CreatedDate = dateTime,
                                    CreatedUser = checkRole.CreatedUser,
                                    LastupdateDate = dateTime,
                                    LastUpdateUser = checkRole.LastUpdateUser
                                };
                                db.MenuRoles.Add(itemInsert);
                            }
                            db.SaveChanges();
                        }
                        return true;
                    }
                    else
                    {
                        var codeSave = "";
                        var checkCode = db.Roles.OrderByDescending(d => d.CreatedDate).FirstOrDefault()?.RoleCode;
                        if (string.IsNullOrEmpty(checkCode))
                        {
                            codeSave = "R0001";
                        }
                        else
                        {
                            var listData = db.Roles.Select(d => d.RoleCode.Substring(1, d.RoleCode.Length - 1)).ToList();
                            var maxCode = listData.Select(int.Parse).ToList().Max();

                            if (Convert.ToInt32(maxCode + 1) < 10)
                                codeSave = "R000" + (Convert.ToInt32(maxCode) + 1).ToString();
                            else if (Convert.ToInt32(maxCode + 1) >= 10 && Convert.ToInt32(maxCode + 1) < 100)
                                codeSave = "R00" + (Convert.ToInt32(maxCode) + 1).ToString();
                            else
                                codeSave = "R" + (Convert.ToInt32(maxCode) + 1).ToString();
                        }
                        var item = new Role
                        {
                            RoleCode = codeSave,
                            RoleName = roleModel.RoleName,
                            Status = roleModel.Status,
                            CreatedUser = roleModel.CreatedUser,
                            CreatedDate = dateTime
                        };

                        db.Roles.Add(item);
                        db.SaveChanges();

                        foreach (var itemMenu in lstMenu)
                        {
                            var itemInsert = new MenuRole
                            {
                                MenuID = itemMenu.ID,
                                RoleCode = item.RoleCode,
                                Status = true,
                                CreatedDate = dateTime,
                                CreatedUser = item.CreatedUser
                            };
                            db.MenuRoles.Add(itemInsert);
                        }
                        db.SaveChanges();
                        return true;
                    }
                }
                catch
                {
                    return false;
                }

            }
        }
        public List<AdminUserModel> LoadUser(string typeLogin, string roleCode, string userName,string storeNo, string status, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var data = db.AdminUsers
                                .Select(a => new AdminUserModel
                                {
                                    ID = a.ID,
                                    TypeLogin = a.TypeLogin,
                                    UserName = a.UserName,
                                    Password = a.Password,
                                    RoleCode = a.RoleCode,
                                    RoleName = db.Roles.FirstOrDefault(c => c.RoleCode == a.RoleCode).RoleName,
                                    StaffCode = a.StaffCode,
                                    FullName = a.FullName,
                                    StoreNo = a.StoreNo,
                                    IsAllStore = a.IsAllStore,
                                    IsActive = a.IsActive,
                                    CreatedBy = a.CreatedBy,
                                    CreatedDate = a.CreatedDate,
                                    UpdatedBy = a.UpdatedBy,
                                    UpdatedDate = a.UpdatedDate
                                });

                    if (!string.IsNullOrEmpty(typeLogin))
                    {
                        data = data.Where(m => m.TypeLogin == typeLogin);
                    }

                    if (!string.IsNullOrEmpty(userName))
                    {
                        data = data.Where(m => m.UserName.ToLower().Contains(userName.ToLower()) || m.StaffCode.ToLower().Contains(userName.ToLower()) || m.FullName.ToLower().Contains(userName.ToLower()));
                    }

                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        data = data.Where(m => m.IsAllStore == true || m.StoreNo.ToLower().Contains(storeNo.ToLower()));
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        var IsActive = status == "1" ? true : false;
                        data = data.Where(m => m.IsActive == IsActive);
                    }

                    var listData = data.ToList();
                    if (!string.IsNullOrEmpty(roleCode))
                    {
                        listData = listData.Where(m => m.RoleCode == roleCode).ToList();
                    }
                    total = listData.Count();
                    var result = listData.OrderByDescending(d => d.CreatedDate).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }
                return new List<AdminUserModel>();
            }
        }
        public List<RoleModel> LoadRole()
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var data = db.Roles.Where(a => a.Status == true)
                        .Select(a => new RoleModel
                        {
                            ID = a.ID,
                            RoleCode = a.RoleCode,
                            RoleName = a.RoleName
                        });

                    var result = data.ToList();
                    return result;
                }
                catch (Exception ex)
                {
                }
                return new List<RoleModel>();
            }
        }
        public Tuple<bool, string> UpdatePermUser(List<AdminUserModel> listModel)
        {
            var result = new Tuple<bool, string>(false, "");
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 5 * 60;
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in listModel)
                        {
                            var stt = item.IsActive;
                            var checkUser = db.AdminUsers.FirstOrDefault(d => d.UserName.ToLower() == item.UserName.ToLower());
                            if (checkUser != null)
                            {
                                checkUser.IsActive = stt;
                                checkUser.RoleCode = item.RoleCode;
                                checkUser.StoreNo = item.StoreNo;
                                checkUser.IsAllStore = item.IsAllStore;
                                checkUser.TypeLogin = item.TypeLogin;
                                checkUser.StaffCode = item.StaffCode;
                                checkUser.FullName = item.FullName;
                                checkUser.UpdatedBy = item.UpdatedBy;
                                checkUser.UpdatedDate = item.UpdatedDate;
                            }
                            else
                            {
                                var itemAdd = new VCM.BLUEPOS.Data.EF.Central.AdminUser
                                { 
                                    TypeLogin = item.TypeLogin,
                                    UserName = item.UserName,
                                    Password = item.Password,
                                    StaffCode = item.StaffCode,
                                    RoleCode = item.RoleCode,
                                    StoreNo = item.StoreNo,
                                    IsAllStore = item.IsAllStore,
                                    FullName = item.FullName,
                                    IsActive = stt,
                                    CreatedBy = item.CreatedBy,
                                    CreatedDate = item.CreatedDate
                                };
                                db.AdminUsers.Add(itemAdd);
                            }
                        }

                        db.SaveChanges();
                        transaction.Commit();
                        result = new Tuple<bool, string>(true, $"Phân quyền thành công user ({string.Join(",", listModel.Select(a => a.UserName))})");
                    }
                    catch
                    {
                        transaction.Rollback();
                        result = new Tuple<bool, string>(false, $"Phân quyền thất bại user ({string.Join(",", listModel.Select(a => a.UserName))})");
                    }
                }
                return result;
            }
        }
        public List<StoreModel> GetSiteNoByUser(string userName)
        {
            var result = new List<StoreModel>();
            try
            {
                var storeSet = new List<StoreSetServerModel>();
                using (var dbGeneral = new CentralGeneralContainer())
                {
                    dbGeneral.Database.CommandTimeout = 2 * 60;
                    storeSet = dbGeneral.StoreSetServers.Select(a => new StoreSetServerModel
                    {
                        ServerIP = a.ServerIP,
                        ServerRead = a.ServerRead,
                        StoreNo = a.StoreNo,
                        Description = a.Description
                    }).ToList();
                }

                if (storeSet != null && storeSet.Count > 0)
                {
                    using (var db = new CentralMDPartnerContainer())
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var data = db.AdminUsers.Where(d => d.UserName == userName).Select(a => new AdminUserModel
                        {
                            TypeLogin = a.TypeLogin,
                            UserName = a.UserName,
                            StaffCode = a.StaffCode,
                            StoreNo = a.StoreNo,
                            IsAllStore = a.IsAllStore
                        }).FirstOrDefault();

                        if (data != null)
                        {
                            var storePermision = db.Stores.Where(a => a.ClosingMethod == 0).ToList();
                            if (data.IsAllStore == true)
                            {                             
                                result = (from a in storePermision
                                          join b in storeSet on a.No equals b.StoreNo
                                          select new StoreModel
                                          {
                                              StoreNo = a.No,
                                              StoreName = a.Name,                                            
                                              ServerIP = b.ServerIP
                                          }).ToList();
                            }
                            else
                            {
                                var listStore = JsonConvert.DeserializeObject<List<string>>(data.StoreNo);
                                result = (from a in storePermision
                                          join b in storeSet on a.No equals b.StoreNo
                                          where listStore.Contains(a.No)
                                          select new StoreModel
                                          {
                                              StoreNo = a.No,
                                              StoreName = a.Name,
                                              ServerIP = b.ServerIP
                                          }).ToList();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<MenuModel> LoadAllMenu(string menuName, string controller, string action)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {

                    var data = db.Menus.Select(a => new MenuModel
                    {
                        ID = a.ID,
                        MenuName = a.MenuName,
                        Controller = a.Controller,
                        Action = a.Action,
                        Status = a.Status,
                        Icon = a.Icon,
                        ParentMenu = a.ParentMenu,
                        Orderby = a.Orderby,
                        CreatedDate = a.CreatedDate,
                        CreatedUser = a.CreatedUser,
                        LastupdateDate = a.LastupdateDate,
                        LastUpdateUser = a.LastUpdateUser

                    });
                    if (!string.IsNullOrEmpty(menuName))
                    {
                        data = data.Where(m => m.MenuName.ToLower().Contains(menuName.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(controller))
                    {
                        data = data.Where(m => m.Controller.ToLower().Contains(controller.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(action))
                    {
                        data = data.Where(m => m.Action.ToLower().Contains(action.ToLower()));
                    }

                    var result = data.OrderBy(d => d.Orderby).ToList();
                    return result;
                }
                catch (Exception ex)
                {

                }
                return new List<MenuModel>();
            }
        }
        public Tuple<bool, string, MenuModel> UpdateMenu(MenuModel model)
        {
            var dataModel = new MenuModel();
            var result = new Tuple<bool, string, MenuModel>(true, "", dataModel);
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var checkMenu = db.Menus.FirstOrDefault(d => d.ID == model.ID);
                        if (checkMenu != null)
                        {
                            checkMenu.MenuName = model.MenuName;
                            checkMenu.Controller = model.Controller;
                            checkMenu.Action = model.Action;
                            checkMenu.Status = model.Status;
                            checkMenu.LastupdateDate = model.LastupdateDate;
                            checkMenu.LastUpdateUser = model.LastUpdateUser;

                            dataModel = new MenuModel
                            {
                                ID = checkMenu.ID,
                                MenuName = checkMenu.MenuName,
                                Controller = checkMenu.Controller,
                                Action = checkMenu.Action,
                                Status = checkMenu.Status,
                                Icon = checkMenu.Icon,
                                Orderby = checkMenu.Orderby,
                                ParentMenu = checkMenu.ParentMenu
                            };

                        }
                        else
                        {
                            var item = new Menu
                            {
                                MenuName = model.MenuName,
                                Controller = model.Controller,
                                Action = model.Action,
                                Status = model.Status,
                                Icon = model.Icon,
                                Orderby = db.Menus.Where(d => d.ParentMenu == model.ParentMenu).Max(a => a.Orderby) + 1 ?? 1,
                                ParentMenu = model.ParentMenu,
                                CreatedDate = model.CreatedDate,
                                CreatedUser = model.CreatedUser
                            };
                            db.Menus.Add(item);
                            db.SaveChanges();

                            dataModel = new MenuModel
                            {
                                ID = item.ID,
                                MenuName = item.MenuName,
                                Controller = item.Controller,
                                Action = item.Action,
                                Status = item.Status,
                                Icon = item.Icon,
                                Orderby = item.Orderby,
                                ParentMenu = item.ParentMenu
                            };

                        }
                        result = new Tuple<bool, string, MenuModel>(true, "", dataModel);
                        db.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        result = new Tuple<bool, string, MenuModel>(false, ex.Message, null);
                    }
                }
                return result;
            }
        }
        public Tuple<bool, string> DeleteMenu(int typeMenu, int ID)
        {
            var result = new Tuple<bool, string>(true, "");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkID = db.Menus.FirstOrDefault(d => d.ID == ID);
                    if (checkID != null)
                    {
                        if (typeMenu == 3)
                        {
                            db.Menus.Remove(checkID);
                        }
                        else if (typeMenu == 2)
                        {
                            db.Menus.RemoveRange(db.Menus.Where(d => d.ParentMenu == ID));
                            db.Menus.Remove(checkID);
                        }
                        else if (typeMenu == 1)
                        {
                            var listMenuParentC2 = db.Menus.Where(d => d.ParentMenu == ID);
                            foreach (var menuC2 in listMenuParentC2)
                            {
                                db.Menus.RemoveRange(db.Menus.Where(d => d.ParentMenu == menuC2.ID));
                                db.Menus.Remove(db.Menus.FirstOrDefault(d => d.ID == menuC2.ID));
                            }
                            db.Menus.Remove(checkID);
                        }

                        db.SaveChanges();
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, $"Không tồn tại Menu này trong hệ thống");
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }
        public MenuModel GetMenuByID(int ID)
        {
            var result = new MenuModel();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.Menus.Where(d => d.ID == ID)
                              .Select(a => new MenuModel
                              {
                                  ID = a.ID,
                                  MenuName = a.MenuName,
                                  Controller = a.Controller,
                                  Action = a.Action,
                                  Status = a.Status,
                                  Icon = a.Icon,
                                  ParentMenu = a.ParentMenu,
                                  Orderby = a.Orderby
                              }).FirstOrDefault();
                }
                catch
                {

                }
                return result;
            }
        }
        public UserRoleModel GetRoleByUser(string userName)
        {
            var result = new UserRoleModel();

            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var userDB = db.AdminUsers.Where(d => d.UserName.ToLower() == userName.ToLower());
                    result = (from a in userDB
                              join b in db.Roles on a.RoleCode equals b.RoleCode
                              select new UserRoleModel
                              {
                                  RoleCode = a.RoleCode,
                                  RoleName = b.RoleName
                              }).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public Tuple<bool, string, List<StaffModel>> GetStaff(List<string> listStaffCode)
        {
            var listModel = new List<StaffModel>();
            var result = new Tuple<bool, string, List<StaffModel>>(false, "", listModel);

            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var checkStaff = db.Staffs.Where(d => listStaffCode.Any(a => a == d.ID)).ToList();
                    var checkNotStaff = listStaffCode.Except(checkStaff.Select(a => a.ID)).ToList();

                    if (checkNotStaff.Count > 0)
                    {
                        result = new Tuple<bool, string, List<StaffModel>>(false, $"Không tồn tại user ({string.Join(",", checkNotStaff)}) này trong hệ thống", listModel);
                        return result;
                    }

                    listModel = checkStaff.Select(a => new StaffModel
                    {
                        ID = a.ID,
                        FirstName = a.FirstName,
                        LastName = a.LastName,
                        StoreNo = a.StoreNo,
                        EmploymentType = a.EmploymentType,
                        Password = a.Password                        
                    }).ToList();

                    result = new Tuple<bool, string, List<StaffModel>>(true, $"User ({string.Join(",", listModel.Select(a => a.ID))}) hợp lệ", listModel);
                    return result;
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, List<StaffModel>>(false, $"{JsonConvert.SerializeObject(ex)}", listModel);
                }
                return result;
            }
        }

        public Tuple<bool, string, List<ParamJsonStoreList>> GetSiteNoByStaffCode(string userName, string storeNo, string storeName, out int total, int skip, int take)
        {
            var listModel = new List<ParamJsonStoreList>();
            var result = new Tuple<bool, string, List<ParamJsonStoreList>>(false, "", listModel);
            total = 0;

            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.AdminUsers.Where(d => d.UserName == userName).FirstOrDefault();

                    if (data != null)
                    {
                        if (data.IsAllStore == true)
                        {
                            var dataStore = db.Stores.Where(a => a.ClosingMethod == 0)
                                .Select(a => new ParamJsonStoreList
                                {
                                    SiteNo = a.No,
                                    SiteName = a.Name

                                });
                            if (!string.IsNullOrEmpty(storeNo))
                            {
                                dataStore = dataStore.Where(m => m.SiteNo == storeNo);
                            }
                            if (!string.IsNullOrEmpty(storeName))
                            {
                                dataStore = dataStore.Where(m => m.SiteName.Contains(storeName));
                            }

                            total = dataStore.Count();
                            listModel = dataStore.OrderBy(d => d.SiteNo).Skip(skip).Take(take).ToList();                            
                        }
                        else
                        {
                            var listStore = JsonConvert.DeserializeObject<List<string>>(data.StoreNo);
                            var dataStore = db.Stores.Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0)
                                .Select(a => new ParamJsonStoreList
                                {
                                    SiteNo = a.No,
                                    SiteName = a.Name
                                });

                            if (!string.IsNullOrEmpty(storeNo))
                            {
                                dataStore = dataStore.Where(m => m.SiteNo == storeNo);
                            }
                            if (!string.IsNullOrEmpty(storeName))
                            {
                                dataStore = dataStore.Where(m => m.SiteName == storeName);
                            }

                            total = dataStore.Count();
                            listModel = dataStore.OrderByDescending(d => d.SiteNo).Skip(skip).Take(take).ToList();
                        }

                        result = new Tuple<bool, string, List<ParamJsonStoreList>>(true, $"OK", listModel);

                    }
                    else
                    {
                        result = new Tuple<bool, string, List<ParamJsonStoreList>>(false, $"User này {userName} chưa được phân quyền vào hệ thống", listModel);
                    }
                }
            }
            catch (Exception ex)
            {
                total = 0;
                result = new Tuple<bool, string, List<ParamJsonStoreList>>(false, $"{JsonConvert.SerializeObject(ex)}", listModel);
            }
            return result;
        }

        // ------------ PHAN QUYEN USER POSWEB -------------

        //public List<PosWebAdminUserModel> LoadUserByPOSWeb(string typeLogin, string userName, string storeNo, string status, out int total, int skip, int take)
        //{
        //    using (var db = new PLHWebMobileContainer())  // DB = PLHWebMobile : 10.235.55.122\PLH (Test)
        //    {
        //        try
        //        {
        //            var data = db.AdminUsers
        //                        .Select(a => new AdminUserModel
        //                        {
        //                            ID = a.ID,
        //                            TypeLogin = a.TypeLogin,
        //                            UserName = a.UserName,
        //                            Password = a.Password,
        //                            StaffCode = a.StaffCode,
        //                            FullName = a.FullName,
        //                            StoreNo = a.StoreNo,
        //                            StoreSales = a.StoreSale,
        //                            IsActive = a.IsActive,
        //                            IsMobile = a.IsMobile,
        //                            CreatedBy = a.CreatedBy,
        //                            CreatedDate = a.CreatedDate,
        //                            UpdatedBy = a.UpdatedBy,
        //                            UpdatedDate = a.UpdatedDate
        //                        });

        //            if (!string.IsNullOrEmpty(typeLogin))
        //            {
        //                data = data.Where(m => m.TypeLogin == typeLogin);
        //            }
        //            if (!string.IsNullOrEmpty(userName))
        //            {
        //                data = data.Where(m => m.UserName.ToLower().Contains(userName.ToLower()) || m.StaffCode.ToLower().Contains(userName.ToLower()) || m.FullName.ToLower().Contains(userName.ToLower()));
        //            }
        //            if (!string.IsNullOrEmpty(storeNo))
        //            {
        //                data = data.Where(m => m.StoreNo.ToLower().Contains(storeNo.ToLower()));
        //                //data = data.Where(m => m.IsAllStore == true || m.StoreNo.ToLower().Contains(storeNo.ToLower()));
        //            }
        //            if (!string.IsNullOrEmpty(status))
        //            {
        //                var IsActive = status == "1" ? true : false;
        //                data = data.Where(m => m.IsActive == IsActive);
        //            }

        //            var listData = data.ToList();
        //            total = listData.Count();
        //            var result = listData.OrderByDescending(d => d.CreatedDate).Skip(skip).Take(take).ToList();
        //            var data1 = result.ToList().Select(a => new PosWebAdminUserModel
        //            {
        //                ID = a.ID,
        //                TypeLogin = a.TypeLogin,
        //                UserName = a.UserName,
        //                Password = a.Password,
        //                StaffCode = a.StaffCode,
        //                FullName = a.FullName,
        //                StoreNo = a.StoreNo,
        //                StoreSalesStr = a.StoreSales,
        //                IsActive = a.IsActive,
        //                IsMobile = a.IsMobile,
        //                CreatedBy = a.CreatedBy,
        //                CreatedDate = a.CreatedDate,
        //                UpdatedBy = a.UpdatedBy,
        //                UpdatedDate = a.UpdatedDate
        //            }).ToList();

        //            var listUserAdmin = new List<PosWebAdminUserModel>();
        //            var listItem = new List<PosWebAdminUserModel>();
        //            var model = new PosWebAdminUserModel();
        //            foreach (var item in data1)
        //            {
        //                model = new PosWebAdminUserModel
        //                {
        //                    ID = item.ID,
        //                    TypeLogin = item.TypeLogin,
        //                    UserName = item.UserName,
        //                    Password = item.Password,
        //                    StaffCode = item.StaffCode,
        //                    FullName = item.FullName,
        //                    IsActive = item.IsActive,
        //                    IsMobile = item.IsMobile,
        //                    CreatedBy = item.CreatedBy,
        //                    CreatedDate = item.CreatedDate,
        //                    UpdatedBy = item.UpdatedBy,
        //                    UpdatedDate = item.UpdatedDate,
        //                    StoreNo = item.StoreSalesStr,
        //                    PosTerminal = item.StoreSalesStr
        //                };
        //                listItem.Add(model);
        //            }

        //            var data2 = listItem.ToList().Select(a => new PosWebAdminUserModel
        //            {
        //                ID = a.ID,
        //                TypeLogin = a.TypeLogin,
        //                UserName = a.UserName,
        //                Password = a.Password,
        //                StaffCode = a.StaffCode,
        //                FullName = a.FullName,
        //                PosTerminal = string.IsNullOrEmpty(a.StoreNo) ? string.Empty: a.StoreNo.Split('[','"',':')[3], // VD : ["2001:200105"]
        //                StoreNo = string.IsNullOrEmpty(a.StoreNo) ? string.Empty: a.StoreNo.Split(':','"',']')[1],
        //                IsActive = a.IsActive,
        //                IsMobile = a.IsMobile,
        //                CreatedBy = a.CreatedBy,
        //                CreatedDate = a.CreatedDate,
        //                UpdatedBy = a.UpdatedBy,
        //                UpdatedDate = a.UpdatedDate
        //            }).ToList();

        //            listUserAdmin = data2;
        //            return listUserAdmin;
        //        }
        //        catch (Exception ex)
        //        {
        //            total = 0;
        //            return new List<PosWebAdminUserModel>();
        //        }
        //    }
        //}


        //public Tuple<bool, string> CreatePermUserByPOSWeb(List<AdminUserModel> listModel)
        //{
        //    var result = new Tuple<bool, string>(false, "");

        //    using (var db = new PLHWebMobileContainer())
        //    {

        //        db.Database.CommandTimeout = 2 * 60;

        //        using (var transaction = db.Database.BeginTransaction())
        //        {
        //            try
        //            {
        //                /* - 1 tài khoản thì chỉ đi với 1 posterminal
        //                 * - Posterminal không được trùng
        //                 */

        //                foreach (var item in listModel)
        //                {
        //                    var checkPOS = db.AdminUsers.FirstOrDefault(a =>a.StoreSale == item.StoreSales);

        //                    if (checkPOS != null)
        //                    {
        //                        result = new Tuple<bool, string>(false, $"Máy POS ({string.Join(",", item.StoreSales)}) này đã tồn tại trong hệ thống. Vui lòng chọn máy POS khác");
        //                        return result;
        //                    }

        //                    var stt = item.IsActive;
        //                    var checkUser = db.AdminUsers.FirstOrDefault(d => d.UserName.ToLower() == item.UserName.ToLower() && d.TypeLogin == item.TypeLogin);

        //                    if (checkUser != null) // Đã tồn tại trong hệ thống
        //                    {
        //                        result = new Tuple<bool, string>(false, $"User ({string.Join(",", listModel.Select(a => a.UserName))}) này đã được phân quyền trong hệ thống. Vui lòng kiểm tra lại");
        //                        return result;
        //                    }
        //                    else
        //                    {
        //                        var passWord = "";
        //                        if (item.TypeLogin == "LOCAL") // Lấy PassWord Local
        //                        {
        //                            using (var db1 = new CentralMDPartnerContainer())
        //                            {
        //                                passWord = db1.Staffs.FirstOrDefault(x => x.ID == item.UserName.Trim().ToLower()).Password;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            passWord = String.Empty;
        //                        }

        //                        var itemAdd = new AdminUser
        //                        {
        //                            TypeLogin = item.TypeLogin,
        //                            UserName = item.UserName,
        //                            Password = passWord,
        //                            FullName = item.FullName,
        //                            RoleCode = item.RoleCode,
        //                            StaffCode = item.StaffCode,
        //                            StoreNo = item.StoreNo,
        //                            StoreSale = item.StoreSales,
        //                            IsAllStore = false,             // Mặc định là không xem all CH/ST
        //                            IsActive = stt,
        //                            IsMobile = item.IsMobile,
        //                            CreatedBy = item.CreatedBy,
        //                            CreatedDate = item.CreatedDate
        //                        };
        //                        db.AdminUsers.Add(itemAdd);
        //                    }
        //                }

        //                db.SaveChanges();
        //                transaction.Commit();
        //                result = new Tuple<bool, string>(true, $"Phân quyền thành công user ({string.Join(",", listModel.Select(a => a.UserName))})");
        //            }
        //            catch
        //            {
        //                transaction.Rollback();
        //                result = new Tuple<bool, string>(false, $"Phân quyền thất bại user ({string.Join(",", listModel.Select(a => a.UserName))})");
        //            }
        //        }
        //        return result;
        //    }
        //}

        //public ResultResponseModel UpdatePermUserAdminByPOSWeb(string typeLogin, string userName, string status, string UpdateBy, string isMobile)
        //{
        //    using (var db = new PLHWebMobileContainer())
        //    {
        //        try
        //        {
        //            var data = db.AdminUsers.Where(x =>x.UserName == userName.Trim() && x.TypeLogin == typeLogin).FirstOrDefault();
        //            bool isActive = true;
        //            if (status == "1")
        //            {
        //                isActive = true;
        //            }
        //            else
        //            {
        //                isActive = false;
        //            }

        //            data.IsActive = isActive;
        //            //data.IsMobile = isMobile == "1" ? true : false;
        //            data.UpdatedBy = UpdateBy;
        //            data.UpdatedDate = DateTime.Now;
        //            db.SaveChanges();
        //        }
        //        catch (Exception ex)
        //        {
        //            return new ResultResponseModel
        //            {
        //                Status = Model.Enums.ResultEnum.Fail,
        //                Message = "Có lỗi xảy ra"
        //            };
        //        }
        //    }
        //    return new ResultResponseModel
        //    {
        //        Status = Model.Enums.ResultEnum.Success,
        //        Message = "Cập nhật thành công"
        //    };
        //}

        //public ResultResponseModel DeleteUserAdminByPOSWeb(DeleteUserAdminByPosWebModel req)
        //{
        //    try
        //    {
        //        using (var db = new PLHWebMobileContainer())
        //        {
        //            var data = db.AdminUsers.FirstOrDefault(x => x.UserName == req.UserName && x.TypeLogin == req.TypeLogin);
        //            db.AdminUsers.Remove(data);
        //            db.SaveChanges();

        //            return new ResultResponseModel
        //            {
        //                Status = Model.Enums.ResultEnum.Success,
        //                Message = "Xóa thành công"
        //            };
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ResultResponseModel
        //        {
        //            Status = Model.Enums.ResultEnum.ErrorSystem,
        //            Message = ex.Message
        //        };
        //    }
        //}

        public Tuple<string, ADUserModel> ChangePassWord(ChangePassWordModel req)
        {
            var result = new ADUserModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;

                    result = (from a in db.AdminUsers
                              join b in db.Staffs on a.UserName equals b.ID
                              where a.UserName == req.UserName && a.TypeLogin == req.LoginType
                              select new ADUserModel
                              {
                                  StaffCode = a.StaffCode,
                                  Department = "",
                                  FullName = a.FullName,
                                  Role = a.RoleCode,
                                  Email = string.Empty,
                                  MasterCode = string.Empty,
                                  Phone = string.Empty,
                                  Title = string.Empty,
                                  UserName = req.UserName,
                                  SiteCode = b.StoreNo,
                                  PasswordLocal = b.Password,
                                  PasswordOld = req.PassWordOld,
                                  PasswordNew = req.PassWordNew,
                                  IsActive = a.IsActive ?? false
                              }).FirstOrDefault();

                    if (result.PasswordLocal == req.PassWordOld.Trim()) // cho đổi mật khẩu
                    {
                        return new Tuple<string, ADUserModel>($"OK", result);
                    }
                    else
                    {
                        return new Tuple<string, ADUserModel>($"Mật khẩu không đúng. Vui lòng kiểm tra lại", result);
                    }
                }
            }
            catch (Exception ex)
            {
                return new Tuple<string, ADUserModel>(ex.Message, result);
            }
        }

        public Tuple<string, ADUserModel> UpdateChangePassWord(UpdateChangePassWordModel req)
        {
            var result = new ADUserModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Staffs.FirstOrDefault(x => x.ID == req.StaffCode.Trim() && x.Blocked == 0);
                    if (data == null)
                    {
                        return new Tuple<string, ADUserModel>($"Nhân viên này không tồn tại", result);
                    }
                    var maxCounter = db.Staffs.Max(x => x.Counter) ?? 0;
                    data.Password = req.PassWordNew1.Trim();  
                    data.LastDateModified = DateTime.Now;
                    data.Counter = Convert.ToInt32(maxCounter) + 1;
                    db.SaveChanges();
                    return new Tuple<string, ADUserModel>($"OK", result);  // doi mat khau thanh cong
                }
            }
            catch (Exception ex)
            {
                return new Tuple<string, ADUserModel>(ex.Message, result);
            }
        }

        public Tuple<string, ADUserModel> CheckTypeLogin(UpdateChangePassWordModel req)
        {
            var result = new ADUserModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.AdminUsers.AsNoTracking().FirstOrDefault(x => x.StaffCode == req.StaffCode.Trim() && x.IsActive == true);
                    if (data == null)
                    {
                        return new Tuple<string, ADUserModel>($"Không tìm thấy nhân viên này", result);
                    }                    
                    return new Tuple<string, ADUserModel>($"{data.TypeLogin}", result);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<string, ADUserModel>(ex.Message, result);
            }
        }

        public Tuple<string, ADUserModel> CheckPassWordOld(UpdateChangePassWordModel req)
        {
            var result = new ADUserModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Staffs.AsNoTracking().FirstOrDefault(x => x.ID == req.StaffCode.Trim() && x.Blocked == 0);                  
                    if (data.Password != req.PassWordOld)
                    {
                        return new Tuple<string, ADUserModel>($"FALSE", result);
                    }                    
                    return new Tuple<string, ADUserModel>($"OK", result);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<string, ADUserModel>(ex.Message, result);
            }
        }

        // 16/01/2025,tungnt8
        public SupportInforModel GetSupportInfor()
        {
            var data = new SupportInforModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var email = db.OptionDatas.AsNoTracking().Where(x => x.Caption == "SUPPORTINFOR" && x.Code == "EMAIL" && x.Status == true).FirstOrDefault()?.Description;
                    var netwworksystem = db.OptionDatas.AsNoTracking().Where(x => x.Caption == "SUPPORTINFOR" && x.Code == "NETWORKSYSTEM" && x.Status == true).FirstOrDefault()?.Description;
                    var operateapp = db.OptionDatas.AsNoTracking().Where(x => x.Caption == "SUPPORTINFOR" && x.Code == "OPERATEAPP" && x.Status == true).FirstOrDefault()?.Description;
                    var model = new SupportInforModel()
                                    {
                                        Email = email,
                                        NetWorkSystem = netwworksystem,
                                        OperateApp = operateapp
                                    };
                    data = model;
                }
            }
            catch (Exception ex)
            {
            }
            return data;
        }

        //30/06/2025,tungnt8
        public async Task<ResultResponseModel> CreateUserStoreAsync(List<CreateUserStoreModel> req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var now = DateTime.Now;
                    string listStore = "";
                   
                    foreach ( var a in req)
                    {
                        if (a.TypeLogin == "LOCAL")   // login local
                        {
                            var checkUser = await db.AdminUsers.AsNoTracking().AnyAsync(x => x.UserName == a.StaffCode);
                            if (checkUser)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Fail,
                                    Message = $"Mã nhân viên {a.StaffCode} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại"
                                };
                            }

                            if (a.IsAllStore != "1")  // Không xem tất cả Store
                            {
                                if (!a.IsAutoImportStore)
                                {
                                    listStore = a.StoreNo;
                                }
                                else
                                {
                                    var listStoreImport = a.MutilStoreAuto?.Split(',', ';').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).Distinct().ToList();
                                    var storeDb = await db.Stores.AsNoTracking().Where(x => x.ClosingMethod == 0 && listStoreImport.Contains(x.No))
                                        .Select(x => x.No)
                                        .ToListAsync();

                                    if (listStoreImport.Count != storeDb.Count())
                                    {
                                        var inValidStore = listStoreImport.Where(x => !storeDb.Contains(x)).ToList();
                                        return new ResultResponseModel
                                        {
                                            Status = Model.Enums.ResultEnum.Fail,
                                            Message = $"Danh sách cửa hàng {String.Join(",", inValidStore.ToArray())} không hợp lệ. Vui lòng kiểm tra lại"
                                        };
                                    }
                                    listStore = JsonConvert.SerializeObject(listStoreImport);
                                }
                            }

                            var item = new AdminUser
                            {
                                TypeLogin = a.TypeLogin,
                                StaffCode = string.IsNullOrEmpty(a.StaffCode) ? a.UserName : a.StaffCode,     // Neu khong co staffcode thi luu username                        
                                StoreNo = a.IsAllStore == "0" ? listStore : string.Empty,                     //neu duoc quyen vao tat ca st/ch thi luu ALL nguoc lai luu liststore duoc chon
                                UserName = a.TypeLogin == "LOCAL" ? a.StaffCode : a.UserName,                 // Login local thi save staffCode vao column UserName
                                FullName = a.FullName,
                                RoleCode = a.RoleCode,
                                IsAllStore = a.IsAllStore == "1" ? true : false,
                                IsActive = a.IsActive,
                                CreatedDate = DateTime.Now,
                                CreatedBy = a.CreatedBy
                            };
                            db.AdminUsers.Add(item);
                            await db.SaveChangesAsync();
                        }
                        else    // TypeLogin = 1 : tai khoan AD
                        {
                            
                            var checkUser = await db.AdminUsers.AsNoTracking().AnyAsync(x => x.UserName == a.UserName);
                            if (checkUser)
                            {
                                return new ResultResponseModel { Status = Model.Enums.ResultEnum.Fail, Message = $"Tài khoản {a.UserName} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại" };
                            }

                            if (a.IsAllStore != "1")  // Không xem tất cả Store
                            {
                                if (!a.IsAutoImportStore)
                                {
                                    listStore = a.StoreNo;
                                }
                                else
                                {
                                    var listStoreImport = a.MutilStoreAuto?.Split(',', ';').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).Distinct().ToList();
                                    var storeDb = await db.Stores.AsNoTracking().Where(x => x.ClosingMethod == 0 && listStoreImport.Contains(x.No))
                                        .Select(x => x.No)
                                        .ToListAsync();

                                    if (listStoreImport.Count != storeDb.Count())
                                    {
                                        var inValidStore = listStoreImport.Where(x => !storeDb.Contains(x)).ToList();
                                        return new ResultResponseModel
                                        {
                                            Status = Model.Enums.ResultEnum.Fail,
                                            Message = $"Danh sách cửa hàng {String.Join(",", inValidStore.ToArray())} không hợp lệ. Vui lòng kiểm tra lại"
                                        };
                                    }
                                    listStore = JsonConvert.SerializeObject(listStoreImport);
                                }
                            }

                            var item = new AdminUser
                            {
                                TypeLogin = a.TypeLogin,
                                StaffCode = string.IsNullOrEmpty(a.StaffCode) ? a.UserName : a.StaffCode,     // Neu khong co staffcode thi luu username                        
                                StoreNo = a.IsAllStore == "0" ? listStore : string.Empty,                   //neu duoc quyen vao tat ca st/ch thi luu ALL nguoc lai luu liststore duoc chon
                                UserName = a.TypeLogin == "LOCAL" ? a.StaffCode : a.UserName,                 // Login local thi save staffCode vao column UserName
                                FullName = a.FullName,
                                RoleCode = a.RoleCode,
                                IsAllStore = a.IsAllStore == "1" ? true : false,
                                IsActive = a.IsActive,
                                CreatedDate = DateTime.Now,
                                CreatedBy = a.CreatedBy
                            };
                            db.AdminUsers.Add(item);
                            await db.SaveChangesAsync();
                        }   
                    }
                    return new ResultResponseModel
                    {
                        IsStatus = 200,
                        Message = "Thêm mới thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        //30/06/2025,tungnt8
        public async Task<ResultResponseModel> UpdateUserStoreAsyncV2(UpdateUserStoreModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = await db.AdminUsers.FirstOrDefaultAsync(x => x.StaffCode == req.StaffCode || x.UserName.ToLower() == req.UserName.ToLower());
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    string listStore = "";
                    if (req.IsAutoImportStore == true)
                    {
                        var listStoreImport = req.MutilStoreAuto?.Split(',', ';').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).Distinct().ToList();
                        var storeDb = await db.Stores.AsNoTracking().Where(x => x.ClosingMethod == 0 && listStoreImport.Contains(x.No))
                            .Select(x => x.No)
                            .ToListAsync();

                        if (listStoreImport.Count != storeDb.Count())
                        {
                            var inValidStore = listStoreImport.Where(x => !storeDb.Contains(x)).ToList();
                            if (req.IsUpdateStore == false)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Fail,
                                    Message = $"Danh sách cửa hàng: {String.Join(",", inValidStore.ToArray())} này đang tạm ngưng hoạt động (hoặc không tồn tại) trong hệ thống. Vui lòng kiểm tra lại",
                                    Flag = 3,
                                    ListStoreNo = $"{ String.Join(",", inValidStore.ToArray()) }",
                                    Total = inValidStore.Count()
                                };
                            }
                            else
                            {
                                var listStoreUpdate = listStoreImport.Except(inValidStore);
                                listStore = JsonConvert.SerializeObject(listStoreUpdate);
                                data.StoreNo = listStore;
                                data.RoleCode = req.RoleCode;
                                data.IsActive = req.IsActive;
                                data.IsAllStore = false; // = 0
                                data.UpdatedDate = DateTime.Now;
                                data.UpdatedBy = req.UpdatedBy;

                                await db.SaveChangesAsync();
                                return new ResultResponseModel
                                {
                                    Item1 = req.StaffCode,
                                    Item2 = req.UserName,
                                    Item3 = req.FullName,
                                    Item4 = Convert.ToString(req.IsActive),
                                    Item5 = req.Permission,
                                    IsStatus = 200,
                                    Message = "Cập nhật thành công"
                                };
                            }
                        }

                        listStore = JsonConvert.SerializeObject(listStoreImport);
                        data.StoreNo = listStore;
                        data.RoleCode = req.RoleCode;
                        data.IsActive = req.IsActive;
                        data.IsAllStore = false; // = 0
                        data.UpdatedDate = DateTime.Now;
                        data.UpdatedBy = req.UpdatedBy;

                        await db.SaveChangesAsync();
                        return new ResultResponseModel
                        {
                            Item1 = req.StaffCode,
                            Item2 = req.UserName,
                            Item3 = req.FullName,
                            Item4 = Convert.ToString(req.IsActive),
                            Item5 = req.Permission,
                            IsStatus = 200,
                            Message = "Cập nhật thành công"
                        };

                    }
                    else
                    {
                        if (req.IsAllStore != "1") // Không xem tất cả Store
                        {
                            if (!req.IsAutoImportStore)
                            {
                                listStore = req.StoreNo;
                            }
                            else
                            {
                                var listStoreImport = req.MutilStoreAuto?.Split(',',';').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).Distinct().ToList();
                                var storeDb = await db.Stores.AsNoTracking().Where(x => x.ClosingMethod == 0 && listStoreImport.Contains(x.No))
                                    .Select(x => x.No)
                                    .ToListAsync();

                                if (listStoreImport.Count != storeDb.Count())
                                {
                                    var inValidStore = listStoreImport.Where(x => !storeDb.Contains(x)).ToList();
                                    if (req.IsUpdateStore == false)
                                    {
                                        return new ResultResponseModel
                                        {
                                            Status = Model.Enums.ResultEnum.Fail,
                                            Message = $"Danh sách cửa hàng: {String.Join(",", inValidStore.ToArray())} này đang tạm ngưng hoạt động (hoặc không tồn tại) trong hệ thống. Vui lòng kiểm tra lại",
                                            Flag = 3,
                                            ListStoreNo = $"{ String.Join(",", inValidStore.ToArray()) }",
                                            Total = inValidStore.Count()
                                        };
                                    }
                                    else
                                    {
                                        var listStoreUpdate = listStoreImport.Except(inValidStore);
                                        listStore = JsonConvert.SerializeObject(listStoreUpdate);
                                        data.StoreNo = req.IsAllStore == "1" ? string.Empty : listStore;
                                        data.RoleCode = req.RoleCode;
                                        data.IsActive = req.IsActive;
                                        data.IsAllStore = req.IsAllStore == "1" ? true : false;
                                        data.UpdatedDate = DateTime.Now;
                                        data.UpdatedBy = req.UpdatedBy;

                                        await db.SaveChangesAsync();
                                        return new ResultResponseModel
                                        {
                                            Item1 = req.StaffCode,
                                            Item2 = req.UserName,
                                            Item3 = req.FullName,
                                            Item4 = Convert.ToString(req.IsActive),
                                            Item5 = req.Permission,
                                            Status = Model.Enums.ResultEnum.Success,
                                            Message = "Cập nhật thành công"
                                        };
                                    }
                                }
                                listStore = JsonConvert.SerializeObject(listStoreImport);
                            }
                        }
                    }

                    data.StoreNo = req.IsAllStore == "1" ? string.Empty : listStore;
                    data.RoleCode = req.RoleCode;
                    data.IsActive = req.IsActive;
                    data.IsAllStore = req.IsAllStore == "1" ? true : false;
                    data.UpdatedDate = DateTime.Now;
                    data.UpdatedBy = req.UpdatedBy;

                    await db.SaveChangesAsync();
                    return new ResultResponseModel
                    {
                        Item1 = req.StaffCode,
                        Item2 = req.UserName,
                        Item3 = req.FullName,
                        Item4 = Convert.ToString(req.IsActive),
                        Item5 = req.Permission,
                        IsStatus = 200,
                        Message = "Cập nhật thành công"
                    };
                      
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }
        public ResultResponseModel GetStoreByUser(ViewStoreByUserModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.AdminUsers.AsNoTracking().Where(x => x.StaffCode == req.StaffCode || x.UserName.ToLower() == req.UserName.ToLower());
                    var StoreArray = data.FirstOrDefault()?.StoreNo;
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy thông tin"
                        };
                    }

                    var  listStore = "";
                    var model = new ViewStoreByUserModel();
                    var modelList = new List<ViewStoreByUserModel>();
                    if (req.IsAutoImportStore == false)
                    {
                        listStore = string.Empty;
                    }
                    else
                    {
                        if (data.FirstOrDefault()?.IsAllStore == true || StoreArray == "")
                        {
                            var storeList = db.Stores.AsNoTracking().Where(x => x.ClosingMethod == 0).ToList();
                            var str = storeList.Select(a => a.No);
                            listStore = JsonConvert.SerializeObject(str);
                        }
                        else
                        {
                            if (StoreArray != "")
                            {
                                listStore = StoreArray;
                            }
                        } 
                    }
                    return new ResultResponseModel
                    {
                        Item1 = listStore,
                        IsStatus = 200
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }



    }
}
