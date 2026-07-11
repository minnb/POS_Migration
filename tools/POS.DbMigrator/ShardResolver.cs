using Microsoft.Data.SqlClient;

namespace POS.DbMigrator;

/// <summary>
/// Danh sách shard CentralSale (1 DB/store) — lấy ĐÚNG cách runtime đang làm, xem
/// src/POS.Infrastructure/Database/StoreRoutedConnectionFactory.cs:76.
/// </summary>
public static class ShardResolver
{
    public static async Task<List<string>> GetDistinctServerIPsAsync(string centralGeneralConnectionString, CancellationToken ct)
    {
        await using var conn = new SqlConnection(centralGeneralConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            "SELECT DISTINCT ServerIP FROM RPOSCentralGeneral.dbo.StoreSetServer WHERE ServerIP IS NOT NULL AND ServerIP <> '';",
            conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new List<string>();
        while (await reader.ReadAsync(ct))
            result.Add(reader.GetString(0));
        return result;
    }

    public static string BuildShardConnectionString(string centralSaleTemplate, string serverIP) =>
        centralSaleTemplate.Replace("{server}", serverIP);
}
