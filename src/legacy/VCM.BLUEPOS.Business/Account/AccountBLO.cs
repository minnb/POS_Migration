using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Account;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Account;
using VCM.BLUEPOS.Model.Store;


namespace VCM.BLUEPOS.Account
{
    public interface IAccountBLO
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

    public class AccountBLO : IAccountBLO
    {
        private AccountData _account { get; set; }
        public AccountBLO()
        {
            _account = new AccountData();
        }

        public UserRoleModel GetRoleByUser(string userName)
        {
            return _account.GetRoleByUser(userName);
        }
        public Tuple<string, ADUserModel> LoginLocal(LoginViewModel req)
        {
            return _account.LoginLocal(req);
        }
        public Tuple<bool, string> CheckUser(LoginViewModel req)
        {
            return _account.CheckUser(req);
        }
        public List<MenuModel> LoadMenuByUser(string userName)
        {
            return _account.LoadMenuByUser(userName);
        }
        public Tuple<string, ADUserModel> ChangePassWord(ChangePassWordModel req)
        {
            return _account.ChangePassWord(req);
        }
        public Tuple<string, ADUserModel> UpdateChangePassWord(UpdateChangePassWordModel req)
        {
            return _account.UpdateChangePassWord(req);
        }
        public Tuple<string, ADUserModel> CheckPassWordOld(UpdateChangePassWordModel req)
        {
            return _account.CheckPassWordOld(req);
        }
        public Tuple<string, ADUserModel> CheckTypeLogin(UpdateChangePassWordModel req)
        {
            return _account.CheckTypeLogin(req);
        }
        public Tuple<bool, string, string> CheckUserExistV2(string userName)
        {
            return _account.CheckUserExistV2(userName);
        }
        public SupportInforModel GetSupportInfor()
        {
            return _account.GetSupportInfor();
        }


    }
}
