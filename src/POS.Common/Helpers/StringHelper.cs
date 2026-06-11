using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace POS.Common.Helpers;

public static class StringHelper
{
    public static JObject? StringToJObject(string json)
    {
        try
        {
            if (!string.IsNullOrEmpty(json))
            {
                //var jObject = JObject.Parse(consumeResult.Message.Value.Replace("'", ""));
                return JObject.Parse(json);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
    public static string Uppering(string str)
    {
        if (!string.IsNullOrEmpty(str))
            return str.ToUpper();
        return str;
    }

    public static string RandomRequestID(string posNo, bool isRandom = false)
    {
        if (isRandom)
        {
            var randomInt = new Random();
            return posNo + DateTimeHelper.UnixTimestampString() + randomInt.Next(0, 99).ToString("D2");
        }
        return posNo + DateTimeHelper.UnixTimestampString();
    }

    public static string InitRandomString(string prefix)
    {
        var randomInt = new Random();
        if (!string.IsNullOrEmpty(prefix))
            return prefix + "_" + randomInt.Next(10, 99) + DateTime.Now.ToString("yMMddHHmmssfff");
        return randomInt.Next(10, 99) + DateTime.Now.ToString("yMMddHHmmssfff");
    }

    public static string CreateKeyRedis(string appCode, string key)
    {
        return appCode + ":" + key;
    }

    public static string ReplaceString(string str, string oldValue, string newValue)
    {
        try
        {
            if (!string.IsNullOrEmpty(str))
                return str.Replace(oldValue, newValue);
            return str;
        }
        catch
        {
            return str;
        }
    }

    public static string FormatNumberString(int value)
    {
        return value.ToString("N0");
    }

    public static string FormatDate_yyyyMMdd(string date)
    {
        try
        {
            return DateTime.Parse(date).ToString("yyyy-MM-dd");
        }
        catch
        {
            return date;
        }
    }

    public static bool ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return false;
        if (phoneNumber.Length < 9 || phoneNumber.Length > 11)
            return false;
        if (phoneNumber[0] != '0')
            return false;
        foreach (char c in phoneNumber)
        {
            if (!char.IsDigit(c))
                return false;
        }
        return true;
    }

    public static string Left(string input, int count)
    {
        if (!string.IsNullOrEmpty(input))
            return input.Substring(0, Math.Min(input.Length, count));
        return "";
    }

    public static string Right(string input, int count)
    {
        if (!string.IsNullOrEmpty(input))
            return input.Substring(input.Length - count, count);
        return input;
    }

    public static string FormartDate(string strDate, char fromChar, char toChar, string formatDate)
    {
        try
        {
            var arr = strDate.Split(fromChar);
            if (arr.Length > 0 && formatDate == "yyyy-MM-dd")
                return arr[2] + toChar.ToString() + arr[1] + toChar + arr[0];
            return strDate;
        }
        catch
        {
            return strDate;
        }
    }

    public static string IsNull(string str, string strDefault = "")
    {
        if (!string.IsNullOrEmpty(str))
            return str;
        return strDefault;
    }

    public static string[] ConverStringToArray(string str, char c)
    {
        try
        {
            if (string.IsNullOrEmpty(str)) return [];

            if (str.IndexOf(c) != -1)
            {
                var arr = str.Split(c);
                var list = new List<string>();
                foreach (var item in arr)
                {
                    if (!string.IsNullOrEmpty(item))
                        list.Add(item);
                }
                return list.ToArray();
            }
            return [];
        }
        catch
        {
            return [];
        }
    }

    public static string ConvertArrayToString(string[] arr, char c)
    {
        var data = new System.Text.StringBuilder();
        foreach (var a in arr)
        {
            if (!string.IsNullOrEmpty(a))
                data.Append(a + c);
        }
        if (data.Length > 0)
            data.Remove(data.Length - 1, 1);
        return data.ToString();
    }

    public static string ObjectToStringLowercase(object obj)
    {
        var serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        return JsonConvert.SerializeObject(obj, serializerSettings);
    }

    public static T? StringToObject<T>(string json)
    {
        try
        {
            if (!string.IsNullOrEmpty(json))
                return JsonConvert.DeserializeObject<T>(json);
            return (T?)Convert.ChangeType(null, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}
