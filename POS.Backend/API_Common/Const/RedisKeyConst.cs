using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI;
using TCX.API.Common.Constants;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;

namespace TCX.API.Common.Const
{
    public static class RedisKeyConst
    {
        public static string GetShardKeyPrefix(string key, string hashField, int charNumber = 1)
        {
            try
            {
                if (string.IsNullOrEmpty(hashField))
                {
                    return $"{key}:data";
                }

                var prefix = StringHelper.Left(hashField, charNumber).Trim();
                if (Regex.IsMatch(prefix, @"^\d+$"))
                {
                    return $"{key}:{prefix}";
                }

                return $"{key}:data";
            }
            catch
            {
                return $"{key}:data";
            }
        }
    }
}
