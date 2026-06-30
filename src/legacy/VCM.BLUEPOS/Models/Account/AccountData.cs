using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using VCM.BLUEPOS.Model.Account;

namespace VCM.BLUEPOS.Models.Account
{
    public interface IAccount
    {
        Tuple<ADConnectionStatus, ADUserModel> LoginAD(LoginViewModel req);
        Tuple<bool, ADUserModel> CheckADUser(string userName);

    }

    public class AccountData : IAccount
    {
        public Tuple<bool, ADUserModel> CheckADUser(string userName)
        {
            var lapIp = ConfigurationManager.AppSettings["PathLapIP"];
            var usernameLapAdmin = ConfigurationManager.AppSettings["LapAdUserNameAdmin"];
            var passwordLapAdmin = ConfigurationManager.AppSettings["LapAdPasswordAdmin"];
            var data = LapConnection.GetADUserInfo(lapIp, usernameLapAdmin, passwordLapAdmin, userName);
            if (data == null || string.IsNullOrEmpty(data.UserName))
            {
                data = new ADUserModel { UserName = userName };
                return new Tuple<bool, ADUserModel>(false, data);
            }
            return new Tuple<bool, ADUserModel>(true, data);
        }

        public Tuple<ADConnectionStatus, ADUserModel> LoginAD(LoginViewModel req)
        {
            var pathLapAd = ConfigurationManager.AppSettings["PathLapAd"];
            var lapIp = ConfigurationManager.AppSettings["PathLapIP"];
            var usernameLapAdmin = ConfigurationManager.AppSettings["LapAdUserNameAdmin"];
            var passwordLapAdmin = ConfigurationManager.AppSettings["LapAdPasswordAdmin"];
            var status = LapConnection.CheckUsesLoginAd(lapIp, pathLapAd, usernameLapAdmin, passwordLapAdmin, req.UserName, req.PassWord);
            return new Tuple<ADConnectionStatus, ADUserModel>(status, LapConnection.ADUser);
        }
    }
}