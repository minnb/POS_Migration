using System.Security.Cryptography;
using System.Text;

namespace POS.Api.Middleware;

/// <summary>
/// Tính toán/so sánh checksum xác thực request POS (SHA-256 của
/// RequestId|Timestamp|Secret+PosNo) và kiểm tra cửa sổ thời gian hợp lệ. Tách riêng khỏi
/// <see cref="PosApiKeyMiddleware"/> để unit test được trực tiếp, không cần Redis/DB/HttpContext.
/// </summary>
public static class PosApiKeyChecksum
{
    /// <summary>Tính SHA-256 hex (uppercase) của "{requestId}|{timestampUnixSeconds}|{secret}{posNo}".</summary>
    public static string Compute(string requestId, long timestampUnixSeconds, string secret, string posNo)
    {
        var raw = $"{requestId}|{timestampUnixSeconds}|{secret}{posNo}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    /// <summary>
    /// So sánh 2 chuỗi hex checksum bằng thời gian cố định (chống timing attack). Không throw khi
    /// input không phải hex hợp lệ — trả false. Độ dài khác nhau vẫn chạy FixedTimeEquals trên
    /// buffer cùng kích thước giả để tránh lộ thông tin độ dài qua timing.
    /// </summary>
    public static bool Verify(string expectedHex, string? actualHex)
    {
        if (string.IsNullOrEmpty(actualHex)) return false;

        byte[] expectedBytes;
        try { expectedBytes = Convert.FromHexString(expectedHex); }
        catch (FormatException) { return false; }

        byte[]? actualBytes;
        try { actualBytes = Convert.FromHexString(actualHex); }
        catch (FormatException) { actualBytes = null; }

        if (actualBytes == null)
            return false;

        if (actualBytes.Length != expectedBytes.Length)
        {
            // Vẫn chạy FixedTimeEquals trên buffer cùng độ dài giả để không rò rỉ thời gian
            // xử lý theo độ dài input thật.
            CryptographicOperations.FixedTimeEquals(expectedBytes, expectedBytes);
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    /// <summary>Timestamp lệch so với giờ hiện tại không quá windowMinutes phút (2 chiều).</summary>
    public static bool IsWithinWindow(long timestampUnixSeconds, DateTimeOffset now, int windowMinutes)
    {
        var diffSeconds = Math.Abs(now.ToUnixTimeSeconds() - timestampUnixSeconds);
        return diffSeconds <= windowMinutes * 60L;
    }
}
