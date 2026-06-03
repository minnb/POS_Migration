using System.Data;

namespace POS.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    /// <summary>Tạo connection tới CentralMD (Master Data DB mặc định)</summary>
    IDbConnection CreateConnection();

    /// <summary>Tạo connection tới DB khác (DCMStar, CentralGeneral...) theo tên trong appsettings</summary>
    IDbConnection CreateConnectionByName(string connectionStringName);
}
