using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace VCM.POSBLUE.Shared.Helpers;

/// <summary>
/// Helper mã hóa/giải mã. Giữ NGUYÊN thuật toán của bản cũ
/// (TCX.API.Common.Utils.EncryptionUtil + TCX.API.Common.Shared.EncryptionHelper)
/// để đảm bảo X-API hash và Basic Auth tương thích 100% với client hiện tại.
/// </summary>
public static class EncryptionHelper
{
    /// <summary>MD5 hash, output hex hoa (X2). Input encode bằng ASCII — giống bản cũ.</summary>
    public static string GetMd5Hash(string input)
    {
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    /// <summary>Mã hóa chuỗi thành Base64 (UTF8).</summary>
    public static string Base64Encode(string input) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(input));

    /// <summary>Giải mã chuỗi Base64 (UTF8). Trả null nếu lỗi — giống bản cũ.</summary>
    public static string? Base64Decode(string plainText)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(plainText));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Ký RSA SHA256 dữ liệu plainText bằng private key dưới dạng XML string.
    /// Port từ VINIDHelper.VinPaySignature + VINIDHelper.SignMessage cũ.
    /// Private key XML lưu trong SysWebApi.PrivateKey (format RSA XML chuẩn .NET: &lt;RSAKeyValue&gt;...&lt;/RSAKeyValue&gt;).
    /// Trả chuỗi Base64 signature, hoặc "" nếu lỗi.
    /// </summary>
    public static string VinPaySignature(string plainTextData, string privateKeyXml)
    {
        if (string.IsNullOrEmpty(plainTextData) || string.IsNullOrEmpty(privateKeyXml))
            return string.Empty;
        try
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml);
            var bytes = Encoding.UTF8.GetBytes(plainTextData);
            return Convert.ToBase64String(rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }
        catch
        {
            return string.Empty;
        }
    }
}
