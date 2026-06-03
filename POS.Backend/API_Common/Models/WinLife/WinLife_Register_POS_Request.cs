using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TCX.API.Common.Attribute;

namespace VCM.POSBLUE.Model.WinLife
{
    public class WinLife_Register_POS_Request
    {
        [Required]
        public string fullName { get; set; }//
        [Required]
        public string title { get; set; }//07/03/2023 ---+ Anh (ANH) + Chị (CHI) + Chú(CHU) + Cô(CO)
        [Required]
        [MinLength(9)]
        public string phoneNo { get; set; }
        [Required]
        [StringRange(AllowableValues = new[] { "F", "M"}, ErrorMessage = "Giới tính không đúng F/M")]
        public string gender { get; set; }//F,M
        public string otp { get; set; }
        public string userPOS { get; set; }
        [Required]
        [StringLength(6, MinimumLength = 4)]
        public string storeCode { get; set; }
        [Required]
        public string posCode { get; set; }
        public string dob { get; set; }
        public string identifierCardid { get; set; }//Số CMND/CCCD của khách hàng
        public string issuedDate { get; set; }//Ngày cấp(yyyy-MM-dd)
        public string issuedPlace { get; set; }//Nơi cấp

        [StringRange(AllowableValues = new[] { "REGISTER", "TEMP", "CAP", "", null }, ErrorMessage = "Giá trị Status đăng ký không đúng")]
        [DefaultValue("REGISTER")]
        public string status { get; set; } = "REGISTER";
        public string email { get; set; } 
        public string address { get; set; }
        public string province { get; set; }
        public string district { get; set; }
        public string city { get; set; }
        public string ward { get; set; }
    }
    public class WinLife_Register_CX_Request
    {
        public string fullName { get; set; }//
        public string title { get; set; }//07/03/2023 ---+ Anh (ANH) + Chị (CHI) + Chú(CHU) + Cô(CO)
        public string phoneNo { get; set; }
        public string gender { get; set; }//F,M
        public string merchantId { get; set; }//WCM
        public string otp { get; set; }
        public string dob { get; set; }
        public string storeNo { get; set; }
        public string source { get; set; }//POS
        public string staffEntityId { get; set; }//WCM
        public string refStaffId { get; set; }//POS truyền UserPOS
        public string identifierCardid { get; set; }//Bo Sung Ngay 26/09/2022
        public string issuedDate { get; set; }//Bo Sung Ngay 26/09/2022
        public string issuedPlace { get; set; }//Bo Sung Ngay 26/09/2022
    }
}
