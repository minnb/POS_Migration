using System.Data;
using Dapper;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Infrastructure.Persistence;

/// <summary>
/// Triển khai IPosTerminalRepository bằng Dapper trên DB CentralMD.
/// Tương đương query EF6 cũ: db.POSTerminals.Where(d => d.IPAddress == ip).Select(...).
/// Cột "No" được alias thành TerminalPOS để khớp DTO (giống mapping EF cũ).
/// </summary>
public sealed class PosTerminalRepository : IPosTerminalRepository
{
    private const string SqlByIp = """
        SELECT TOP 1
            IPAddress,
            StoreNo,
            No                              AS TerminalPOS,
            TerminalNetworkID,
            StyleProfile,
            DefaultSalesType,
            SalesTypeFilter,
            Pkey,
            DualDisHost,
            BillNoseri,
            Placement,
            ISNULL(StatementMethod, 0)      AS StatementMethod,
            TerminalStatement,
            ISNULL(TerminalConnection, 0)   AS TerminalConnection,
            PrintReceiptLogo,
            CustomerDisplayText1,
            CustomerDisplayText2,
            ISNULL(PrintReceiptBCType, 0)   AS PrintReceiptBCType,
            InterfaceProfile
        FROM POSTerminals WITH (NOLOCK)
        WHERE IPAddress = @IPAddress;
        """;

    private readonly ISqlConnectionFactory _connectionFactory;

    public PosTerminalRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PosTerminalDto?> GetByIpAddressAsync(string ipAddress)
    {
        using IDbConnection db = _connectionFactory.CreateConnection(ConnectionNames.CentralMD);
        var cmd = new CommandDefinition(SqlByIp, new { IPAddress = ipAddress }, commandTimeout: 120);
        return await db.QuerySingleOrDefaultAsync<PosTerminalDto>(cmd);
    }
}
