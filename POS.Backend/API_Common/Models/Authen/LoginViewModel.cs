using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Authen
{
    public class LoginViewModel
    {
        [Display(Name = "Tài Khoản")]
        public string UserName { get; set; }
        [Display(Name = "Mật Khẩu")]
        public string PassWord { get; set; }
    }
}
