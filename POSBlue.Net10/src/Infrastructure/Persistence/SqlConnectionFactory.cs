using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace VCM.POSBLUE.Infrastructure.Persistence;

/// <summary>
/// Triển khai ISqlConnectionFactory dùng Microsoft.Data.SqlClient (thay System.Data.SqlClient cũ).
/// </summary>
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection(string connectionName)
    {
        var connStr = _configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"Không tìm thấy connection string '{connectionName}' trong cấu hình.");
        return new SqlConnection(connStr);
    }

    public IDbConnection CreateConnectionFromRaw(string connectionString)
        => new SqlConnection(connectionString);
}

/// <summary>Tên các connection string dùng chung — tránh hard-code chuỗi rải rác.</summary>
public static class ConnectionNames
{
    public const string CentralMD = "DAPPER_CENTRAL_MD";
    public const string Loyalty = "DAPPER_LOYALTY";
    public const string Partner = "DAPPER_PARTNER";
    public const string StagingDb = "DAPPER_STAGINGDB";
}
