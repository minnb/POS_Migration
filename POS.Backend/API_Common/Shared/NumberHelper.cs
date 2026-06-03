using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;

namespace TCX.API.Common.Shared
{
    public static class NumberHelper
    {
        public static int GetTimeOutApi(string timeOut)
        {
            if (!string.IsNullOrEmpty(timeOut))
            {
                return int.Parse(timeOut);
            }
            return 30000;
        }
        public static bool IsPhoneNumber(string phoneNumber)
        {
            if (phoneNumber.Length >= 9 && phoneNumber.Length <= 11 && RegexHelper.IsDigitsOnly(phoneNumber))
            {
                return true;
            }
            return false;
        }
        public static Tuple<bool,string> IsMemberCapillary(string phoneNumber, bool isMobile)
        {
            if (!isMobile && phoneNumber.Length >= 9 && phoneNumber.Length <= 11 && RegexHelper.IsDigitsOnly(phoneNumber))
            {
                return new Tuple<bool, string>(true, MemberCapillaryEnum.PHONE.ToString());
            }
            else if(isMobile && phoneNumber.Length <= 9 && StringHelper.Left(phoneNumber, 1) != "0")
            {
                return new Tuple<bool, string>(true, MemberCapillaryEnum.ID.ToString()); 
            }
            else if (isMobile && phoneNumber.Length >= 9 && phoneNumber.Length <= 11 && RegexHelper.IsDigitsOnly(phoneNumber) && StringHelper.Left(phoneNumber, 1) == "0")
            {
                return new Tuple<bool, string>(true, MemberCapillaryEnum.PHONE.ToString());
            }
            else if(isMobile && phoneNumber.Length == 12)
            {
                return new Tuple<bool, string>(true, MemberCapillaryEnum.WINCARE.ToString());
            }
            else if (isMobile && phoneNumber.Length == 14)
            {
                return new Tuple<bool, string>(true, MemberCapillaryEnum.WINX.ToString());
            }
            return new Tuple<bool, string>(false, MemberCapillaryEnum.NONE.ToString()); ;
        }
        public static double DecimalToSgl_Dbl(decimal argument)
        {  
            return decimal.ToDouble(argument);
        }
        public static double IsNumber(string number)
        {
            if (string.IsNullOrEmpty(number)) return 0;
            var tryParse = double.TryParse(number, out double value);
            return tryParse == true ? value : 0;
        }
        public static int IsInteger(string number)
        {
            if(string.IsNullOrEmpty(number)) return 0;

            var tryParse = int.TryParse(number, out int value);
            return tryParse == true ? value : 0;
        }
        public static long IsLong(string number)
        {
            if (string.IsNullOrEmpty(number)) return 0;

            var tryParse = long.TryParse(number, out long value);
            return tryParse == true ? value : 0;
        }
        public static decimal IsDecimal(string number)
        {
            if (string.IsNullOrEmpty(number)) return 0;

            var tryParse = decimal.TryParse(number, out decimal value);
            return tryParse == true ? value : 0;
        }
        public static decimal TryParseDecimal(string numberString)
        {
            if (string.IsNullOrWhiteSpace(numberString))
            {
                return 0;
            }
            if (decimal.TryParse(numberString, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value))
            {
                return value;
            }
            else
            {
                return 0;
            }
        }
        public static int TryParseInt(string str, int setDefault = 0)
        {
            if (int.TryParse(str, out int result))
            {
                return result;
            }
            else
            {
                return setDefault;
            }
        }
        public static long TryParseInt64(string str)
        {
            if (Int64.TryParse(str, out long result))
            {
                return result;
            }
            else
            {
                return 0;
            }
        }
        public static T ConvertScientificToNumberV1<T>(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return default;

            try
            {
                if (input.IndexOfAny(new char[] { '.', 'E', 'e' }) == -1)
                {
                    if (long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longVal))
                    {
                        return (T)Convert.ChangeType(longVal, typeof(T));
                    }
                }

                double tempDouble = double.Parse(input, NumberStyles.Float, CultureInfo.InvariantCulture);
                return (T)Convert.ChangeType(tempDouble, typeof(T));
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("ConvertScientificToNumber", ex);
                return default;
            }
        }
        public static T ConvertScientificToNumber<T>(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return default;

            try
            {
                input = input.Trim().ToUpper();
                decimal result;

                if (input.Contains("E"))
                {
                    string[] parts = input.Split('E');
                    decimal mantissa = decimal.Parse(parts[0], CultureInfo.InvariantCulture);
                    int exponent = int.Parse(parts[1], CultureInfo.InvariantCulture);
                    decimal multiplier = (decimal)Math.Pow(10, Math.Abs(exponent));

                    if (exponent >= 0)
                        result = mantissa * multiplier;
                    else
                        result = mantissa / multiplier;
                }
                else
                {
                    result = decimal.Parse(input, CultureInfo.InvariantCulture);
                }

                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("ConvertScientificToNumber.Exception:", ex);
                return default;
            }
        }
    }
}
