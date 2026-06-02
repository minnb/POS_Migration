using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Helpers
{
    public static class DataTypeHelper
    {
        public static T GetValueDictionary<T>(Dictionary<string, T> dict, string key) where T : class
        {
            try
            {
                if (dict.TryGetValue(key, out T value))
                {
                    return value;
                }
            }
            catch
            {
            }
            return null;
        }
    }
}
