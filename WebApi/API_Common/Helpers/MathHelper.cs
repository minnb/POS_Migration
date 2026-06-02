using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Helpers
{
    public static class MathHelper
    {
        public static bool IsValidPointRedeem(int point, int rate)
        {
            return point% rate == 0;
        }
        public static bool IsDivisible(long a, long b)
        {
            if (b == 0) return false;
            return a % b == 0;
        }
        public static double IsNumber(string number)
        {
            var tryParse = double.TryParse(number, out double value);
            return tryParse == true ? value : 0;
        }
        public static decimal IsDecimal(string number)
        {
            var tryParse = decimal.TryParse(number, out decimal value);
            return tryParse == true ? value : 0;
        }
        public static int IsInteger(string number)
        {
            var tryParse = int.TryParse(number, out int value);
            return tryParse == true ? value : 0;
        }
    }
}
