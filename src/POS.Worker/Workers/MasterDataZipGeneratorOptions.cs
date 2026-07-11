namespace POS.Worker.Workers;

/// <summary>Cấu hình MasterDataZipGeneratorWorker. Bind section "MasterDataZipGenerator".</summary>
public sealed class MasterDataZipGeneratorOptions
{
    public const string SectionName = "MasterDataZipGenerator";

    /// <summary>Chu kỳ poll counter — giây.</summary>
    public int IntervalSeconds { get; set; } = 300;

    /// <summary>TTL cho distributed lock "chỉ 1 instance chạy 1 lượt" — phút.</summary>
    public int LockTtlMinutes { get; set; } = 30;

    /// <summary>Số terminal xử lý song song tối đa (Parallel.ForEachAsync).</summary>
    public int MaxParallelTerminals { get; set; } = 4;

    /// <summary>Lần chạy đầu tiên (watermark chưa tồn tại): true → chỉ seed, không generate.</summary>
    public bool SeedWatermarkOnFirstRun { get; set; } = true;

    /// <summary>Số lần lỗi liên tiếp trước khi 1 terminal bị đưa vào quarantine (bỏ qua các lượt sau).</summary>
    public int QuarantineThreshold { get; set; } = 3;
}
