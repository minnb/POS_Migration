using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Authen;
using VCM.BLUEPOS.Model.Authen;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Model.Employee;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Role;


namespace VCM.BLUEPOS.Authen
{
    public interface IAuthenBLO
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

        // ------- PHAN QUYEN USER POS WEB ----------

        //List<PosWebAdminUserModel> LoadUserByPOSWeb(string typeLogin, string userName, string storeNo, string status, out int total, int skip, int take);
        //Tuple<bool, string> CreatePermUserByPOSWeb(List<AdminUserModel> listModel);
        //ResultResponseModel UpdatePermUserAdminByPOSWeb(string typeLogin, string userName, string status, string UpdateBy, string isMobile);
        //ResultResponseModel DeleteUserAdminByPOSWeb(DeleteUserAdminByPosWebModel req);

        //-----------------------------
        Tuple<string, ADUserModel> CheckPassWordOld(UpdateChangePassWordModel req);
        Tuple<string, ADUserModel> ChangePassWord(ChangePassWordModel req);
        Tuple<string, ADUserModel> UpdateChangePassWord(UpdateChangePassWordModel req);
        Tuple<string, ADUserModel> CheckTypeLogin(UpdateChangePassWordModel req);

        //30/06/2025,tungnt8
        Task<ResultResponseModel> CreateUserStoreAsync(List<CreateUserStoreModel> req);
        Task<ResultResponseModel> UpdateUserStoreAsyncV2(UpdateUserStoreModel req);
        ResultResponseModel GetStoreByUser(ViewStoreByUserModel req);
    }

    public class AuthenBLO : IAuthenBLO
    {
        private AuthenData data { get; set; }
        public AuthenBLO()
        {
            data = new AuthenData();
        }
        public Tuple<string, ADUserModel> LoginLocal(LoginViewModel req)
        {
            return data.LoginLocal(req);
        }
        public List<MenuModel> LoadMenu()
        {
            return data.LoadMenu();
        }
        public List<MenuModel> LoadMenuByUser(string userName)
        {
            if (!string.IsNullOrEmpty(userName))
            {
                userName = userName.Trim();
            }
            return data.LoadMenuByUser(userName);
        }
        public List<RoleModel> LoadDataRole(string roleCode, string roleName, string status, out int total, int skip, int take)
        {
            return data.LoadDataRole(roleCode, roleName, status, out total, skip, take);
        }

        public List<MenuRoleModel> LoadMenuRole(string roleCode)
        {
            return data.LoadMenuRole(roleCode);
        }
        public bool UpdateRole(RoleModel roleModel, List<ListIDMenuRequest> lstMenu)
        {
            return data.UpdateRole(roleModel, lstMenu);

        }
        public List<AdminUserModel> LoadUser(string typeLogin, string roleCode, string userName, string storeNo, string status, out int total, int skip, int take)
        {
            return data.LoadUser(typeLogin, roleCode, userName, storeNo, status, out total, skip, take);
        }
        public List<RoleModel> LoadRole()
        {
            return data.LoadRole();
        }
        public Tuple<bool, string> UpdatePermUser(List<AdminUserModel> listModel)
        {
            return data.UpdatePermUser(listModel);
        }
        public List<StoreModel> GetSiteNoByUser(string userName)
        {
            return data.GetSiteNoByUser(userName);
        }
        public List<MenuModel> LoadAllMenu(string menuName, string controller, string action)
        {
            return data.LoadAllMenu(menuName, controller, action);
        }
        public Tuple<bool, string, MenuModel> UpdateMenu(MenuModel model)
        {
            return data.UpdateMenu(model);
        }
        public MenuModel GetMenuByID(int ID)
        {
            return data.GetMenuByID(ID);
        }
        public Tuple<bool, string> DeleteMenu(int typeMenu, int ID)
        {
            return data.DeleteMenu(typeMenu, ID);
        }
        public UserRoleModel GetRoleByUser(string userName)
        {
            return data.GetRoleByUser(userName);
        }
        public Tuple<bool, string, List<StaffModel>> GetStaff(List<string> listStaffCode)
        {
            return data.GetStaff(listStaffCode);
        }
        public Tuple<bool, string, List<ParamJsonStoreList>> GetSiteNoByStaffCode(string userName, string storeNo, string storeName, out int total, int skip, int take)
        {
            return data.GetSiteNoByStaffCode(userName, storeNo, storeName, out total, skip, take);
        }
        public Tuple<bool, string> CheckUser(LoginViewModel req)
        {
            return data.CheckUser(req);
        }

        // --------- PHAN QUYEN USER POS WEB ----------

        //public List<PosWebAdminUserModel> LoadUserByPOSWeb(string typeLogin, string userName, string storeNo, string status, out int total, int skip, int take)
        //{
        //    return data.LoadUserByPOSWeb(typeLogin, userName, storeNo, status, out total, skip, take);
        //}

        //public Tuple<bool, string> CreatePermUserByPOSWeb(List<AdminUserModel> listModel)
        //{
        //    return data.CreatePermUserByPOSWeb(listModel);
        //}
        //public ResultResponseModel UpdatePermUserAdminByPOSWeb(string typeLogin, string userName, string status, string UpdateBy, string isMobile)
        //{
        //    return data.UpdatePermUserAdminByPOSWeb(typeLogin, userName, status, UpdateBy, isMobile);
        //}
        //public ResultResponseModel DeleteUserAdminByPOSWeb(DeleteUserAdminByPosWebModel req)
        //{
        //    return data.DeleteUserAdminByPOSWeb(req);
        //}

        //-----------
        public Tuple<string, ADUserModel> ChangePassWord(ChangePassWordModel req)
        {
            return data.ChangePassWord(req);
        }
        public Tuple<string, ADUserModel> UpdateChangePassWord(UpdateChangePassWordModel req)
        {
            return data.UpdateChangePassWord(req);
        }
        public Tuple<string, ADUserModel> CheckTypeLogin(UpdateChangePassWordModel req)
        {
            return data.CheckTypeLogin(req);
        }
        public Tuple<string, ADUserModel> CheckPassWordOld(UpdateChangePassWordModel req)
        {
            return data.CheckPassWordOld(req);
        }
        public SupportInforModel GetSupportInfor()
        {
            return data.GetSupportInfor();
        }

        //30/06/2025,tungnt8
        public Task<ResultResponseModel> CreateUserStoreAsync(List<CreateUserStoreModel> req)
        {
            return data.CreateUserStoreAsync(req);
        }
        public Task<ResultResponseModel> UpdateUserStoreAsyncV2(UpdateUserStoreModel req)
        {
            return data.UpdateUserStoreAsyncV2(req);
        }
        public ResultResponseModel GetStoreByUser(ViewStoreByUserModel req)
        {
            return data.GetStoreByUser(req);
        }

    }
}
