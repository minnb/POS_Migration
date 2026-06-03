using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Helpers
{
    public static class ParseHelper
    {
        public static int ToPowerOfTenFromDecimalPlaces(decimal value, bool normalize = true)
        {
            if (value == 0m)
                throw new ArgumentOutOfRangeException(nameof(value), "Value must not be 0.");

            value = Math.Abs(value);

            if (normalize)
                value = decimal.Parse(value.ToString("0.#############################")); // trim trailing zeros

            int[] bits = decimal.GetBits(value);
            int scale = (bits[3] >> 16) & 0xFF; // number of decimal digits (0..28)

            // compute 10^scale as int (scale up to 9 is safe for int; bigger may overflow)
            if (scale > 9)
                throw new OverflowException($"Scale={scale} too large for int.");

            int result = 1;
            for (int i = 0; i < scale; i++)
                result *= 10;

            return result;
        }

        public static int IntOrZero(string value)
        {
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }

        public static long LongOrZero(string value)
        {
            if (long.TryParse(value, out long result))
                return result;
            return 0;
        }

        public static double DoubleOrZero(string value)
        {
            if (double.TryParse(value, out double result))
                return result;
            return 0;
        }

        public static decimal DecimalOrZero(string value)
        {
            if (decimal.TryParse(value, out decimal result))
                return result;
            return 0;
        }

        public static float FloatOrZero(string value)
        {
            if (float.TryParse(value, out float result))
                return result;
            return 0;
        }

        public static DateTime DateTimeOrMin(string value)
        {
            if (DateTime.TryParse(value, out DateTime result))
                return result;
            return DateTime.MinValue;
        }

        public static bool BoolOrFalse(string value)
        {
            if (bool.TryParse(value, out bool result))
                return result;
            return false;
        }

    }
}
