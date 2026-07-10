namespace POS.Application.Features.DataSync;

/// <summary>
/// Ném khi số lượt sinh master data .zip đang chạy đồng thời đã đạt giới hạn
/// (<see cref="POS.Infrastructure.Files.MasterDataSyncOptions.MaxConcurrentGeneration"/>) — phân biệt với lỗi
/// sinh file thật để caller (controller/POS.Web) trả đúng thông báo throttle.
/// </summary>
public sealed class MasterDataThrottleException(string message) : Exception(message);
