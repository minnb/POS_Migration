using POS.Common.Dtos.CentralSale;

namespace POS.Application.Features.StoreActivities;

public interface IBusinessDayService
{
    /// <summary>Lưới máy POS + trạng thái đóng ngày cho 1 store + 1 ngày kinh doanh.</summary>
    Task<List<PosDayStagingDto>> GetPosDayStagingAsync(
        string storeNo, DateTime businessDate, CancellationToken ct = default);

    /// <summary>Trạng thái xác nhận (nếu có) của store + ngày kinh doanh đó.</summary>
    Task<BusinessDayConfirmDto?> GetConfirmStatusAsync(
        string storeNo, DateTime businessDate, CancellationToken ct = default);

    /// <summary>Ngày kinh doanh hiện tại của store (BussinessDateOpen) — null nếu store chưa có.</summary>
    Task<DateTime?> GetCurrentBusinessDateAsync(string storeNo, CancellationToken ct = default);

    /// <summary>
    /// Xác nhận kết thúc ngày — chặn nếu còn máy POS chưa đóng ngày hoặc đã xác nhận trước đó.
    /// Thành công sẽ advance BussinessDateOpen (+1 ngày) cho máy POS.
    /// Khi <paramref name="allowForceConfirm"/> = true (role ITOps/SystemAdmin) sẽ BỎ QUA kiểm tra
    /// "còn máy POS chưa đóng ngày" (force EOD); các guard khác vẫn giữ.
    /// </summary>
    Task<ConfirmBusinessDayResult> ConfirmBusinessDayAsync(
        string storeNo, DateTime businessDate, string confirmedBy,
        bool allowForceConfirm = false, CancellationToken ct = default);
}
