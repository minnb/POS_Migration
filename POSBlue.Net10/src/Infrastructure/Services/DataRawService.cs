using System.Threading.Tasks;
using VCM.POSBLUE.Application.Interfaces;

namespace VCM.POSBLUE.Infrastructure.Services;

/// <summary>
/// Stub implementation for raw data processing used by SyncDataPosController.
/// Currently provides no‑op methods; replace with real Kafka/Staging DB logic later.
/// </summary>
public class DataRawService : IDataRawService
{
    public Task ProcessFileToStagingDB(string bootstrapServers,
                                      string pathFile,
                                      string fileName,
                                      string extension,
                                      string backupPath)
    {
        // TODO: integrate Kafka producer & DB insertion.
        return Task.CompletedTask;
    }

    public Task<string> RetryInsDataRawToDB(string connectionString)
    {
        // Stub implementation – simply return a success message.
        return Task.FromResult("Retry completed");
    }
}
