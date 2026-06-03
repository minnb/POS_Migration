using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Menu
{
   public class MenuModel
    {
        public int ID { get; set; }
        public string MenuCode { get; set; }
        public string MenuName { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public Nullable<bool> Status { get; set; }
        public Nullable<bool> IsAction { get; set; }
        public string Icon { get; set; }
        public Nullable<int> ParentMenu { get; set; }
        public Nullable<int> Orderby { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<System.DateTime> LastUpdateDate { get; set; }
        public string LastUpdateUser { get; set; }
    }
}
