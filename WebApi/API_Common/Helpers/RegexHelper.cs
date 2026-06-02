using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TCX.API.Common.Helpers
{
    public static class RegexHelper
    {
        public static bool IsCharacters(string str)
        {
            var regexItem = new Regex("^[a-zA-Z0-9 ]*$");
            return regexItem.IsMatch(str);
        }
        public static bool IsDigitsOnly(string str)
        {
            var regex = new Regex("^[0-9]*$");
            return regex.IsMatch(str);
        }
    }
}
