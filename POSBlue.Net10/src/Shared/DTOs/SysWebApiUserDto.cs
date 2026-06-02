namespace VCM.POSBLUE.Shared.DTOs;

/// <summary>
/// Read model cho bảng SysWebApiUser (giữ nguyên từ TCX.API.Common.Dtos.SysWebApiUserDto).
/// Dùng cho Basic Authentication.
/// </summary>
public class SysWebApiUserDto
{
    public string? AppCode { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? Description { get; set; }
    public string? Authorization { get; set; }
    public bool Blocked { get; set; }
}
