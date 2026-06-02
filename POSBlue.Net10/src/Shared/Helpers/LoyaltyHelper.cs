using VCM.POSBLUE.Application.Interfaces;

namespace VCM.POSBLUE.Shared.Helpers;

/// <summary>Helper Loyalty — port từ TCX.WebApiCore.LoyaltyHelper và TCX.API.Common.Helpers.LoyaltyHelper cũ.</summary>
public static class LoyaltyHelper
{
    /// <summary>
    /// Thẻ VINID: bắt đầu "8888"/"66668" + 16 ký tự, HOẶC bắt đầu "P" và KHÔNG phải 14 ký tự.
    /// Port nguyên từ LoyaltyHelper.IsVINID cũ.
    /// </summary>
    public static bool IsVINID(string memberCard)
    {
        if (string.IsNullOrEmpty(memberCard)) return false;
        if ((StringHelper.Left(memberCard, 4) == "8888" || StringHelper.Left(memberCard, 5) == "66668") && memberCard.Length >= 16)
            return true;
        if (StringHelper.Left(memberCard, 1).ToUpper() == "P" && memberCard.Length != 14)
            return true;
        return false;
    }

    /// <summary>Thông báo số thẻ/SĐT không hợp lệ.</summary>
    public static string MessageNotValidPhone(string phoneNumber)
        => $"Số thẻ {phoneNumber} không hợp lệ";

    /// <summary>
    /// Kiểm tra VINID API có đang tắt (NotUsed) cho function cụ thể không.
    /// Đọc từ SysWebApiRoute của AppCode="VINID": nếu Route == "NotUsed" → off, Notes là thông báo.
    /// </summary>
    public static (bool IsOff, string Message) IsOffVinID(IMemoryCacheService cache, string function)
    {
        try
        {
            var cfgList = cache.GetSysWebApi(cache.GetConnectStringCentralMD());
            var cfg = cfgList?.FirstOrDefault(x => string.Equals(x.AppCode, "VINID", StringComparison.OrdinalIgnoreCase));
            if (cfg?.SysWebApiRoute != null)
            {
                var route = cfg.SysWebApiRoute.FirstOrDefault(x => string.Equals(x.Name, function, StringComparison.OrdinalIgnoreCase));
                if (route?.Route?.Equals("NotUsed", StringComparison.OrdinalIgnoreCase) == true)
                    return (true, route.Notes ?? "Tính năng tạm dừng");
            }
        }
        catch { /* không chặn luồng chính */ }
        return (false, "OK");
    }

    /// <summary>Kiểm tra store có phải HF (không được tích/tiêu VINID) không.</summary>
    public static bool IsHFStore(IMemoryCacheService cache, string storeNo)
    {
        try
        {
            return cache.GetStores()?.Any(x => x.No == storeNo && x.BusinessAreaNo == "HF") == true;
        }
        catch { return false; }
    }

    /// <summary>Kiểm tra store có phải WINCARE không (dùng Capillary thay vì CX).</summary>
    public static bool IsWincareStore(IMemoryCacheService cache, string storeNo)
    {
        try
        {
            return cache.GetStores()?.Any(x => x.No == storeNo && x.PostCode == "WINCARE") == true;
        }
        catch { return false; }
    }
}
