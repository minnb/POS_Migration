using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace POS.Infrastructure.Database;

public sealed class CentralSaleConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString =
        configuration.GetConnectionString("CentralSale")
        ?? throw new InvalidOperationException("ConnectionString 'CentralSale' không tìm thấy.");

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    public IDbConnection CreateOpenConnection()
    {
        var conn = new SqlConnection(_connectionString);
        conn.Open();
        return conn;
    }
}
