using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace POS.Application.Features.CouponVoucher;

/// <summary>
/// Sinh/validate mã coupon & voucher (dùng chung cho <see cref="CouponService"/> và
/// <see cref="VoucherService"/>). Port từ VCM.BLUEPOS SetupCouponController nhánh Auto/Import —
/// thay <c>Thread.Sleep(1)</c> legacy bằng <see cref="RandomNumberGenerator"/> (crypto-strength,
/// không phụ thuộc <c>UtcNow</c>) để giảm tối đa xác suất trùng mã khi nhiều request Auto-issue
/// chạy đồng thời trong cùng millisecond.
/// </summary>
internal static partial class CouponVoucherCodeGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Numbers = "0123456789";

    /// <summary>Sinh danh sách mã Auto. Trả (Codes, Error) — Error != null nếu tham số không hợp lệ.</summary>
    public static (List<string> Codes, string? Error) GenerateAutoCodes(
        int quantity, int lenCode, string? prefix, int charOfNumber, int charPosition)
    {
        if (quantity <= 0)
            return ([], "Vui lòng nhập số lượng phát hành");
        if (lenCode < 5 || lenCode > 20)
            return ([], "Kích thước mã từ 5->20 ký tự");

        var pfx = (prefix ?? string.Empty).Trim().ToUpperInvariant();
        if (lenCode + pfx.Length + charOfNumber > 20)
            return ([], "Tổng ký tự đã vượt hơn 20");

        var list = new List<string>(quantity);
        var set = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < quantity; i++)
        {
            var codeChar = charOfNumber > 0 ? RandomStringSecure(Alphabet, charOfNumber) : string.Empty;
            var code = RandomStringSecure(Numbers, lenCode);

            if (charOfNumber > 0)
            {
                var pos = Math.Min(Math.Max(charPosition, 0), code.Length);
                code = code.Insert(pos, codeChar);
            }

            var codeValue = pfx + code;
            if (!set.Add(codeValue))
                return ([], "Hiện tại hệ thống generate mã trùng nhau, vui lòng chờ trong ít phút để tạo lại");
            list.Add(codeValue);
        }

        return (list, null);
    }

    /// <summary>
    /// Sinh 1 chuỗi ngẫu nhiên (chữ hoa + số) độ dài <paramref name="length"/> — dùng cho Số serial
    /// voucher khi người dùng để trống. Không đảm bảo unique DB (Serial không bắt buộc unique như mã).
    /// </summary>
    public static string GenerateRandomSerial(int length = 13)
    {
        var rnd = new Random();
        return RandomString(Alphabet + Numbers, length, rnd);
    }

    /// <summary>Validate danh sách mã Import từ Excel. Trả (Codes, Error).</summary>
    public static (List<string> Codes, string? Error) ValidateImportCodes(List<string> importCodes)
    {
        var codes = importCodes ?? [];
        if (codes.Count == 0)
            return ([], "Vui lòng kiểm tra file Excel, không có mã");
        if (codes.Any(string.IsNullOrWhiteSpace))
            return ([], "Vui lòng kiểm tra cột mã, có giá trị trống");

        var trimmed = codes.Select(c => c.Trim()).ToList();

        var invalid = trimmed.Where(c => !CodeRegex().IsMatch(c)).ToList();
        if (invalid.Count > 0)
            return ([], $"Có {invalid.Count} mã trong file excel có ký tự đặc biệt ({string.Join(",", invalid)})");

        var tooLong = trimmed.Where(c => c.Length > 20).ToList();
        if (tooLong.Count > 0)
            return ([], $"Có {tooLong.Count} mã trong file excel vượt quá 20 ký tự ({string.Join(",", tooLong)})");

        var dup = trimmed.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (dup.Count > 0)
            return ([], $"File excel có giá trị trùng ({string.Join(",", dup)}), Vui lòng kiểm tra lại");

        return (trimmed, null);
    }

    private static string RandomString(string source, int length, Random rnd)
        => new(Enumerable.Range(0, length).Select(_ => source[rnd.Next(source.Length)]).ToArray());

    /// <summary>
    /// Sinh chuỗi ngẫu nhiên bằng <see cref="RandomNumberGenerator"/> (crypto-strength) — dùng cho
    /// phần mã voucher/coupon cần đảm bảo unique, khác <see cref="RandomString"/> (dựa vào
    /// <see cref="Random"/> thường, seed theo thời gian, chỉ đủ dùng cho Serial không unique).
    /// </summary>
    private static string RandomStringSecure(string alphabet, int length)
    {
        if (length <= 0) return string.Empty;
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(chars);
    }

    [GeneratedRegex(@"^[0-9\-_A-Za-z]*$")]
    private static partial Regex CodeRegex();
}
