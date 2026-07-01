using System.Globalization;
using System.Text.RegularExpressions;
using POS.Common.Dtos.CentralMD;
using POS.Common.Dtos.SetupCoupon;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Features.CouponVoucher;

/// <summary>
/// 8.1/8.2 Setup Coupon — port từ VCM.BLUEPOS SetupCouponController + SetupCouponData.
/// Sinh mã Auto & validate ở tầng này (business); persistence qua ICouponRepository (SP).
/// Item picker tái dùng ICentralMDRepository.GetProductListAsync (migrate 6.1).
/// </summary>
public sealed partial class CouponService(
    ICouponRepository repository,
    ICentralMDRepository centralMDRepository) : ICouponService
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Numbers = "0123456789";

    public Task<(List<CouponListItemDto> Items, int Total)> GetListAsync(
        CouponListFilter filter, CancellationToken ct = default)
        => repository.GetListAsync(filter, ct);

    public Task<(List<CouponCodeDto> Items, int Total)> GetCodesAsync(
        CouponCodeFilter filter, CancellationToken ct = default)
        => repository.GetCodesAsync(filter, ct);

    public Task<CouponDetailDto?> GetDetailAsync(string itemNo, CancellationToken ct = default)
        => repository.GetDetailAsync(itemNo, ct);

    public Task<CouponFormLookupDto> GetFormLookupAsync(CancellationToken ct = default)
        => repository.GetFormLookupAsync(ct);

    public async Task<(List<CouponItemLineDto> Items, int Total)> SearchItemsAsync(
        string? itemNo, string? itemName, int pageSize, int pageNumber, CancellationToken ct = default)
    {
        var (items, total) = await centralMDRepository.GetProductListAsync(new ProductListFilter
        {
            ItemNo     = (itemNo ?? string.Empty).Trim(),
            ItemName   = (itemName ?? string.Empty).Trim(),
            PageSize   = Math.Max(1, pageSize),
            PageNumber = Math.Max(0, pageNumber)
        }, ct);

        var mapped = items.Select(p => new CouponItemLineDto
        {
            ItemNo   = p.ItemNo ?? string.Empty,
            ItemName = p.ItemName1 ?? string.Empty,
            UOM      = p.SalesUnit ?? string.Empty,
            Barcode  = p.BarcodeNo ?? string.Empty
        }).ToList();

        return (mapped, total);
    }

    public async Task<CouponSaveResult> SaveIssueAsync(CouponIssueSaveRequest request, CancellationToken ct = default)
    {
        // ── Validate ngày ──
        var start = ParseDmy(request.StartingDateStr);
        var end = ParseDmy(request.EndingDateStr);
        if (start.Date > end.Date)
            return Fail("TỪ NGÀY không lớn hơn ĐẾN NGÀY");

        if (string.IsNullOrWhiteSpace(request.Description))
            return Fail("Vui lòng nhập tên phát hành coupon");

        // ── Sinh/validate danh sách mã (chỉ khi tạo mới hoặc chưa có mã trong DB) ──
        var needCodes = string.IsNullOrWhiteSpace(request.ItemNo) || request.QuantityCodeInDB == 0;
        List<string> codes = [];
        if (needCodes)
        {
            string? err;
            if (string.Equals(request.IssueType, "Auto", StringComparison.OrdinalIgnoreCase))
                (codes, err) = GenerateAutoCodes(request);
            else
                (codes, err) = ValidateImportCodes(request.ImportCodes);

            if (err != null) return Fail(err);

            var existing = (await repository.CheckCodesExistAsync(codes, ct)).Distinct().ToList();
            if (existing.Count > 0)
            {
                var msg = string.Equals(request.IssueType, "Auto", StringComparison.OrdinalIgnoreCase)
                    ? $"Mã coupon trùng trong DB ({string.Join(",", existing)}), vui lòng chờ trong ít phút để tạo lại"
                    : $"Mã coupon trùng trong DB ({string.Join(",", existing)}), vui lòng kiểm tra lại file Excel";
                return Fail(msg);
            }
        }

        // ── Validate sản phẩm vs "Tổng hóa đơn" (IsCheckItem) ──
        // IsCheckItem == true  → áp dụng theo sản phẩm (bắt buộc có danh sách)
        // IsCheckItem == false → áp dụng tổng hóa đơn (không được có danh sách)
        if (request.IsCheckItem && request.Items.Count == 0)
            return Fail("Vui lòng thêm sản phẩm vào voucher/coupon");
        if (!request.IsCheckItem && request.Items.Count > 0)
            return Fail("Voucher/Coupon đang áp dụng tổng hóa đơn, vui lòng xóa danh sách sản phẩm");

        try
        {
            var itemNo = await repository.SaveIssueAsync(request, codes, ct);
            return new CouponSaveResult { Ok = true, Message = $"Cập nhật thành công coupon {itemNo}", ItemNo = itemNo };
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    public async Task<CouponSaveResult> SaveAdvancedAsync(CouponAdvancedSaveRequest request, CancellationToken ct = default)
    {
        var start = ParseDmy(request.StartingDateStr);
        var end = ParseDmy(request.EndingDateStr);
        if (start.Date < DateTime.Now.Date)
            return Fail("TỪ NGÀY không được nhỏ hơn ngày hiện tại");
        if (start.Date > end.Date)
            return Fail("TỪ NGÀY không lớn hơn ĐẾN NGÀY");
        if (string.IsNullOrWhiteSpace(request.UOM))
            return Fail("Vui lòng chọn đơn vị tính");
        if (request.DiscountValue <= 0)
            return Fail("Vui lòng nhập giá trị giảm giá");
        if (request.DiscountType == 1 && request.DiscountValue > 100)
            return Fail("Giá trị phần trăm giảm giá không lớn hơn 100");
        if (request.IsMultiUsed && request.LimitQtyUsed == 0)
            return Fail("Vui lòng nhập số lần sử dụng");

        try
        {
            var itemNo = await repository.SaveAdvancedAsync(request, ct);
            return new CouponSaveResult { Ok = true, Message = $"Cập nhật thành công coupon {itemNo}", ItemNo = itemNo };
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    public async Task<(bool Ok, string Message)> DeleteAsync(string itemNo, CancellationToken ct = default)
    {
        var (deleted, message) = await repository.DeleteAsync(itemNo, ct);
        return (deleted, message);
    }

    // ── Sinh mã Auto (port controller SaveIssueCoupon nhánh "Auto") ─────────────
    // Port: thay Thread.Sleep(1) legacy bằng offset theo index để mã duy nhất, không block thread.
    private static (List<string> Codes, string? Error) GenerateAutoCodes(CouponIssueSaveRequest r)
    {
        if (r.Quantity <= 0)
            return ([], "Vui lòng nhập số lượng phát hành");
        if (r.LenCode < 5 || r.LenCode > 20)
            return ([], "Kích thước mã từ 5->20 ký tự");

        var prefix = (r.Prefix ?? string.Empty).Trim().ToUpperInvariant();
        if (r.LenCode + prefix.Length + r.CharOfNumber > 20)
            return ([], "Tổng ký tự coupon đã vượt hơn 20");

        var rnd = new Random();
        var list = new List<string>(r.Quantity);
        var set = new HashSet<string>(StringComparer.Ordinal);
        var baseUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        for (var i = 0; i < r.Quantity; i++)
        {
            var codeChar = r.CharOfNumber > 0 ? RandomString(Alphabet, r.CharOfNumber, rnd) : string.Empty;

            var lenNumber = r.LenCode;
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
            if (r.CharOfNumber > 0)
            {
                var pos = Math.Min(Math.Max(r.CharPosition, 0), code.Length);
                code = code.Insert(pos, codeChar);
            }

            var codeValue = prefix + code;
            if (!set.Add(codeValue))
                return ([], "Hiện tại hệ thống generate coupon trùng nhau, vui lòng chờ trong ít phút để tạo lại");
            list.Add(codeValue);
        }

        return (list, null);
    }

    // ── Validate mã Import từ Excel (port controller nhánh "Import") ────────────
    private static (List<string> Codes, string? Error) ValidateImportCodes(List<string> importCodes)
    {
        var codes = importCodes ?? [];
        if (codes.Count == 0)
            return ([], "Vui lòng kiểm tra file Excel, không có mã coupon");
        if (codes.Any(string.IsNullOrWhiteSpace))
            return ([], "Vui lòng kiểm tra cột CodeCoupon, có giá trị trống");

        var trimmed = codes.Select(c => c.Trim()).ToList();

        var invalid = trimmed.Where(c => !CodeRegex().IsMatch(c)).ToList();
        if (invalid.Count > 0)
            return ([], $"Có {invalid.Count} mã coupon trong file excel có ký tự đặc biệt ({string.Join(",", invalid)})");

        var tooLong = trimmed.Where(c => c.Length > 20).ToList();
        if (tooLong.Count > 0)
            return ([], $"Có {tooLong.Count} mã coupon trong file excel vượt quá 20 ký tự ({string.Join(",", tooLong)})");

        var dup = trimmed.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (dup.Count > 0)
            return ([], $"File excel có giá trị trùng ({string.Join(",", dup)}), Vui lòng kiểm tra lại");

        return (trimmed, null);
    }

    private static string RandomString(string source, int length, Random rnd)
        => new(Enumerable.Range(0, length).Select(_ => source[rnd.Next(source.Length)]).ToArray());

    private static DateTime ParseDmy(string? dmy)
        => DateTime.TryParseExact(dmy, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : new DateTime(1900, 1, 1);

    private static CouponSaveResult Fail(string message) => new() { Ok = false, Message = message };

    [GeneratedRegex(@"^[0-9\-_A-Za-z]*$")]
    private static partial Regex CodeRegex();
}
