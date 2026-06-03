using System;
using System.Configuration;
using TCX.API.Common.Constants;
using TCX.API.Common.Enums;
using VCM.POSBLUE.Model.Authen;

namespace VCM.POSBLUE.Data.Authen
{
    public interface ILogin
    {
        Tuple<ADConnectionStatus, ADUserModel> Login(LoginViewModel req);
    }
    public class LoginData : ILogin
    {        
        public Tuple<ADConnectionStatus, ADUserModel> Login(LoginViewModel req)
        {
            var pathLapAd = ConfigurationManager.AppSettings[LAPConfigResource.PathLapAd];
            var lapIp = ConfigurationManager.AppSettings[LAPConfigResource.PathLapIP];
            var usernameLapAdmin = ConfigurationManager.AppSettings[LAPConfigResource.LapAdUserNameAdmin];
            var passwordLapAdmin = ConfigurationManager.AppSettings[LAPConfigResource.LapAdPasswordAdmin];
            var loginUser = new LoginModel();
            var status = LapConnection.CheckUsesLoginAd(lapIp, pathLapAd, usernameLapAdmin, passwordLapAdmin, req.UserName, req.PassWord);
            var result = new Tuple<ADConnectionStatus, ADUserModel>(status, LapConnection.ADUser);
            return result;
        }

    }
}
