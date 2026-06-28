using System.Reflection;
using Newtonsoft.Json;

namespace POS.ContractTests;

/// <summary>
/// Tiện ích trích tên field JSON "hiệu dụng" của một type theo đúng cách
/// Newtonsoft.Json serialize: tôn trọng [JsonProperty("...")] và bỏ [JsonIgnore].
///
/// Mục đích: khoá hợp đồng JSON với 5.000 máy POS. Mọi thay đổi vô tình tên field
/// (rename property, đổi/xoá [JsonProperty], xoá field) sẽ làm test đỏ NGAY.
/// </summary>
internal static class JsonContract
{
    /// <summary>Tập tên field JSON hiệu dụng (đã sort) cho <paramref name="type"/>.</summary>
    public static string[] EffectiveFieldNames(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .Select(p => p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? p.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }
}
