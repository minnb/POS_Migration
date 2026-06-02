namespace VCM.POSBLUE.Shared.Enums;

/// <summary>Mã hệ thống đối tác (giữ nguyên tên member từ AppCodeEnum cũ — dùng .ToString()).</summary>
public enum AppCodeEnum
{
    CX,
    CAPILLARY,
    WINLIFE,
    WINCUSTOMER,
    VINID,
}

/// <summary>Action OTP của CrownX (giữ nguyên tên member từ CXEnum cũ).</summary>
public enum CXEnum
{
    WIN_MEMBER_REGISTER,
    WIN_MEMBER_UPDATE,
}

/// <summary>Loại token Authorization header.</summary>
public enum TokenTypeEnum
{
    Basic,
    Bearer,
}

/// <summary>Loại message SMS/notify.</summary>
public enum MessageTypeEnum
{
    Info,
    Warning,
    Error,
}
