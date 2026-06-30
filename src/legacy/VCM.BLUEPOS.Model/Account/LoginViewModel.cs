using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Account
{
    public class LoginViewModel
    {
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string LoginType { get; set; }
        public string RedirectUrl { get; set; }
    }

    public class ChangePassWordModel
    {
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string PassWordOld { get; set; }
        public string PassWordNew { get; set; }
        public string LoginType { get; set; }
        public string RedirectUrl { get; set; }
    }

    public class UpdateChangePassWordModel
    {
        public string UserName { get; set; }
        public string StaffCode { get; set; }
        public string PassWord { get; set; }
        public string PassWordOld { get; set; }
        public string PassWordNew1 { get; set; }
        public string PassWordNew2 { get; set; }
        public string LoginType { get; set; }
        public string RedirectUrl { get; set; }
    }


}
