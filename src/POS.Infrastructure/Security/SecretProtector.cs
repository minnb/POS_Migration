using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace POS.Infrastructure.Security;

/// <summary>
/// Mã hóa/giải mã chuỗi bí mật (password trong appsettings) bằng AES-256-GCM.
/// Token có dạng <c>enc:</c> + base64(<c>nonce(12) || tag(16) || ciphertext</c>).
/// Khóa = 32 byte, truyền dạng base64 (env <c>POS_SECRET_KEY</c>, dùng chung cho POS.Api + POS.Web).
/// Cross-platform (chạy được trong Docker Linux) — KHÔNG dùng DPAPI.
/// </summary>
public static class SecretProtector
{
    public const string Prefix = "enc:";
    private const int KeySize   = 32; // AES-256
    private const int NonceSize = 12; // GCM chuẩn
    private const int TagSize   = 16;

    // enc: theo sau bởi base64 (A-Z a-z 0-9 + / =). Dừng ở ký tự ngoài tập
    // (vd ';' trong connection string) → cho phép mã hóa CHỈ phần password.
    private static readonly Regex TokenRegex = new(@"enc:[A-Za-z0-9+/=]+", RegexOptions.Compiled);

    /// <summary>Chuỗi có chứa token enc: hay không.</summary>
    public static bool HasToken(string? value)
        => value != null && value.Contains(Prefix, StringComparison.Ordinal);

    /// <summary>Sinh khóa AES-256 mới (base64) để đặt vào env POS_SECRET_KEY.</summary>
    public static string GenerateKey()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(KeySize));

    /// <summary>Mã hóa 1 plaintext → token "enc:...".</summary>
    public static string Encrypt(string plaintext, string base64Key)
    {
        var key        = ParseKey(base64Key);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce      = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher     = new byte[plainBytes.Length];
        var tag        = new byte[TagSize];

        using var gcm = new AesGcm(key, TagSize);
        gcm.Encrypt(nonce, plainBytes, cipher, tag);

        var blob = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce,  0, blob, 0,                  NonceSize);
        Buffer.BlockCopy(tag,    0, blob, NonceSize,          TagSize);
        Buffer.BlockCopy(cipher, 0, blob, NonceSize + TagSize, cipher.Length);
        return Prefix + Convert.ToBase64String(blob);
    }

    /// <summary>Giải mã 1 token "enc:..." → plaintext.</summary>
    public static string Decrypt(string token, string base64Key)
    {
        var key  = ParseKey(base64Key);
        var b64  = token.StartsWith(Prefix, StringComparison.Ordinal) ? token[Prefix.Length..] : token;
        byte[] blob;
        try { blob = Convert.FromBase64String(b64); }
        catch (FormatException) { throw new CryptographicException("Token enc: không phải base64 hợp lệ."); }

        if (blob.Length < NonceSize + TagSize)
            throw new CryptographicException("Token enc: không hợp lệ (độ dài quá ngắn).");

        var nonce  = blob.AsSpan(0, NonceSize);
        var tag    = blob.AsSpan(NonceSize, TagSize);
        var cipher = blob.AsSpan(NonceSize + TagSize);
        var plain  = new byte[cipher.Length];

        using var gcm = new AesGcm(key, TagSize);
        gcm.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    /// <summary>
    /// Tìm &amp; giải mã MỌI token enc:... trong 1 chuỗi (cho phép mã hóa chỉ một phần,
    /// vd Password trong connection string <c>...;Password=enc:XXX;...</c>).
    /// </summary>
    public static string DecryptTokens(string value, string base64Key)
        => TokenRegex.Replace(value, m => Decrypt(m.Value, base64Key));

    private static byte[] ParseKey(string base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
            throw new CryptographicException("Thiếu khóa giải mã (env POS_SECRET_KEY).");

        byte[] key;
        try { key = Convert.FromBase64String(base64Key.Trim()); }
        catch (FormatException) { throw new CryptographicException("POS_SECRET_KEY không phải base64 hợp lệ."); }

        if (key.Length != KeySize)
            throw new CryptographicException($"POS_SECRET_KEY phải là {KeySize} byte (base64). Hiện tại: {key.Length} byte.");
        return key;
    }
}
