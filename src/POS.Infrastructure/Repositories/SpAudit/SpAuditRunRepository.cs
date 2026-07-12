using Dapper;
using POS.Common.Dtos.Ops.SpAudit;
using POS.Common.Enums;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

/// <summary>Lưu/đọc lịch sử SQL Audit trong RPOSMasterData (dbo.SqlAuditRun/dbo.SqlAuditFinding).</summary>
public sealed class SpAuditRunRepository(CentralMDConnectionFactory connectionFactory)
    : BaseRepository(connectionFactory), ISpAuditRunRepository
{
    private sealed class RunHeaderRow
    {
        public long RunId { get; set; }
        public DateTime RunStartedUtc { get; set; }
        public DateTime RunFinishedUtc { get; set; }
        public string DatabasesScanned { get; set; } = "";
        public int TotalProcedures { get; set; }
        public bool ProceduresTruncated { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public async Task SaveRunAsync(SpAuditSnapshotDto snapshot, CancellationToken ct = default)
    {
        const string insertRunSql = """
            INSERT INTO dbo.SqlAuditRun
                (RunStartedUtc, RunFinishedUtc, DatabasesScanned, TotalProcedures,
                 MigrationCandidateCount, CleanupCandidateCount, ProceduresTruncated, ErrorMessage)
            OUTPUT INSERTED.RunId
            VALUES (@RunStartedUtc, @RunFinishedUtc, @DatabasesScanned, @TotalProcedures,
                    @MigrationCandidateCount, @CleanupCandidateCount, @ProceduresTruncated, @ErrorMessage);
            """;

        const string insertFindingSql = """
            INSERT INTO dbo.SqlAuditFinding
                (RunId, SchemaName, ProcedureName, DatabaseKey, Complexity,
                 ExecutionCount, LastExecutionAt, Recommendation, Note)
            VALUES (@RunId, @Schema, @ProcedureName, @DatabaseKey, @Complexity,
                    @ExecutionCount, @LastExecutionAt, @Recommendation, @Note);
            """;

        await ExecuteInTransactionAsync(async (conn, tx) =>
        {
            var runId = await conn.QuerySingleAsync<long>(new CommandDefinition(insertRunSql, new
            {
                snapshot.RunStartedUtc,
                snapshot.RunFinishedUtc,
                DatabasesScanned = string.Join(",", snapshot.DatabasesScanned),
                snapshot.TotalProcedures,
                MigrationCandidateCount = snapshot.Items.Count(i => i.Recommendation == SpRecommendation.MigrationCandidate),
                CleanupCandidateCount = snapshot.Items.Count(i => i.Recommendation == SpRecommendation.CleanupCandidate),
                snapshot.ProceduresTruncated,
                snapshot.ErrorMessage
            }, transaction: tx, cancellationToken: ct));

            if (snapshot.Items.Count == 0) return;

            var findingParams = snapshot.Items.Select(i => new
            {
                RunId = runId,
                i.Schema,
                i.ProcedureName,
                i.DatabaseKey,
                Complexity = i.Complexity.ToString(),
                i.ExecutionCount,
                i.LastExecutionAt,
                Recommendation = i.Recommendation.ToString(),
                i.Note
            });

            await conn.ExecuteAsync(new CommandDefinition(insertFindingSql, findingParams, transaction: tx, cancellationToken: ct));
        }, ct: ct);
    }

    public async Task<SpAuditSnapshotDto?> GetLatestRunAsync(CancellationToken ct = default)
    {
        const string runSql = """
            SELECT TOP (1) RunId, RunStartedUtc, RunFinishedUtc, DatabasesScanned,
                   TotalProcedures, ProceduresTruncated, ErrorMessage
            FROM dbo.SqlAuditRun
            ORDER BY RunFinishedUtc DESC;
            """;

        var run = await QueryFirstOrDefaultAsync<RunHeaderRow>(runSql, ct: ct);
        if (run == null) return null;

        const string findingSql = """
            SELECT SchemaName AS Schema, ProcedureName, DatabaseKey, Complexity,
                   ExecutionCount, LastExecutionAt, Recommendation, Note
            FROM dbo.SqlAuditFinding
            WHERE RunId = @RunId
            ORDER BY SchemaName, ProcedureName;
            """;

        var findings = await QueryAsync<SpInventoryItemDto>(findingSql, new { run.RunId }, ct: ct);

        return new SpAuditSnapshotDto
        {
            RunStartedUtc = run.RunStartedUtc,
            RunFinishedUtc = run.RunFinishedUtc,
            DatabasesScanned = run.DatabasesScanned.Split(',', StringSplitOptions.RemoveEmptyEntries),
            TotalProcedures = run.TotalProcedures,
            ProceduresTruncated = run.ProceduresTruncated,
            ErrorMessage = run.ErrorMessage,
            Items = findings.ToList()
        };
    }
}
