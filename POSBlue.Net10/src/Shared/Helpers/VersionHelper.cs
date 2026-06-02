using System.Reflection;

namespace VCM.POSBLUE.Shared.Helpers;

/// <summary>
/// Lấy thông tin version/ngày build của assembly đang chạy.
/// Thay thế MemoryCacheService.GetVersion("bin\\VCM.POSBLUE.API.dll") của bản cũ
/// (vốn đọc ngày sửa đổi file DLL).
/// </summary>
public static class VersionHelper
{
    public static string GetAssemblyModifiedDate()
    {
        try
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var location = asm.Location;
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
            {
                return File.GetLastWriteTime(location).ToString("yyyy-MM-dd HH:mm:ss");
            }
            return asm.GetName().Version?.ToString() ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}
