using POS.Common.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace POS.Common.Dtos.Loyalty
{
    public class RegisterMemberDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required]
        public string Title { get; set; } = string.Empty;//07/03/2023 ---+ Anh (ANH) + Chị (CHI) + Chú(CHU) + Cô(CO)
        [Required]
        [MinLength(9)]
        public string PhoneNo { get; set; } = string.Empty;
        [Required]
        //[StringRange(AllowableValues = new[] { "F", "M" }, ErrorMessage = "Giới tính không đúng F/M")]
        public string Gender { get; set; } = string.Empty;//F,M
        public string Otp { get; set; } = string.Empty;
        public string UserPOS { get; set; } = string.Empty;
        [Required]
        [StringLength(6, MinimumLength = 4)]
        public string StoreCode { get; set; } = string.Empty;
        [Required]
        public string PosCode { get; set; } = string.Empty;
        public string Dob { get; set; } = string.Empty;
        public string IdentifierCardid { get; set; } = string.Empty;//Số CMND/CCCD của khách hàng
        public string IssuedDate { get; set; } = string.Empty;//Ngày cấp(yyyy-MM-dd)
        public string IssuedPlace { get; set; } = string.Empty;//Nơi cấp

        //[StringRange(AllowableValues = new[] { "REGISTER", "TEMP", "CAP", "", null }, ErrorMessage = "Giá trị Status đăng ký không đúng")]
        [DefaultValue("REGISTER")]
        public string Status { get; set; } = "REGISTER";
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
    }
}
