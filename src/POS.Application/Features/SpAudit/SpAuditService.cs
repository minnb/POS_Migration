using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POS.Common.Dtos.Ops.SpAudit;
using POS.Common.Enums;
using POS.Infrastructure.Repositories;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Features.SpAudit;

public sealed class SpAuditService(
    ISpInventoryRepository inventoryRepository,
    ISpAuditRunRepository runRepository,
    IOptions<SpAuditOptions> options,
    ILogger<SpAuditService> logger) : ISpAuditService
{
    public async Task<SpAuditSnapshotDto> RunAuditAsync(CancellationToken ct = default)
    {
        var opt = options.Value;
        var startedUtc = DateTime.UtcNow;
        var items = new List<SpInventoryItemDto>();
        var errors = new List<string>();
        var truncated = false;

        foreach (var databaseKey in opt.TargetDatabases)
        {
            try
            {
                var rows = await inventoryRepository.GetProcedureInventoryAsync(
                    databaseKey, opt.MaxProceduresPerDatabase, opt.CommandTimeoutSeconds, ct);

                if (rows.Count >= opt.MaxProceduresPerDatabase) truncated = true;

                foreach (var row in rows)
                    items.Add(BuildFinding(databaseKey, row, opt.CleanupRetentionDays));
            }
            catch (Exception ex)
            {
                // 1 database lỗi (permission/timeout/network) không được làm hỏng kết quả các DB còn lại.
                logger.LogError(ex, "[SpAuditService] Quét {DatabaseKey} thất bại", databaseKey);
                errors.Add($"{databaseKey}: {ex.Message}");
            }
        }

        var snapshot = new SpAuditSnapshotDto
        {
            RunStartedUtc = startedUtc,
            RunFinishedUtc = DateTime.UtcNow,
            DatabasesScanned = opt.TargetDatabases,
            TotalProcedures = items.Count,
            ProceduresTruncated = truncated,
            ErrorMessage = errors.Count > 0 ? string.Join("; ", errors) : null,
            Items = items
        };

        await runRepository.SaveRunAsync(snapshot, ct);
        return snapshot;
    }

    public Task<SpAuditSnapshotDto?> GetLatestRunAsync(CancellationToken ct = default)
        => runRepository.GetLatestRunAsync(ct);

    private static SpInventoryItemDto BuildFinding(string databaseKey, SpRawInventoryRow row, int cleanupRetentionDays)
    {
        var (complexity, classifierNote) = SpComplexityClassifier.Classify(row.Definition);
        var lineCount = row.Definition is null ? 0 : SpComplexityClassifier.CountLines(row.Definition);
        var isCalledFromCode = KnownProcedureRegistry.IsCalledFromCode(row.ProcedureName);

        // LastExecutionAt là giờ LOCAL của SQL Server (không phải UTC) — so sánh với DateTime.Now,
        // giả định CLI chạy cùng múi giờ với SQL Server (chấp nhận cho v1, xem ghi chú trong plan).
        var isRecentlyUsed = row.ExecutionCount > 0
            && row.LastExecutionAt is { } lastExec
            && lastExec >= DateTime.Now.AddDays(-cleanupRetentionDays);

        var recommendation = complexity == SpComplexity.Simple && isCalledFromCode
            ? SpRecommendation.MigrationCandidate
            : !isRecentlyUsed
                ? SpRecommendation.CleanupCandidate
                : SpRecommendation.KeepAsIs;

        var note = recommendation == SpRecommendation.CleanupCandidate
            ? AppendNote(classifierNote, "Execution stats reset khi SQL Server restart/recompile — xác minh thực tế trước khi xóa")
            : classifierNote;

        return new SpInventoryItemDto
        {
            Schema = row.SchemaName,
            ProcedureName = row.ProcedureName,
            DatabaseKey = databaseKey,
            CreateDate = row.CreateDate,
            ModifyDate = row.ModifyDate,
            LineCount = lineCount,
            ExecutionCount = row.ExecutionCount,
            LastExecutionAt = row.LastExecutionAt,
            Complexity = complexity,
            IsCalledFromCode = isCalledFromCode,
            Recommendation = recommendation,
            Note = note
        };
    }

    private static string AppendNote(string existing, string addition) =>
        string.IsNullOrEmpty(existing) ? addition : $"{existing}; {addition}";
}
