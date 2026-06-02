using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Constants
{
    public static class MessageConst
    {
        public static readonly string ApiNotFounDataConfig = @"WebApi chưa được cấu hình - Vui lòng liên hệ IT";
        public static readonly string ApiNotAuthorization = @"Lỗi xác thực WebApi - Vui lòng liên hệ IT";
        public static readonly string ApiError = @"Lỗi kết nối WebApi - Vui lòng liên hệ IT";
        public static readonly string ApiServiceUnavailable = @"503 Service Unavailable";
        public static readonly string ApiServiceConvertJsonError = @"Lỗi convert Json response sang dữ liệu object";
    }
}
