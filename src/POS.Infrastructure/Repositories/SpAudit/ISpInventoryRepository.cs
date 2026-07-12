namespace POS.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Dòng thô từ sys.procedures/sys.sql_modules/sys.dm_exec_procedure_stats — chỉ dùng nội bộ
/// giữa Infrastructure và Application để phân loại, KHÔNG map trực tiếp ra DTO response.
/// </summary>
public sealed class SpRawInventoryRow
{
    public string SchemaName { get; set; } = "";
    public string ProcedureName { get; set; } = "";
    public long ObjectId { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime ModifyDate { get; set; }
    public string? Definition { get; set; }
    public long ExecutionCount { get; set; }
    public DateTime? LastExecutionAt { get; set; }
}

public interface ISpInventoryRepository
{
    /// <summary>Kiểm kê stored procedure của 1 database ("CentralMD"/"CentralSale"/"Loyalty").</summary>
    Task<List<SpRawInventoryRow>> GetProcedureInventoryAsync(
        string databaseKey, int maxRows, int commandTimeoutSec, CancellationToken ct = default);
}
