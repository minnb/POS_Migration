using System;
using TCX.API.Common.Enums;
using VCM.POSBLUE.Data.Authen;
using VCM.POSBLUE.Model.Authen;

namespace VCM.POSBLUE.Business.Authen
{
    public interface ILoginBLO
    {
        Tuple<ADConnectionStatus, ADUserModel> LoginAD(LoginViewModel req);
    }
    public class LoginBLO : ILoginBLO
    {
        private LoginData data { get; set; }
        public LoginBLO()
        {
            data = new LoginData();
        }
        public Tuple<ADConnectionStatus, ADUserModel> LoginAD(LoginViewModel req)
        {
            return data.Login(req);
        }
    }
}
