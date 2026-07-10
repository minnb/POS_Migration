using System.Data;
using Dapper;
using POS.Infrastructure.Database;

namespace POS.Infrastructure.Repositories;

/// <inheritdoc cref="POS.Infrastructure.Repositories.Interfaces.ISyncTrackerRepository"/>
public sealed class SyncTrackerRepository(CentralMDConnectionFactory connectionFactory)
    : BaseRepository(connectionFactory), Interfaces.ISyncTrackerRepository
{
    public async Task BulkUpdateCounterAsync(
        IReadOnlyDictionary<string, long> counters, CancellationToken ct = default)
    {
        if (counters.Count == 0) return;

        var table = new DataTable();
        table.Columns.Add("TableName", typeof(string));
        table.Columns.Add("Counter", typeof(long));
        foreach (var (tableName, counter) in counters)
            table.Rows.Add(tableName, counter);

        var p = new DynamicParameters();
        p.Add("@Counters", table.AsTableValuedParameter("dbo.TVP_SyncCounterUpdate"));

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("dbo.usp_SyncTableList_BulkUpdateCounter", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 30, cancellationToken: ct));
    }
}
