namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Abstraction cho cơ chế xác thực hiện tại (Basic Auth + X-API header).
/// GIỮ NGUYÊN logic auth của bản cũ (BasicAuthenticationAttribute):
///  - X-API header được đối chiếu với giá trị cấu hình (so MD5).
///  - Các route chứa "api/v2" yêu cầu Basic Auth username/password.
/// Implementation (đọc DB/cache) nằm ở Infrastructure.
/// </summary>
public interface IPosAuthenticationService
{
    /// <summary>Lấy giá trị X-API plaintext đã cấu hình (từ POS data setup) để so sánh MD5.</summary>
    Task<string?> GetConfiguredXApiAsync();

    /// <summary>Kiểm tra cặp username/password hợp lệ với danh sách SysWebApiUser.</summary>
    Task<bool> ValidateWebApiUserAsync(string username, string password);
}
