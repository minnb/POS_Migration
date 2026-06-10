using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using POS.Application.Interfaces;
using POS.Common.Dtos.CentralMD;
using POS.Common.Dtos.StagingDB;
using POS.Common.Helpers;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Messaging;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;
using IFileLogHelper = POS.Infrastructure.Logging.IFileLogHelper;

namespace POS.Application.Services;

/// <summary>
/// Migrated từ TCX.WebApiCore.DataRawService (API_WebApiCore/AppServices/DataRawService.cs).
/// Khác bản cũ: Kafka qua IKafkaProducer (DI), StoreSet config qua ICentralMDRepository (Redis cache),
/// retry-key qua IRedisService.GetKeysByPattern thay RedisConnectionHelper.
/// </summary>
public sealed class DataRawService(
    ICentralMDRepository centralMDRepository,
    IDataRawJsonRepository dataRawJsonRepository,
    IKafkaProducer kafkaProducer,
    IRedisService redis,
    IKibanaService kibanaService,
    IFileLogHelper fileLogHelper,
    IConfiguration configuration) : IDataRawService
{
    private const string ParentKeyDataRaw = "DATA_RAW";

    public async Task<(bool Success, string Message)> CreateFileSODFakeAsync(string storeNo, string localPath, CancellationToken ct = default)
    {
        var processId = StringHelper.InitRandomString(storeNo);
        var zipFilePath = Path.Combine(localPath, $"{processId}.zip");
        try
        {
            // Giữ nguyên behavior cũ: list bảng hardcode (GetSyncTableList bị comment trong code gốc)
            var lstSyncTable = new List<string> { "CXOfflinePercent" };

            using var memoryStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                int i = 1;
                foreach (var item in lstSyncTable)
                {
                    var fileName = $"{storeNo}_{item}_{StringHelper.InitRandomString("")}_{i}.txt";
                    var zipEntry = zipArchive.CreateEntry(fileName);
                    var textData = new SyncTableList
                    {
                        FileName = fileName,
                        TableName = item,
                        Action = "DELETE-INSERT",
                        ProcedureName = "",
                        ProcessID = processId,
                        Data = null
                    };

                    using var entryStream = zipEntry.Open();
                    using var streamWriter = new StreamWriter(entryStream, Encoding.UTF8);
                    await streamWriter.WriteLineAsync(JsonConvert.SerializeObject(textData));
                    i++;
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write))
            {
                await memoryStream.CopyToAsync(fileStream, ct);
            }

            if (File.Exists(zipFilePath))
                return (true, $"Created file: {zipFilePath}");
            return (false, $"Cannot create file: {zipFilePath}");
        }
        catch (Exception ex)
        {
            kibanaService.LogException("CreateFile_SOD_Fake", "", 0, "", $"CreateFile_SOD_Fake.Exception: {JsonConvert.SerializeObject(ex)}");
            return (false, ex.Message);
        }
    }

    public async Task ProcessFileToStagingDBAsync(string pathFile, string fileName, string? extension, string pathBackup, CancellationToken ct = default)
    {
        try
        {
            var fullFile = Path.Combine(pathFile, fileName);
            if (!File.Exists(fullFile)) return;

            bool flg = true;
            // fileName format: {DataType}_{PosNo...}_... — vd SALE_153501_xxx.zip
            var parts = StringHelper.ConverStringToArray(fileName, '_');
            var store = parts[1].Substring(0, 4);
            var posNo = parts[1].Substring(0, 6);

            var st1 = Stopwatch.StartNew();
            if (extension != null && extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = ZipFile.OpenRead(fullFile);
                foreach (var entry in archive.Entries)
                {
                    using var entryStream = entry.Open();
                    using var memoryEntryStream = new MemoryStream();
                    await entryStream.CopyToAsync(memoryEntryStream, ct);
                    var dataJson = Encoding.UTF8.GetString(memoryEntryStream.ToArray());

                    if (!await PushSalesToKafkaAsync(store, posNo, entry.FullName, dataJson))
                        flg = false;
                }
            }
            st1.Stop();

            if (flg)
            {
                kibanaService.LogResponse("PushSaleFileToKafka", posNo, st1.ElapsedMilliseconds, "", $"processed file: {fileName}");
                MoveFileToDestination(fullFile, pathBackup);
            }
        }
        catch (Exception ex)
        {
            kibanaService.LogException("PushDataRawToStagingDB.Exception", Environment.MachineName, 0, "", fileName + "_|_" + JsonConvert.SerializeObject(ex));
            fileLogHelper.WriteExpLogs("DataRawService.ProcessFileToStagingDBAsync", ex);
        }
    }

    public async Task<List<string>> RetryInsDataRawToDBAsync(CancellationToken ct = default)
    {
        var result = new List<string>();
        try
        {
            var connectStringDb = configuration.GetConnectionString("StagingDB") ?? "";
            var lstKey = redis.GetKeysByPattern($"{ParentKeyDataRaw}*");
            foreach (var item in lstKey)
            {
                var data = await redis.StringGetAsync<List<DataRawJsonDto>>(item);
                if (data?.Count > 0)
                    await ProcessDataRawToDBAsync(connectStringDb, data, ct);
                result.Add(item);
                await Task.Delay(1000, ct);
            }
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("RetryInsDataRawToDB", ex);
        }
        return result;
    }

    private async Task<bool> PushSalesToKafkaAsync(string storeNo, string posNo, string key, string data)
    {
        try
        {
            var storeSet = await centralMDRepository.GetStoreSetConfigAsync();
            var dataStore = storeSet?.FirstOrDefault(x => x.StoreNo == storeNo);
            if (dataStore == null) return false;

            if (await kafkaProducer.PushMessageAsync(dataStore.Name, key, data, storeNo, posNo))
            {
                kibanaService.LogResponse("", posNo, 0, "", data);
                return true;
            }

            // Giữ nguyên behavior cũ: retry 1 lần sau 1s nhưng vẫn trả false
            // → file không bị move backup, lần retry endpoint sau sẽ xử lý lại (dedup ở consumer).
            await Task.Delay(1000);
            await kafkaProducer.PushMessageAsync(dataStore.Name, key, data, storeNo, posNo);
            kibanaService.LogResponse("", posNo, 0, "", data);
            return false;
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteLogs($"{key} PushSalesToKafka.Exception: {ex.Message}");
            return false;
        }
    }

    private async Task ProcessDataRawToDBAsync(string connectStringDb, List<DataRawJsonDto> request, CancellationToken ct)
    {
        var (success, message) = await dataRawJsonRepository.InsertDataRawJsonAsync(connectStringDb, request, ct);
        if (success) return;

        fileLogHelper.WriteLogs("Error " + message + " ====> " + JsonConvert.SerializeObject(request));
        foreach (var item in request)
        {
            var key = $"{ParentKeyDataRaw}:{item.OrderNo}";
            if (!redis.KeyExists(key))
                redis.StringSet(key, request, ttlSeconds: SecondsUntilTomorrowOneAm());
        }
    }

    // Thay DateTimeHelper.GetTimeUntilHourInTomorrow(1) cũ — TTL đến 1h sáng hôm sau
    private static int SecondsUntilTomorrowOneAm()
    {
        var target = DateTime.Now.Date.AddDays(1).AddHours(1);
        return (int)(target - DateTime.Now).TotalSeconds;
    }

    // Port từ FileHelper.MoveFileToDestination cũ
    private void MoveFileToDestination(string file, string destination)
    {
        try
        {
            Directory.CreateDirectory(destination);
            var fileTemp = Path.Combine(destination, Path.GetFileName(file));
            if (File.Exists(fileTemp))
                File.Delete(fileTemp);
            File.Move(file, fileTemp);
        }
        catch (IOException ex)
        {
            fileLogHelper.WriteLogs("IOException MoveFile: " + ex.Message);
        }
    }
}
