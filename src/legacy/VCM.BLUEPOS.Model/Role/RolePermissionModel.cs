using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Role
{
    public class CreateUserStoreModel
    {
        public string StaffCode { get; set; }
        public string StoreNo { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool? IsActive { get; set; }
        public string IsAllStore { get; set; }  // 1: xem tất cả Store ; 0: không xem tất cả Store
        public List<string> ListStore { get; set; }
        public bool IsAutoImportStore { get; set; }
        public string MutilStoreAuto { get; set; }
        public string Area { get; set; }
        public string Region { get; set; }
        public string EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string TypeLogin { get; set; }     // Hinh thuc login : 0 : local ; 1 : AD
        public string RoleCode { get; set; }

    }

    public class UpdateUserStoreModel
    {
        public string StaffCode { get; set; }
        public string StoreNo { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string RoleCode { get; set; }
        public bool? IsActive { get; set; }
        public string IsActiveStr { get; set; }
        public string Permission { get; set; } // All : xem tat ca ; 0 : xem theo Store
        public List<string> ListStore { get; set; }
        public string MutilStoreAuto { get; set; }
        public bool IsAutoImportStore { get; set; }
        public string Area { get; set; }
        public string Region { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string EffectiveDateStr { get; set; }
        public string ExpirationDateStr { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsUpdateStore { get; set; }
        public string IsAllStore { get; set; }  // 1: xem tất cả Store ; 0: không xem tất cả Store
        
    }

    public class ViewStoreByUserModel
    {
        public string StaffCode { get; set; }
        public string UserName { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public bool? IsActive { get; set; }
        public string IsActiveStr { get; set; }
        public string Permission { get; set; }  // All : xem tat ca ; 0 : xem theo Store
        public List<string> ListStore { get; set; }
        public bool IsAutoImportStore { get; set; }
        public string MutilStoreAuto { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedBy { get; set; }

    }

    public class OptionStoreByUserModel
    {
        public string StoreNo { get; set; }
    }






    }
