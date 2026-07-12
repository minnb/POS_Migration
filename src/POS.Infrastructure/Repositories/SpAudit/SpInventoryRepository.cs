using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// Kiểm kê stored procedure trực tiếp từ SQL Server (sys.procedures/sys.sql_modules/
/// sys.dm_exec_procedure_stats). Chỉ SELECT — không bao giờ ALTER/DROP/CREATE.
/// </summary>
public sealed class SpInventoryRepository(
    CentralMDConnectionFactory centralMdFactory,
    CentralSaleConnectionFactory centralSaleFactory,
    LoyaltyConnectionFactory loyaltyFactory,
    ILogger<SpInventoryRepository> logger) : ISpInventoryRepository
{
    // last_execution_time là giờ LOCAL của SQL Server, không phải UTC — không convert ở đây.
    private const string SqlWithStats = """
        SELECT TOP (@MaxRows)
            s.name AS SchemaName, p.name AS ProcedureName, p.object_id AS ObjectId,
            p.create_date AS CreateDate, p.modify_date AS ModifyDate,
            m.definition AS Definition,
            ISNULL(ps.execution_count, 0) AS ExecutionCount,
            ps.last_execution_time AS LastExecutionAt
        FROM sys.procedures p
        INNER JOIN sys.schemas s ON s.schema_id = p.schema_id
        LEFT JOIN sys.sql_modules m ON m.object_id = p.object_id
        LEFT JOIN sys.dm_exec_procedure_stats ps
            ON ps.object_id = p.object_id AND ps.database_id = DB_ID()
        WHERE p.is_ms_shipped = 0
        ORDER BY s.name, p.name;
        """;

    // Fallback khi login thiếu quyền VIEW SERVER STATE / VIEW SERVER PERFORMANCE STATE —
    // bỏ execution stats, KHÔNG fail cả lượt quét database.
    private const string SqlWithoutStats = """
        SELECT TOP (@MaxRows)
            s.name AS SchemaName, p.name AS ProcedureName, p.object_id AS ObjectId,
            p.create_date AS CreateDate, p.modify_date AS ModifyDate,
            m.definition AS Definition,
            CAST(0 AS BIGINT) AS ExecutionCount,
            CAST(NULL AS DATETIME) AS LastExecutionAt
        FROM sys.procedures p
        INNER JOIN sys.schemas s ON s.schema_id = p.schema_id
        LEFT JOIN sys.sql_modules m ON m.object_id = p.object_id
        WHERE p.is_ms_shipped = 0
        ORDER BY s.name, p.name;
        """;

    // Error 297/300/1088 — các mã lỗi permission-denied phổ biến khi thiếu quyền xem DMV.
    // Heuristic theo môi trường thực tế, có thể cần điều chỉnh nếu SQL Server đổi version.
    private static bool IsPermissionDenied(SqlException ex) => ex.Number is 297 or 300 or 1088;

    public async Task<List<SpRawInventoryRow>> GetProcedureInventoryAsync(
        string databaseKey, int maxRows, int commandTimeoutSec, CancellationToken ct = default)
    {
        var factory = ResolveFactory(databaseKey);
        using var conn = await factory.CreateOpenConnectionAsync(ct);

        try
        {
            var rows = await conn.QueryAsync<SpRawInventoryRow>(
                new CommandDefinition(SqlWithStats, new { MaxRows = maxRows },
                    commandTimeout: commandTimeoutSec, cancellationToken: ct));
            return rows.AsList();
        }
        catch (SqlException ex) when (IsPermissionDenied(ex))
        {
            logger.LogWarning(ex,
                "[SpInventoryRepository] Thiếu quyền xem execution stats trên {DatabaseKey} — bỏ qua, chỉ lấy định nghĩa procedure",
                databaseKey);
            var rows = await conn.QueryAsync<SpRawInventoryRow>(
                new CommandDefinition(SqlWithoutStats, new { MaxRows = maxRows },
                    commandTimeout: commandTimeoutSec, cancellationToken: ct));
            return rows.AsList();
        }
    }

    private IDbConnectionFactory ResolveFactory(string databaseKey) => databaseKey switch
    {
        "CentralMD" => centralMdFactory,
        "CentralSale" => centralSaleFactory,
        "Loyalty" => loyaltyFactory,
        _ => throw new ArgumentOutOfRangeException(nameof(databaseKey), databaseKey, "Database key không hợp lệ")
    };
}
