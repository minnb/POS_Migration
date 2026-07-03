using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Authen
{
    public class ADUserModel
    {
        public string UserName { get; set; }
        public string PasswordLocal { get; set; }
        public string PasswordOld { get; set; }
        public string PasswordNew { get; set; }
        public string StaffCode { get; set; }
        public string FullName { get; set; }
        public string FullNameDep { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Company { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string SiteCode { get; set; }
        public string SiteName { get; set; }
        public string MasterCode { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
        public string TypeLogin { get; set; }
        public List<MenuModel> ListMenu { get; set; }

    }

}
