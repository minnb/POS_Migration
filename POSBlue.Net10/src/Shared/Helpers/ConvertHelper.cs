using Newtonsoft.Json;

namespace VCM.POSBLUE.Shared.Helpers;

/// <summary>Helper convert JSON ↔ object (giữ nguyên ConvertHelper.ToObject cũ, dùng Newtonsoft).</summary>
public static class ConvertHelper
{
    public static T? ToObject<T>(string json)
    {
        if (string.IsNullOrEmpty(json)) return default;
        try { return JsonConvert.DeserializeObject<T>(json); }
        catch { return default; }
    }
}
