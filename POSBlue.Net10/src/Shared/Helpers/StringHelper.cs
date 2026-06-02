using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace VCM.POSBLUE.Shared.Helpers;

/// <summary>Helper chuỗi (port các method dùng trong partner services của bản cũ).</summary>
public static class StringHelper
{
    public static string Left(string? input, int count)
        => !string.IsNullOrEmpty(input) ? input.Substring(0, Math.Min(input.Length, count)) : "";

    public static string Right(string? input, int count)
        => !string.IsNullOrEmpty(input) && input.Length >= count ? input.Substring(input.Length - count, count) : (input ?? "");

    /// <summary>Serialize object sang JSON camelCase (giữ nguyên ObjectToStringLowercase cũ).</summary>
    public static string ObjectToStringLowercase(object obj)
        => JsonConvert.SerializeObject(obj, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

    public static T? StringToObject<T>(string json)
    {
        try { return string.IsNullOrEmpty(json) ? default : JsonConvert.DeserializeObject<T>(json); }
        catch { return default; }
    }

    /// <summary>Tách chuỗi theo ký tự (giữ nguyên ConverStringToArray cũ).</summary>
    public static string[] ConverStringToArray(string? str, char c)
    {
        if (string.IsNullOrEmpty(str)) return Array.Empty<string>();
        return str.IndexOf(c) != -1
            ? str.Split(c).Where(x => !string.IsNullOrEmpty(x)).ToArray()
            : new[] { str };
    }
}
