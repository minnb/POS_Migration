using Newtonsoft.Json;

namespace POS.Common.Dtos.DataSync;

/// <summary>
/// 1 dòng kết quả của SP1 [SyncTable_Get] — mô tả 1 bảng master data cần đồng bộ xuống POS.
/// Map theo tên cột trả về của SP (Dapper map theo property name, không phân biệt hoa thường).
/// </summary>
public sealed class SyncTableInfo
{
    public string? TableName { get; set; }

    public long POSLastCounter { get; set; }

    /// <summary>
    /// Watermark ACK riêng của MasterDataZipGeneratorWorker — chỉ có giá trị thật khi isChange="C"
    /// (nhánh 'A'/'W' không SELECT cột này, Dapper để mặc định 0). So sánh POSLastCounter &gt;
    /// ZipWatermarkCounter để phát hiện bảng đã đổi; ACK bằng cách ghi giá trị POSLastCounter đã
    /// snapshot lúc đầu cycle vào cột này (KHÔNG re-read DB tại thời điểm ACK).
    /// </summary>
    public long ZipWatermarkCounter { get; set; }

    /// <summary>Cột [Procedure] của SP — tên procedure dùng cho ProcedureName trong envelope file POS.</summary>
    [JsonProperty("Procedure")]
    public string? Procedure { get; set; }

    public string? OrderByName { get; set; }

    public bool IsByStore { get; set; }

    public string? ColumnFilter { get; set; }

    public string? GroupName { get; set; }

    /// <summary>true → luôn lấy toàn bộ dữ liệu (POSLastCounter = 0) bất kể typeSync.</summary>
    public bool IsFirstDataAll { get; set; }

    /// <summary>true → bảng này đóng gói riêng 1 file .zip khi sinh master data; false (mặc định) → gom chung zip "common".</summary>
    public bool IsSingleFile { get; set; }

    /// <summary>
    /// Action ghi vào envelope file .txt (vd "TRUNC-INSERT"/"INSERT"/"DELETE-INSERT") — cấu hình theo
    /// từng bảng trong SyncTableList, KHÔNG hardcode ở C#. Null nếu SP chưa có cột này (chưa chạy
    /// docs/sql/SyncTableList_AddAction.sql) — MasterDataSyncService fallback hằng số mặc định.
    /// </summary>
    public string? Action { get; set; }
}
