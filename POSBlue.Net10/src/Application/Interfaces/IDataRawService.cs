using System.Net.Http;
using System.Threading.Tasks;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Service for handling raw data files (Kafka, Staging DB) used by SyncDataPosController.
/// </summary>
public interface IDataRawService
{
    Task ProcessFileToStagingDB(string bootstrapServers, string pathFile, string fileName, string extension, string backupPath);
    Task<string> RetryInsDataRawToDB(string connectionString);
}
