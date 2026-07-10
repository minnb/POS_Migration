using System.IO.Compression;

namespace POS.Infrastructure.Files;

/// <summary>
/// Cấu hình sinh file master data .zip cho POS. Bind từ section "MasterDataSync" của appsettings.
/// LƯU Ý:
/// - KHÔNG có RootPath: thư mục đích lấy từ AppSettings:FtpRootPath qua ISyncDataPosService.MapFtpPath.
/// - KHÔNG có Delimiter/Header/Encoding: format file cố định là JSON (SyncTableList) + UTF-8,
///   bám theo convention của DataRawService.CreateFileSODFakeAsync.
/// </summary>
public sealed class MasterDataSyncOptions
{
    public const string SectionName = "MasterDataSync";

    /// <summary>Timeout cho SP1/SP2 (bảng lớn) — giây.</summary>
    public int SqlCommandTimeoutSeconds { get; set; } = 600;

    /// <summary>Lưới an toàn: xóa mọi .zip trong thư mục đích cũ hơn số ngày này (file mồ côi).</summary>
    public int KeepZipDays { get; set; } = 2;

    /// <summary>Gắn yyyyMMdd vào tên zip để tự sinh lại sang ngày mới.</summary>
    public bool DateInZipName { get; set; } = true;

    /// <summary>
    /// Số record tối đa mỗi file .txt. Bảng lớn → tách nhiều file (..._001.txt, ..._002.txt) giúp POS import nhanh,
    /// peak RAM thấp. Batch đầu Action="TRUNC-INSERT", các batch sau "INSERT" (append). &lt;= 0 → không tách.
    /// </summary>
    public int BatchSizePerFile { get; set; } = 10000;

    /// <summary>
    /// Mức nén zip: Fastest | Optimal | NoCompression | SmallestSize.
    /// Mặc định Fastest — master data JSON lớn, Optimal tốn CPU/chậm; Fastest nhanh 2–5× (file lớn hơn ~10–30%).
    /// </summary>
    public string ZipCompressionLevel { get; set; } = "Fastest";

    /// <summary>
    /// Số bảng xử lý song song tối đa. Mỗi bảng dùng SqlConnection riêng → thread-safe.
    /// Mặc định 4 (~4× tốc độ so với sequential). &lt;= 0 → sequential (1 bảng/lượt, an toàn tuyệt đối).
    /// Tăng nếu SQL Server còn headroom; giảm nếu DB bị quá tải.
    /// </summary>
    public int MaxParallelTables { get; set; } = 4;

    /// <summary>
    /// Số lượt sinh master data .zip tối đa chạy đồng thời trên toàn cụm (Redis-based distributed throttle,
    /// bảo vệ SQL Server/CPU khi nhiều POS cùng sync giờ cao điểm). &lt;= 0 sẽ không cho lượt nào chạy — luôn đặt &gt;= 1.
    /// </summary>
    public int MaxConcurrentGeneration { get; set; } = 3;

    /// <summary>
    /// Slot throttle coi là "mồ côi" (process giữ slot bị crash) sau bao nhiêu giây — tự bị dọn ở lượt
    /// acquire kế tiếp (xem <see cref="POS.Infrastructure.Cache.IRedisManager.TryAcquireSlotAsync"/>).
    /// </summary>
    public int ThrottleStaleAfterSeconds { get; set; } = 600;

    /// <summary>Parse <see cref="ZipCompressionLevel"/> → enum; giá trị lạ → Fastest.</summary>
    public CompressionLevel ResolveCompressionLevel()
        => Enum.TryParse<CompressionLevel>(ZipCompressionLevel, ignoreCase: true, out var level)
            ? level
            : CompressionLevel.Fastest;
}
