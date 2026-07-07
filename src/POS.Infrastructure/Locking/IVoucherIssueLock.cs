namespace POS.Infrastructure.Locking;

/// <summary>
/// Distributed lock (Redis) — chặn TOÀN BỘ thao tác sinh mã Auto voucher/coupon chạy đồng thời,
/// bất kể user/instance nào (khác <c>ISyncFileLock</c> — keyed theo file, chỉ trong 1 process).
/// Hiệu lực xuyên suốt các instance POS.Web sau load balancer vì Redis là external shared store.
/// </summary>
public interface IVoucherIssueLock
{
    /// <summary>
    /// Chờ và lấy khóa toàn cục. Trả null nếu hết thời gian chờ (đang bị request khác giữ quá lâu).
    /// Dispose giá trị trả về (<c>await using</c>) để nhả khóa.
    /// </summary>
    Task<IAsyncDisposable?> AcquireAsync(CancellationToken ct = default);
}
