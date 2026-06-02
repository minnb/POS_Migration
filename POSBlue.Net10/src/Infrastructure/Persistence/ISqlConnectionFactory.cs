using System.Data;

namespace VCM.POSBLUE.Infrastructure.Persistence;

/// <summary>
/// Factory tạo IDbConnection theo tên connection string (đọc từ ConnectionStrings trong appsettings.json).
/// Thay thế DapperContext/DapperConnection + các *Factory.Initialize(connStr) tĩnh của bản cũ.
/// Mỗi lần gọi trả 1 connection mới (chưa Open) — caller tự quản lý vòng đời (using).
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>Tạo connection theo tên (vd: "DAPPER_CENTRAL_MD", "DAPPER_LOYALTY").</summary>
    IDbConnection CreateConnection(string connectionName);

    /// <summary>
    /// Tạo connection từ chuỗi kết nối thô (tương đương DapperContext(connStr) cũ — dùng khi caller
    /// truyền trực tiếp connection string thay vì tên cấu hình).
    /// </summary>
    IDbConnection CreateConnectionFromRaw(string connectionString);
}
