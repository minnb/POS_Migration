using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;

namespace TCX.API.Common.Shared
{
    public static class FormatHelper
    {
        public static DateTime FormatDateTime(DateTime? dateTime)
        {
            try
            {
                if (dateTime != null)
                {
                    return dateTime.Value;
                }
                return DateTime.MinValue;
            }
            catch 
            { 
                return DateTime.MinValue;
            }
        }
        public static string PhoneNumberWithCountryCode(string phoneNumber)
        {
            try
            {
                if (phoneNumber.Length >= 9)
                {
                    return "84" + StringHelper.Right(phoneNumber, 9);
                }
                return phoneNumber;
            }
            catch 
            { 
                return phoneNumber; 
            }
        }
        public static string PhoneNumberVietNam(string phoneNumber)
        {
            try
            {
                if (phoneNumber.Length >= 11 && StringHelper.Left(phoneNumber, 2) == "84")
                {
                    return "0" + phoneNumber.Substring(2, phoneNumber.Length - 2);
                }
                else if (phoneNumber.Length == 9 && phoneNumber.Substring(0, 1) != "0")
                {
                    return "0" + phoneNumber;
                }
                return phoneNumber;
            }
            catch
            {
                return phoneNumber;
            }
        }
        public static string CustomerCapillary(string member, string storeNo)
        {
            return string.Format("{0}-{1}", member, storeNo);
        }
        public static string GetTitleGender(string gender)
        {
            if(!string.IsNullOrEmpty(gender) && gender.ToUpper() == GenderEnum.Male.ToString().ToUpper()) 
            {
                return @"CHỊ";
            }
            else if(!string.IsNullOrEmpty(gender) && gender.ToUpper() == GenderEnum.Female.ToString().ToUpper())
            {
                return @"ANH";
            }
            else
            {
                return "OTHER";
            }
        }
        public static string GetCharGender(string gender)
        {
            if (!string.IsNullOrEmpty(gender) && gender.ToUpper() == GenderEnum.Male.ToString().ToUpper())
            {
                return @"M";
            }
            else if (!string.IsNullOrEmpty(gender) && gender.ToUpper() == GenderEnum.Female.ToString().ToUpper())
            {
                return @"F";
            }
            else
            {
                return "O";
            }
        }
    }
}
