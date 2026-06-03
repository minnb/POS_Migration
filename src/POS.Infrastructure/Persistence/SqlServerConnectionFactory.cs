using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace POS.Infrastructure.Persistence;

public class SqlServerConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlServerConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
        => new SqlConnection(_configuration.GetConnectionString("CentralMD")
            ?? throw new InvalidOperationException("Connection string 'CentralMD' not found"));

    public IDbConnection CreateConnectionByName(string connectionStringName)
        => new SqlConnection(_configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found"));
}
