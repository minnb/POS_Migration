using System.Data;
using POS.Infrastructure.Persistence;

namespace POS.Infrastructure.Repositories;

public abstract class BaseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    protected BaseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    protected IDbConnection CreateConnection() => _connectionFactory.CreateConnection();

    protected IDbConnection CreateConnectionByName(string name) => _connectionFactory.CreateConnectionByName(name);
}
