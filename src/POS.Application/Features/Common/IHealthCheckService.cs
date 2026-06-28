using POS.Common.Dtos.Ops;

namespace POS.Application.Features.Common;

/// <summary>
/// Check kết nối hạ tầng (Redis, RabbitMQ, SQL CentralMD/CentralGeneral/CentralSale/
/// CentralSaleTemplate) — phục vụ endpoint chẩn đoán api/common/CheckConnection.
/// </summary>
public interface IHealthCheckService
{
    /// <param name="storeNo">
    /// Tùy chọn — khi truyền sẽ test CentralSaleTemplate theo đúng logic routing
    /// StoreSetServer của store này; bỏ trống thì test bằng SetDb:DB1.
    /// </param>
    Task<List<HealthCheckItemDto>> CheckAllAsync(string? storeNo, CancellationToken ct = default);
}
