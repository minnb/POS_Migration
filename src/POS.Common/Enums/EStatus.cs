using System.ComponentModel;

namespace POS.Common.Enums;

public enum EStatus
{
    [Description("Lấy dữ liệu thành công")]
    Success = 1,

    [Description("Lỗi server")]
    Fail = 0,
}
