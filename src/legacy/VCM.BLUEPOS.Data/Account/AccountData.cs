using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Account;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.Employee;
using VCM.BLUEPOS.Model.Common;



namespace VCM.BLUEPOS.Data.Account
{
    public interface IAccountData
    {
        UserRoleModel GetRoleByUser(string userName);
        Tuple<string, ADUserModel> LoginLocal(LoginViewModel req);
        Tuple<bool, string> CheckUser(LoginViewModel req);
        List<MenuModel> LoadMenuByUser(string userName);
        Tuple<string, ADUserModel> ChangePassWord(ChangePassWordModel req);
        Tuple<string, ADUserModel> UpdateChangePassWord(UpdateChangePassWordModel req);
        Tuple<string, ADUserModel> CheckPassWordOld(UpdateChangePassWordModel req);
        Tuple<string, ADUserModel> CheckTypeLogin(UpdateChangePassWordModel req);
        Tuple<bool, string, string> CheckUserExistV2(string userName);
        SupportInforModel GetSupportInfor();

    }

    public class AccountData : IAccountData
    {
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

                    //result = (from a in db.AdminUsers
                    //          where a.UserName == req.UserName
                    //          select new ADUserModel
                    //          {
                    //              StaffCode = a.StaffCode,
                    //              Department = string.Empty,
                    //              FullName = a.FullName,
                    //              Role = a.RoleCode,
                    //              Email = string.Empty,
                    //              MasterCode = string.Empty,
                    //              Phone = string.Empty,
                    //              Title = string.Empty,
                    //              UserName = req.UserName,
                    //              IsActive = a.IsActive ?? false
                    //          }).FirstOrDefault();

                    //if (result == null)
                    //    return new Tuple<bool, string>(false,$"Tài khoản {req.UserName} không tồn tại (hoặc chưa được cấp quyền) truy cập trong hệ thống. Vui lòng kiểm tra lại");

                    //if (!result.IsActive)
                    //    return new Tuple<bool, string>(false,$"Tài khoản {req.UserName} đang bị khóa, Vui lòng liên hệ IT");                    


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
                //return new Tuple<bool, string>(false, ex.Message);
                result = new Tuple<bool, string>(false, $"{ex.Message}");
            }
            return result;
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

        // 20/05/2025, tungnt8: Login With SSO
        public Tuple<bool, string, string> CheckUserExistV2(string userName)
        {
            var result = new Tuple<bool, string, string>(true, "", "Success");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var processingUsername = $"0{userName}";
                    var user = db.AdminUsers.AsNoTracking().Where(a => a.IsActive == true && (a.UserName.ToLower() == userName.ToLower()
                    || a.UserName.ToLower() == processingUsername)).FirstOrDefault();
                    if (user == null)
                    {
                        result = new Tuple<bool, string, string>(false, "", $"Tài khoản {userName.ToLower()} này chưa được phân quyền (hoặc đang tạm khóa) truy cập. Vui lòng liên hệ bộ phận IT");
                    }
                    result = new Tuple<bool, string, string>(true, "1", "Success");
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string, string>(false, "", $"{ex.Message}");
            }

            return result;
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


    }
}
