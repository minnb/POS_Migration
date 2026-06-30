using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Authen
{
   public class AdminUserModel
    {
        public long ID { get; set; }
        public string TypeLogin { get; set; }
        public string UserName { get; set; }
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public string Password { get; set; }
        public string StaffCode { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string StoreSales { get; set; }
        public Nullable<bool> IsAllStore { get; set; }
        public string FullName { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<bool> IsMobile { get; set; }
        public Nullable<System.DateTime> EffectiveDate { get; set; }
        public Nullable<System.DateTime> ExpirationDate { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class PosWebAdminUserModel
    {
        [JsonProperty("id")]
        public long ID { get; set; }
        public string TypeLogin { get; set; }
        public string UserName { get; set; }
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public string Password { get; set; }
        public string StaffCode { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string StoreSalesStr { get; set; }
        public List<string> StoreSales { get; set; }
        public List<POSTerminalByAdminUserPosWeb> StoreSalesList { get; set; }
        public Nullable<bool> IsAllStore { get; set; }
        public string FullName { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<bool> IsMobile { get; set; }
        public Nullable<System.DateTime> EffectiveDate { get; set; }
        public Nullable<System.DateTime> ExpirationDate { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class POSTerminalByAdminUserPosWeb
    {
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }      
    }

    public class DeleteUserAdminByPosWebModel
    {
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string TypeLogin { get; set; }
        public string UserName { get; set; }
    }



}
