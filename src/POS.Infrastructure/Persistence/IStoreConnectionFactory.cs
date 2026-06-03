using System.Data;

namespace POS.Infrastructure.Persistence;

/// <summary>
/// Factory tạo DB connection cho từng store — route theo ServerIP lấy từ CentralGeneral hoặc Redis.
/// Một store có thể có SQL Server riêng, cần resolve IP trước khi tạo connection.
/// </summary>
public interface IStoreConnectionFactory
{
    Task<IDbConnection> CreateSaleConnectionAsync(string storeNo);
    Task<IDbConnection> CreateSaleStagingConnectionAsync(string storeNo);
    Task<IDbConnection> CreateKiosConnectionAsync(string storeNo);
    Task<string> GetServerIpAsync(string storeNo);
}
