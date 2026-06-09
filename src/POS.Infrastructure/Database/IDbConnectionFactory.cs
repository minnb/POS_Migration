using System.Data;

namespace POS.Infrastructure.Database;

/// <summary>
/// Factory tạo connection đến SQL Server. Mỗi DB (CentralMD, Loyalty, StagingDB)
/// có một implementation riêng, đăng ký DI là Singleton.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Tạo và mở connection. Caller chịu trách nhiệm Dispose.</summary>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);

    /// <summary>Đồng bộ — dùng khi gọi từ code chưa async.</summary>
    IDbConnection CreateOpenConnection();
}
