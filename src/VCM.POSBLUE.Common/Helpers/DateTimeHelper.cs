using System.Globalization;

namespace VCM.POSBLUE.Common.Helpers;

public static class DateTimeHelper
{
    public static bool TryParseToDateTime(string input, string inputFormat, out DateTime dateTime)
    {
        return DateTime.TryParseExact(
            input,
            inputFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out dateTime
        );
    }

    public static string? ConvertToIsoFormat(string inputDateTimeString, string inputFormat, string outputFormat)
    {
        if (!TryParseToDateTime(inputDateTimeString, inputFormat, out var dt))
            return null;
        return dt.ToString("yyyy-MM-ddTHH:mm:ss");
    }

    public static DateTime StringToDateTime(string dateTimeString, string format)
    {
        if (string.IsNullOrWhiteSpace(dateTimeString))
            return new DateTime(1900, 1, 1);
        if (DateTime.TryParseExact(dateTimeString, format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime result))
            return result;
        return new DateTime(1900, 1, 1);
    }

    public static DateTime ConvertStrToDate(string input, string format)
    {
        return DateTime.ParseExact(input, format, CultureInfo.InvariantCulture);
    }

    public static TimeSpan GetExpiryTimeLastMonth()
    {
        var now = DateTimeOffset.UtcNow;
        var lastDayOfMonth = new DateTimeOffset(
            now.Year,
            now.Month,
            DateTime.DaysInMonth(now.Year, now.Month),
            0, 0, 0,
            TimeSpan.Zero);
        if (now >= lastDayOfMonth)
            lastDayOfMonth = lastDayOfMonth.AddMonths(1);
        return lastDayOfMonth - now;
    }

    public static long GetTotalSecondMidnight(int numberDate = 1)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        DateTimeOffset dayAfterTomorrowMidnight = now.Date.AddDays(numberDate);
        TimeSpan difference = dayAfterTomorrowMidnight - now;
        return (long)difference.TotalSeconds;
    }

    public static long GetSecondsUntilMidnight()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        DateTimeOffset tomorrowMidnight = now.Date.AddDays(1);
        return (long)(tomorrowMidnight - now).TotalSeconds;
    }

    public static DateTimeOffset GetMidnightOffset()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        return now.AddDays(1).Date;
    }

    public static TimeSpan GetTimeUntilHourInTomorrow(int hour)
    {
        DateTime now = DateTime.Now;
        DateTime tomorrowHour = now.AddDays(1).Date.AddHours(hour);
        return tomorrowHour - now;
    }

    public static TimeSpan GetTimeUntilMidnight()
    {
        DateTime now = DateTime.Now;
        DateTime tomorrow = now.AddDays(1).Date;
        return tomorrow - now;
    }

    public static string GetNullableString(this DateTime? time)
    {
        return time.HasValue ? time.ToString()! : "NULL";
    }

    public static string UnixTimestampString(int seconds = 0)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (seconds > 0)
            now = now.AddSeconds(seconds);
        return now.ToUnixTimeSeconds().ToString();
    }

    public static int GetMinuteElapsed(DateTime fromDateTime, DateTime toDateTime)
    {
        TimeSpan khoangThoiGian = toDateTime - fromDateTime;
        return (int)khoangThoiGian.TotalMinutes;
    }
}
