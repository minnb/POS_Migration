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

    /// <summary>Cột [Procedure] của SP — tên procedure dùng cho ProcedureName trong envelope file POS.</summary>
    [JsonProperty("Procedure")]
    public string? Procedure { get; set; }

    public string? OrderByName { get; set; }

    public bool IsByStore { get; set; }

    public string? ColumnFilter { get; set; }

    public string? GroupName { get; set; }

    /// <summary>true → luôn lấy toàn bộ dữ liệu (POSLastCounter = 0) bất kể typeSync.</summary>
    public bool IsFirstDataAll { get; set; }
}
