using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.MasterData
{
    public class ChangePassWordModel
    {
        public string StaffCode { get; set; }
        public string PassWord { get; set; }       
        public string PassWordOld { get; set; }    
        public string PassWordNew { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string StaffName { get; set; }
        public string EmploymentType { get; set; }
        public string PermissionGroup { get; set; }
        public string UpdatedDateStr { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public int Total { get; set; }

    }




}
