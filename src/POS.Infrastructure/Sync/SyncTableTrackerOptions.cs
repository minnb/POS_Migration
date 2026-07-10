namespace POS.Infrastructure.Sync;

/// <summary>
/// Cấu hình flush bất đồng bộ cho <see cref="ISyncTableTrackerService"/>. Bind từ section
/// "SyncTableTracker" của appsettings. Đăng ký ở POS.Api và POS.Web (2 tiến trình ghi master data).
/// </summary>
public sealed class SyncTableTrackerOptions
{
    public const string SectionName = "SyncTableTracker";

    /// <summary>Chu kỳ flush Channel → DB (giây).</summary>
    public int FlushIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Sức chứa tối đa của Channel (bounded). Đầy → drop event cũ nhất (DropOldest) — chấp nhận
    /// được vì POSLastCounter chỉ là cache/hint, giá trị mới hơn ở lần bump kế tiếp sẽ tự "chữa lành".
    /// </summary>
    public int ChannelCapacity { get; set; } = 5000;
}
