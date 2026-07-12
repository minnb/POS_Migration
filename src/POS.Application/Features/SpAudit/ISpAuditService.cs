using POS.Common.Dtos.Ops.SpAudit;

namespace POS.Application.Features.SpAudit;

public interface ISpAuditService
{
    /// <summary>Quét toàn bộ TargetDatabases, phân loại, lưu lịch sử, trả về snapshot vừa chạy.</summary>
    Task<SpAuditSnapshotDto> RunAuditAsync(CancellationToken ct = default);

    /// <summary>Đọc lần chạy gần nhất cho Dashboard. Null nếu chưa từng chạy.</summary>
    Task<SpAuditSnapshotDto?> GetLatestRunAsync(CancellationToken ct = default);
}
