using POS.Common.Dtos.Ops.SpAudit;

namespace POS.Infrastructure.Repositories.Interfaces;

public interface ISpAuditRunRepository
{
    /// <summary>Ghi 1 lần chạy audit + toàn bộ finding, atomic (rollback nếu lỗi giữa chừng).</summary>
    Task SaveRunAsync(SpAuditSnapshotDto snapshot, CancellationToken ct = default);

    /// <summary>Đọc lại lần chạy gần nhất cho Dashboard. Null nếu chưa từng chạy audit.</summary>
    Task<SpAuditSnapshotDto?> GetLatestRunAsync(CancellationToken ct = default);
}
