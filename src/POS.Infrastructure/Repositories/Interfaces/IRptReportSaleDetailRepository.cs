using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Infrastructure.Repositories.Interfaces
{
    /// <summary>
    /// Repository responsible for executing the stored procedure
    /// Rpt_ReportSaleDetail_Insert with the current date.
    /// </summary>
    public interface IRptReportSaleDetailRepository
    {
        /// <summary>
        /// Executes the stored procedure using the provided dates.
        /// </summary>
        /// <param name="fromDate">Start date (Date component only).</param>
        /// <param name="toDate">End date (Date component only).</param>
        /// <param name="ct">Cancellation token.</param>
        Task ExecuteInsertAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    }
}
