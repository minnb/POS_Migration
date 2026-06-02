using System.Data;
using System.Data.SqlClient;

namespace TCX.WebApiCore.EntityFrameworkCore.Dapper
{
    public class DapperContext
    {
        private readonly string _connectionString;
        public DapperContext(string connectionString)
        {
            _connectionString = connectionString;
        }
        public IDbConnection CreateConnDB
        {
            get
            {
                return new SqlConnection(_connectionString);
            }
        }
    }
    public class DapperConnection
    {
        private readonly string _connectString;
        public DapperConnection(string connectString)
        {
            _connectString = connectString;
        }
        public IDbConnection CreateConnDB
        {
            get
            {
                return new SqlConnection(_connectString);
            }
        }
    }
}
