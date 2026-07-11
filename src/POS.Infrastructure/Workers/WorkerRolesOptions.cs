namespace POS.Infrastructure.Workers;

/// <summary>
/// Feature toggle cho POS.Worker — quyết định worker nào được đăng ký khi chạy dạng
/// hosted-service dài hạn. Bind section "WorkerRoles" trong appsettings.json.
/// Không áp dụng cho chế độ <c>--run-once</c> (Program.cs) — chế độ đó luôn chỉ chạy
/// <see cref="PosFileImportService"/> bất kể giá trị các cờ này.
/// </summary>
public sealed class WorkerRolesOptions
{
    public const string SectionName = "WorkerRoles";

    /// <summary>Đăng ký <c>PosFileImportWorker</c> (quét file .zip chung với Web/Api).</summary>
    public bool EnableFileProcessing { get; set; } = true;

    /// <summary>Đăng ký <c>PosSalesConsumerWorker</c> (RabbitMQ consumer).</summary>
    public bool EnableRabbitMQConsumer { get; set; } = true;

    /// <summary>Đăng ký <c>Rpt_ReportSaleDetail_Insert</c> (SQL polling).</summary>
    public bool EnableSqlReportWorker { get; set; } = true;

    /// <summary>Đăng ký <c>WorkerHeartbeatService</c> (ghi heartbeat Redis).</summary>
    public bool EnableHeartbeat { get; set; } = true;

    /// <summary>
    /// Đăng ký <c>MasterDataZipGeneratorWorker</c> (poll watermark POSLastCounter, generate zip theo
    /// thay đổi). Mặc định false — opt-in, chỉ bật sau khi đã cấu hình AppSettings:FtpRootPath +
    /// DBA đánh dấu SyncTableList.IsOnlyChange=1 cho các bảng cần theo dõi (xem docs/ROLLOUT.md).
    /// </summary>
    public bool EnableMasterDataZipGenerator { get; set; } = false;
}
