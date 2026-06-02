namespace VCM.POSBLUE.Shared.Helpers;

/// <summary>Helper thời gian (port các method dùng trong Redis/Queue của bản cũ).</summary>
public static class DateTimeHelper
{
    /// <summary>Số giây từ hiện tại đến nửa đêm (dùng làm TTL mặc định cho cache theo ngày).</summary>
    public static long GetSecondsUntilMidnight()
    {
        var now = DateTime.Now;
        var midnight = now.Date.AddDays(1);
        return (long)(midnight - now).TotalSeconds;
    }

    /// <summary>Unix timestamp (giây) dạng chuỗi.</summary>
    public static string UnixTimestampString() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    /// <summary>Mốc nửa đêm kế tiếp (dùng cho AbsoluteExpiration của memory cache) — thay GetMidnightOffset cũ.</summary>
    public static DateTimeOffset GetMidnightOffset() => new(DateTime.Now.Date.AddDays(1));
}
