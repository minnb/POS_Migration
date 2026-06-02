using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VCM.POSBLUE.API.Models
{   
    public static class ErrorCXVoucherGenerate
    {
        public const string E000 = "E000";//Tạo mới OTP thành công
        public const string E001 = "E001";//codevalue không để trống
        public const string E002 = "E002";//codevalue không tồn tại
        public const string E003 = "E003";//codeValue không hợp lệ
    }
    public static class ErrorCXVoucherGet
    {
        public const string E000 = "E000";//Tạo mới OTP thành công
        public const string E001 = "E001";//codeValue không được để trống
        public const string E002 = "E002";//codeValue không tồn tại
        public const string E003 = "E003";//codeValue không hợp lệ
        public const string E004 = "E004";//otp không được để trống
        public const string E005 = "E005";//otp không tồn tại hoặc hết thời gian sử dụng
        public const string E006 = "E006";//otp không hợp lệ
    }
}