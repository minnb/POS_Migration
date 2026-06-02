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

    /// <summary>
    /// Xác định số thẻ/SĐT có phải hội viên Capillary không và trả về loại member.
    /// Port từ NumberHelper.IsMemberCapillary cũ.
    /// </summary>
    public static (bool IsCapillary, string MemberType) IsMemberCapillary(string phoneNumber, bool isMobile)
    {
        if (!isMobile && phoneNumber.Length >= 9 && phoneNumber.Length <= 11 && phoneNumber.All(char.IsDigit))
            return (true, "PHONE");
        if (isMobile && phoneNumber.Length <= 9 && StringHelper.Left(phoneNumber, 1) != "0")
            return (true, "ID");
        if (isMobile && phoneNumber.Length >= 9 && phoneNumber.Length <= 11 && phoneNumber.All(char.IsDigit) && StringHelper.Left(phoneNumber, 1) == "0")
            return (true, "PHONE");
        if (isMobile && phoneNumber.Length == 12)
            return (true, "WINCARE");
        if (isMobile && phoneNumber.Length == 14)
            return (true, "WINX");
        return (false, "");
    }
}
