namespace POS.Infrastructure.Files;

/// <summary>
/// Cấu hình cho <c>PosFileImportWorker</c> — quét thư mục, giải nén .zip → .txt, insert DB.
/// Bind section "FileImport" trong appsettings.json. Đường dẫn tuyệt đối, độc lập với FtpRootPath.
/// </summary>
public sealed class FileImportOptions
{
    public const string SectionName = "FileImport";

    /// <summary>Bật/tắt worker. false → worker idle, không quét.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Thư mục quét *.zip (đường dẫn tuyệt đối).</summary>
    public string InboxFolder { get; set; } = "";

    /// <summary>Thư mục chứa zip xử lý thất bại (giữ để retry/audit thủ công).</summary>
    public string ErrorFolder { get; set; } = "";

    /// <summary>Thư mục temp để giải nén. Rỗng → dùng "{InboxFolder}/_work".</summary>
    public string WorkFolder { get; set; } = "";

    /// <summary>Mẫu file cần quét trong InboxFolder.</summary>
    public string FileFilter { get; set; } = "*.zip";

    /// <summary>Chu kỳ quét (giây).</summary>
    public int PollIntervalSeconds { get; set; } = 30;

    /// <summary>Bỏ qua zip mới ghi trong vòng N giây (tránh nhận file đang upload dở).</summary>
    public int StableSeconds { get; set; } = 10;

    /// <summary>Số zip tối đa xử lý mỗi vòng (bound, tránh ôm quá nhiều file/lượt).</summary>
    public int MaxFilesPerCycle { get; set; } = 20;

    /// <summary>Giá trị cột "source" ghi vào DataRawJson để truy vết nguồn nạp.</summary>
    public string Source { get; set; } = "FILE";
}
