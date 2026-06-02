using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Enums
{
    public enum EStatus
    {
        [Description("Lấy dữ liệu thành công")]
        Success = 1,

        /// <summary>
        /// Status request fail.
        /// </summary>
        [Description("Lỗi server")]
        Fail = 0,
    }
}
