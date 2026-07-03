using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Employee
{
    public class EmployeeListResponseModel
    {
        public int ID { get; set; }
        public string StaffCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string Password { get; set; }
        public string VoidTransaction { get; set; }           // Quyen huy bill
        public string TypeGroup { get; set; }                 // Nhom loai
        public string PermissionGroup { get; set; }           // Nhom quyen
        public string Status { get; set; }                    // Tình trạng hoạt động
        public string HomePhoneNo { get; set; }
        //public string CodeSAP { get; set; }                 // Mã nhân viên có tính KPI
        //public string KPI { get; set; }                     // Nếu checkbo KPI = true thì lưu mã nhân viên vào column SalesPerson ; Ngược lại Clear data -- */
        public string LastDateModifiedStr { get; set; }
        public long? Counter { get; set; }
        //public string Pkey { get; set; }
        public int Total { get; set; }
    }

    public class ExportEmployeeListModel
    { 
        public string StaffCode { get; set; }
        public string FirstName { get; set; }
        public string Status { get; set; }         // Tình trạng hoạt động
        public string Password { get; set; }
        public string VoidTransaction { get; set; }
        public string TypeGroup { get; set; }
        public string PermissionGroup { get; set; }
        public string HomePhoneNo { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string LastDateModifiedStr { get; set; }
    }

    public class CheckEmployeeModel
    {
        public string StaffCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string Password { get; set; }
        public string VoidTransaction { get; set; }
        public string EmploymentType { get; set; }
        public DateTime? LastDateModified { get; set; }
        public string LastDateModifiedStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public bool Status { get; set; }
        public string Message { get; set; }
    }

    public class CreateEmployeeListModel
    {
        public string StaffCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string PassWord { get; set; }
        public string VoidTransaction { get; set; }           // Quyen huy bill
        public string TypeGroup { get; set; }                 // Nhom loai
        public string PermissionGroup { get; set; }           // Nhom quyen
        public string Status { get; set; }                    // Tình trạng hoạt động
        public string HomePhoneNo { get; set; }
        public string LastDateModifiedStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }

    }
    public class UpdateEmployeeListModel
    {
        public string StaffCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string PassWord { get; set; }
        public string VoidTransaction { get; set; }           // Quyen huy bill
        public string TypeGroup { get; set; }                 // Nhom loai
        public string PermissionGroup { get; set; }           // Nhom quyen
        public string Status { get; set; }                    // Tình trạng hoạt động
        public string HomePhoneNo { get; set; }
        public string LastDateModifiedStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
       
    }

    public class DeleteEmployeeListModel
    {
        public string StaffCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string PassWord { get; set; }
        public string VoidTransaction { get; set; }           // Quyen huy bill
        public string TypeGroup { get; set; }                 // Nhom loai
        public string PermissionGroup { get; set; }           // Nhom quyen
        public string Status { get; set; }                    // Tình trạng hoạt động
        public string HomePhoneNo { get; set; }
        public string LastDateModifiedStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class  StaffCodeResponseModel
    {
        public string UserPOS { get; set; }
        public int? EmploymentType { get; set; }
    }

    public class StaffModel
    {
        public string ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? EmploymentType { get; set; }
        public string StoreNo { get; set; }
        public string Password { get; set; }

    }





}
