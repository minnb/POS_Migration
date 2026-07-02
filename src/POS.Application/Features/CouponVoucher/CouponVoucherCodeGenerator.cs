using System.Globalization;
using System.Text.RegularExpressions;

namespace POS.Application.Features.CouponVoucher;

/// <summary>
/// Sinh/validate mã coupon & voucher (dùng chung cho <see cref="CouponService"/> và
/// <see cref="VoucherService"/>). Port từ VCM.BLUEPOS SetupCouponController nhánh Auto/Import —
/// thay <c>Thread.Sleep(1)</c> legacy bằng offset theo index để mã duy nhất, không block thread.
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

        var rnd = new Random();
        var list = new List<string>(quantity);
        var set = new HashSet<string>(StringComparer.Ordinal);
        var baseUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        for (var i = 0; i < quantity; i++)
        {
            var codeChar = charOfNumber > 0 ? RandomString(Alphabet, charOfNumber, rnd) : string.Empty;

            var lenNumber = lenCode;
            var timeUnix = (baseUnix + i).ToString(CultureInfo.InvariantCulture);

            var lengthCodeUnix = lenNumber;
            var codeBlance = string.Empty;
            if (timeUnix.Length < lenNumber)
            {
                lengthCodeUnix = timeUnix.Length;
                codeBlance = RandomString(Numbers, lenNumber - timeUnix.Length, rnd);
            }

            var codeNumberFirst = codeBlance + timeUnix.Substring(timeUnix.Length - lengthCodeUnix, lengthCodeUnix);

            var code = codeNumberFirst;
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

    [GeneratedRegex(@"^[0-9\-_A-Za-z]*$")]
    private static partial Regex CodeRegex();
}
