using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;

namespace VCM.BLUEPOS.Helpers
{
    public static class DateTimeExtension
    {
        // Convert datetime to UNIX time
        public static string ToUnixTime(this DateTime dateTime)
        {
            DateTimeOffset dto = new DateTimeOffset(dateTime.ToUniversalTime());
            return dto.ToUnixTimeSeconds().ToString();
        }

        // Convert datetime to UNIX time including miliseconds
        public static string ToUnixTimeMilliSeconds(this DateTime dateTime)
        {
            DateTimeOffset dto = new DateTimeOffset(dateTime.ToUniversalTime());
            return dto.ToUnixTimeMilliseconds().ToString();
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

    }

}