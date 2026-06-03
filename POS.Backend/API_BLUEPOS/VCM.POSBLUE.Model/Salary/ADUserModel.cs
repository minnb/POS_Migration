using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Salary
{
    public class ADUserModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string MaNV { get; set; }
        //public string Avatar { get; set; }
        //public string ChucDanh { get; set; }
        public string FullName { get; set; }
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
    }
}
