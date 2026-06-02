using System.Security.Cryptography;
using System.Text;

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
}
