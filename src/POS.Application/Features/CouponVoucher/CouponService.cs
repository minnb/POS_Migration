using System.Globalization;
using POS.Common.Dtos.CentralMD;
using POS.Common.Dtos.SetupCoupon;
using POS.Infrastructure.Locking;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Features.CouponVoucher;

/// <summary>
/// 8.1/8.2 Setup Coupon — port từ VCM.BLUEPOS SetupCouponController + SetupCouponData.
/// Sinh mã Auto &amp; validate ở tầng này (business, qua <see cref="CouponVoucherCodeGenerator"/>);
/// persistence qua ICouponRepository (SP). Item picker tái dùng ICentralMDRepository.GetProductListAsync (migrate 6.1).
///
/// Sinh mã Auto cho "phát hành thêm" (IssueMoreAsync) được bọc trong <see cref="IVoucherIssueLock"/>
/// (distributed lock qua Redis, dùng chung với Voucher) — chặn 2 user/2 instance sinh mã đồng thời.
/// </summary>
public sealed class CouponService(
    ICouponRepository repository,
    ICentralMDRepository centralMDRepository,
    IVoucherIssueLock issueLock) : ICouponService
{
    public Task<(List<CouponListItemDto> Items, int Total)> GetListAsync(
        CouponListFilter filter, CancellationToken ct = default)
        => repository.GetListAsync(filter, ct);

    public Task<(List<CouponHeaderListItemDto> Items, int Total)> GetHeaderListAsync(
        CouponHeaderListFilter filter, CancellationToken ct = default)
        => repository.GetHeaderListAsync(filter, ct);

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

        request.ArticleType = string.IsNullOrWhiteSpace(request.ArticleType) ? "ZCPN" : request.ArticleType;

        // ── Sinh/validate danh sách mã (chỉ khi tạo mới hoặc chưa có mã trong DB) ──
        var needCodes = string.IsNullOrWhiteSpace(request.ItemNo) || request.QuantityCodeInDB == 0;
        List<string> codes = [];
        if (needCodes)
        {
            string? err;
            if (string.Equals(request.IssueType, "Auto", StringComparison.OrdinalIgnoreCase))
                (codes, err) = CouponVoucherCodeGenerator.GenerateAutoCodes(
                    request.Quantity, request.LenCode, request.Prefix, request.CharOfNumber, request.CharPosition);
            else
                (codes, err) = CouponVoucherCodeGenerator.ValidateImportCodes(request.ImportCodes);

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
        if (string.IsNullOrWhiteSpace(request.ItemNo) && start.Date < DateTime.Now.Date)
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

        request.ArticleType = string.IsNullOrWhiteSpace(request.ArticleType) ? "ZCPN" : request.ArticleType;
        request.IsCheckAPI = true;      // cố định
        request.LimitQty = 999999999;   // cố định, không giới hạn

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

    public async Task<CouponSaveResult> IssueMoreAsync(CouponIssueMoreRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ItemNo))
            return Fail("Thiếu mã phát hành (ItemNo)");

        await using var @lock = await issueLock.AcquireAsync(ct);
        if (@lock == null)
            return Fail("Hệ thống đang xử lý phát hành coupon khác, vui lòng thử lại sau.");

        var (codes, err) = CouponVoucherCodeGenerator.GenerateAutoCodes(
            request.Quantity, request.LenCode, request.Prefix, request.CharOfNumber, request.CharPosition);
        if (err != null) return Fail(err);

        var existing = (await repository.CheckCodesExistAsync(codes, ct)).Distinct().ToList();
        if (existing.Count > 0)
            return Fail($"Mã coupon trùng trong DB ({string.Join(",", existing)}), vui lòng thử lại");

        try
        {
            var added = await repository.IssueMoreAsync(request.ItemNo, codes, ct);
            return new CouponSaveResult
            {
                Ok = true, ItemNo = request.ItemNo,
                Message = $"Phát hành thêm {added} mã thành công cho coupon {request.ItemNo}"
            };
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    public Task<CouponSaveResult> UpdateBlockedAsync(string itemNo, bool blocked, CancellationToken ct = default)
        => repository.UpdateBlockedAsync(itemNo, blocked, ct);

    private static DateTime ParseDmy(string? dmy)
        => DateTime.TryParseExact(dmy, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : new DateTime(1900, 1, 1);

    private static CouponSaveResult Fail(string message) => new() { Ok = false, Message = message };
}
