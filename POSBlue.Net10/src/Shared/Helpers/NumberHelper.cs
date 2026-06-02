namespace VCM.POSBLUE.Shared.Helpers;

/// <summary>Helper số (port các method dùng trong partner services của bản cũ).</summary>
public static class NumberHelper
{
    /// <summary>Timeout API (giây) lấy từ field Version của SysWebApi; mặc định 30000.</summary>
    public static int GetTimeOutApi(string? timeOut)
        => !string.IsNullOrEmpty(timeOut) && int.TryParse(timeOut, out var v) ? v : 30000;

    public static bool IsPhoneNumber(string? phoneNumber)
        => !string.IsNullOrEmpty(phoneNumber)
           && phoneNumber.Length >= 9 && phoneNumber.Length <= 11
           && phoneNumber.All(char.IsDigit);
}
