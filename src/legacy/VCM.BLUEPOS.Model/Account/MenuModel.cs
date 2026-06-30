using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Account
{
    public class MenuModel
    {
        public int ID { get; set; }
        public string MenuName { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public Nullable<bool> Status { get; set; }
        public string Icon { get; set; }
        public int ParenMenuID { get; set; }
        public int LevelMenu { get; set; }
        public string ParentMenuName { get; set; }
        public string ParentMenuIcon { get; set; }
        public Nullable<int> ParentMenuOrderby { get; set; }
        public Nullable<int> ParentMenu { get; set; }
        public Nullable<int> ParentMenuC1 { get; set; }
        public Nullable<int> Orderby { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<System.DateTime> LastupdateDate { get; set; }
        public string LastUpdateUser { get; set; }
    }
    public class ParentMenu
    {
        public int ID { get; set; }
        public string MenuName { get; set; }
        public Nullable<int> Orderby { get; set; }
        public List<MenuModel> ListMenu { get; set; }
    }
    public class ListIDMenuRequest
    {
        public int ID { get; set; }
    }

    public class MenuPermissionModel
    {
        public static List<MenuModel> ListMenuPermission { get; set; }
    }

    public class SupportInforModel
    {
        public string Email { get; set; }
        public string NetWorkSystem { get; set; }
        public string OperateApp { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }

}
