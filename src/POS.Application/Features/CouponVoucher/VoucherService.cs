using System.Globalization;
using POS.Common.Dtos.CentralMD;
using POS.Common.Dtos.Voucher;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Features.CouponVoucher;

/// <summary>
/// 8.3 Danh mục Voucher — port từ VCM.BLUEPOS VoucherController + VoucherData.
/// Validate ở tầng này; persistence qua IVoucherRepository (SP). Serial-trùng do SP kiểm (trả Ok=false).
///
/// ⚠️ IsCheckItem NGƯỢC nghĩa Coupon: true = TỔNG BILL (no lines); false = THEO SẢN PHẨM (có lines).
/// </summary>
public sealed class VoucherService(
    IVoucherRepository repository,
    ICentralMDRepository centralMDRepository) : IVoucherService
{
    public Task<(List<VoucherListItemDto> Items, int Total)> GetListAsync(
        VoucherListFilter filter, CancellationToken ct = default)
        => repository.GetListAsync(filter, ct);

    public Task<VoucherDetailDto?> GetDetailAsync(string itemNo, CancellationToken ct = default)
        => repository.GetDetailAsync(itemNo, ct);

    public async Task<VoucherFormLookupDto> GetFormLookupAsync(CancellationToken ct = default)
    {
        var articleTypes = await centralMDRepository.GetArticleTypesAsync(ct);
        var uoms = await centralMDRepository.GetUnitOfMeasuresAsync(ct);
        return new VoucherFormLookupDto
        {
            ArticleTypes = articleTypes
                .Where(a => !string.IsNullOrWhiteSpace(a.Code))
                .Select(a => new VoucherOptionDto { Value = a.Code!, Text = a.Code! }).ToList(),
            Uoms = uoms
                .Where(u => !string.IsNullOrWhiteSpace(u.Code))
                .Select(u => new VoucherOptionDto { Value = u.Code!, Text = u.Code! }).ToList()
        };
    }

    public async Task<(List<VoucherLineDto> Items, int Total)> SearchItemsAsync(
        string? itemNo, string? itemName, int pageSize, int pageNumber, CancellationToken ct = default)
    {
        var (items, total) = await centralMDRepository.GetProductListAsync(new ProductListFilter
        {
            ItemNo     = (itemNo ?? string.Empty).Trim(),
            ItemName   = (itemName ?? string.Empty).Trim(),
            PageSize   = Math.Max(1, pageSize),
            PageNumber = Math.Max(0, pageNumber)
        }, ct);

        var mapped = items.Select(p => new VoucherLineDto
        {
            ItemNo   = p.ItemNo ?? string.Empty,
            ItemName = p.ItemName1 ?? string.Empty,
            UOM      = p.SalesUnit ?? string.Empty,
            Barcode  = p.BarcodeNo ?? string.Empty
        }).ToList();

        return (mapped, total);
    }

    public async Task<VoucherSaveResult> SaveAsync(
        VoucherSaveRequest request, string actor, CancellationToken ct = default)
    {
        // ── Validate nghiệp vụ (port controller/view legacy) ──
        if (string.IsNullOrWhiteSpace(request.Serial))
            return Fail("Vui lòng nhập số serial voucher/coupon");
        if (string.IsNullOrWhiteSpace(request.ItemName))
            return Fail("Vui lòng nhập tên voucher/coupon");
        if (string.IsNullOrWhiteSpace(request.ArticleType))
            return Fail("Vui lòng chọn loại voucher/coupon");
        if (string.IsNullOrWhiteSpace(request.UnitOfMeasure))
            return Fail("Vui lòng chọn đơn vị tính");

        var start = ParseDmy(request.StartingDateStr);
        var end = ParseDmy(request.EndingDateStr);
        if (start == null) return Fail("Vui lòng chọn ngày bắt đầu");
        if (end == null) return Fail("Vui lòng chọn ngày kết thúc");
        if (start.Value.Date > end.Value.Date)
            return Fail("TỪ NGÀY không lớn hơn ĐẾN NGÀY");

        if (request.DiscountValue <= 0)
            return Fail("Vui lòng nhập giá trị giảm giá");
        if (request.DiscountType == 1 && request.DiscountValue > 100)
            return Fail("Giá trị phần trăm giảm giá không lớn hơn 100");

        // IsCheckItem == false → áp dụng theo sản phẩm → bắt buộc có danh sách.
        // IsCheckItem == true  → tổng bill → bỏ qua danh sách (SP tự xóa lines).
        if (!request.IsCheckItem && request.Items.Count == 0)
            return Fail("Vui lòng thêm sản phẩm áp dụng cho voucher/coupon");
        if (request.IsCheckItem)
            request.Items = [];

        return await repository.SaveAsync(request, actor, ct);
    }

    public async Task<(bool Ok, string Message)> DeleteAsync(string itemNo, CancellationToken ct = default)
    {
        var (deleted, message) = await repository.DeleteAsync(itemNo, ct);
        return (deleted, message);
    }

    private static VoucherSaveResult Fail(string message) => new() { Ok = false, Message = message };

    private static DateTime? ParseDmy(string? dmy)
        => DateTime.TryParseExact(dmy, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : null;
}
