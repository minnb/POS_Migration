using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Authen
{
    public class UserRoleModel
    {
        public int ID { get; set; }
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public string UserName { get; set; }
        public string HoTen { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<System.DateTime> LastupdateDate { get; set; }
        public string LastUpdateUser { get; set; }
    }
}
