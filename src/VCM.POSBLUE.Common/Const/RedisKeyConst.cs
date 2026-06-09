using System.Text.RegularExpressions;
using VCM.POSBLUE.Common.Helpers;

namespace VCM.POSBLUE.Common.Const;

public static class RedisKeyConst
{
    public static string GetShardKeyPrefix(string key, string hashField, int charNumber = 1)
    {
        try
        {
            if (string.IsNullOrEmpty(hashField))
                return $"{key}:data";

            var prefix = StringHelper.Left(hashField, charNumber).Trim();
            if (Regex.IsMatch(prefix, @"^\d+$"))
                return $"{key}:{prefix}";

            return $"{key}:data";
        }
        catch
        {
            return $"{key}:data";
        }
    }
}
