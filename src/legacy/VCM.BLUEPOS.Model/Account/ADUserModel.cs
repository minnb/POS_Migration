using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.Authen;


namespace VCM.BLUEPOS.Model.Account
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

        //---------------------------------
        public long? RoleId { get; set; }
        public string RoleName { get; set; }
        //---------------------------------

        // 26/05/2025,tungnt8: login SSO
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string JobTitle { get; set; }
        public string Mail { get; set; }
        public string MobilePhone { get; set; }
        public string OfficeLocation { get; set; }
        public string UserPrincipalName { get; set; }
        public string IdSSO { get; set; }
        public string exp { get; set; }
        public string iss { get; set; }
        public string aud { get; set; }
        public bool? IsSSO { get; set; }
    }

    public class AzureADModel
    {
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string JobTitle { get; set; }
        public string Mail { get; set; }
        public string MobilePhone { get; set; }
        public string OfficeLocation { get; set; }
        public string UserPrincipalName { get; set; }
        public string Id { get; set; }
        public string exp { get; set; }
        public string iss { get; set; }
        public string aud { get; set; }
    }


}
