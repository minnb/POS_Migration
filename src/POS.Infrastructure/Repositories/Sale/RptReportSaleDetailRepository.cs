using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using POS.Infrastructure.Database;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// Executes the stored procedure <c>Rpt_ReportSaleDetail_Insert</c>.
/// It receives two date parameters but the consumer will always pass the same
/// <c>DateTime.Now.Date</c> for both (current day).
/// </summary>
public sealed class RptReportSaleDetailRepository(
    CentralSaleConnectionFactory directConnectionFactory,
    IFileLogHelper fileLogHelper) : IRptReportSaleDetailRepository
{
    private const int CommandTimeout = 120; // seconds

    public async Task ExecuteInsertAsync(DateTime fromDate, DateTime toDate,
                                          CancellationToken ct = default)
    {
        try
        {
            using var conn = await directConnectionFactory
                .CreateOpenConnectionAsync(ct);

            var cmd = new CommandDefinition(
                "[dbo].[Rpt_ReportSaleDetail_Insert]",
                new
                {
                    FromDate = fromDate,
                    ToDate   = toDate,
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: CommandTimeout,
                cancellationToken: ct);

            await conn.ExecuteAsync(cmd);
        }
        catch (Exception ex)
        {
            // Log to console for quick debugging and to file logs.
            Console.WriteLine($"[RptReportSaleDetailRepository] Exec error: {ex.GetType().Name} - {ex.Message}");
            fileLogHelper.WriteExpLogs(nameof(ExecuteInsertAsync), ex);
            // Rethrow so the hosted service can mark degraded state.
            throw;
        }
    }

}
