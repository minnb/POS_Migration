namespace VCM.POSBLUE.Shared.Helpers;

/// <summary>Helper định dạng số điện thoại (giữ nguyên logic FormatHelper cũ).</summary>
public static class FormatHelper
{
    /// <summary>Chuẩn hoá SĐT có mã quốc gia: "84" + 9 số cuối.</summary>
    public static string PhoneNumberWithCountryCode(string phoneNumber)
    {
        try { return phoneNumber.Length >= 9 ? "84" + StringHelper.Right(phoneNumber, 9) : phoneNumber; }
        catch { return phoneNumber; }
    }

    /// <summary>Chuẩn hoá SĐT Việt Nam (bắt đầu bằng 0).</summary>
    public static string PhoneNumberVietNam(string phoneNumber)
    {
        try
        {
            if (phoneNumber.Length >= 11 && StringHelper.Left(phoneNumber, 2) == "84")
                return "0" + phoneNumber.Substring(2);
            if (phoneNumber.Length == 9 && phoneNumber.Substring(0, 1) != "0")
                return "0" + phoneNumber;
            return phoneNumber;
        }
        catch { return phoneNumber; }
    }
}
