namespace VCM.POSBLUE.Shared.Constants;

/// <summary>Thông báo cấu hình (giữ nguyên từ MessConst cũ).</summary>
public static class MessConst
{
    public const string NotFounDataConfig = "Không tìm thấy thông tin cấu hình";
    public const string ApiNotLogin = "Đăng nhập API không thành công";
    public const string ApiError = "Lỗi API ";
}

/// <summary>Thông báo WebApi (giữ nguyên từ MessageConst cũ).</summary>
public static class MessageConst
{
    public const string ApiNotFounDataConfig = "WebApi chưa được cấu hình - Vui lòng liên hệ IT";
    public const string ApiNotAuthorization = "Lỗi xác thực WebApi - Vui lòng liên hệ IT";
    public const string ApiError = "Lỗi kết nối WebApi - Vui lòng liên hệ IT";
    public const string ApiServiceUnavailable = "503 Service Unavailable";
}
