using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using TCX.API.Common.Const;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.StagingDB;
using TCX.API.Common.Dtos.Tax;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;

namespace TCX.WebApiCore
{
    public class DataRawService
    {
        private readonly KibanaService _kibana;
        private readonly DataRawJsonRepository _dataRawJsonRepository;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly string parentKeyDataRaw = $"DATA_RAW";
        private readonly string parentKeySyncDataPos = $"SyncDataPos";
        public DataRawService()
        {
            _kibana = new KibanaService();
            _dataRawJsonRepository = new DataRawJsonRepository();
            _memoryCacheService = new MemoryCacheService();
        }
        public (bool, string) CreateFile_SOD_Fake(string storeNo, string localPath)
        {
            bool flg = false;
            string processID = StringHelper.InitRandomString(storeNo);
            string zipFilePath = localPath + $"\\{processID}.zip";
            try
            {
                var lstSyncTabe = new List<string> { "CXOfflinePercent" }; // _dataRawJsonRepository.GetSyncTableList();
                if (lstSyncTabe != null && lstSyncTabe.Count > 0) 
                {
                    int i = 1;
                    using (var memoryStream = new MemoryStream())
                    {
                        // create ZipArchive in MemoryStream
                        using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                        {
                            foreach (var item in lstSyncTabe)
                            {
                                string fileName = $"{storeNo}_{item}_{StringHelper.InitRandomString("")}_{i}.txt";
                                var zipEntry = zipArchive.CreateEntry(fileName);
                                var textData = new SyncTableList()
                                {
                                    FileName = fileName,
                                    TableName = item,
                                    Action = "DELETE-INSERT",
                                    ProcedureName = "",
                                    ProcessID = processID,
                                    Data = null
                                };
                                // Write
                                using (var entryStream = zipEntry.Open())
                                using (var streamWriter = new StreamWriter(entryStream, Encoding.UTF8))
                                {
                                    streamWriter.WriteLine(JsonConvert.SerializeObject(textData));
                                }
                                i++;
                            }
                        }

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write))
                        {
                            memoryStream.CopyTo(fileStream);
                        }
                        if(File.Exists(zipFilePath))
                        {
                            flg = true;
                        }
                    }
                    
                    return (flg, $"Created file: {zipFilePath}");
                }
                return (false, "Not found SyncTableList");
            }
            catch (Exception ex) 
            {
                _kibana.LogException("CreateFile_SOD_Fake", "", 0, "", $"CreateFile_SOD_Fake.Exception: {JsonConvert.SerializeObject(ex)}");
                return (false, ex.Message);
            }
        }
        public (bool, string, string) InvoiceCreated(List<InvoiceCreated> invoiceCreated)
        {
            try
            {
                var storeInfo = _memoryCacheService.GetStores().FirstOrDefault(x => x.StoreNo == invoiceCreated.FirstOrDefault().SiteNo);
                if (storeInfo == null)
                {
                    return (false, $"Thông tin hóa đơn {invoiceCreated.FirstOrDefault().OrderNo} không hợp lệ", $"Không tìm thấy cấu hình StoreSet của mã storeNo {invoiceCreated.FirstOrDefault().SiteNo}");
                }
                List<string> lstOrder = new List<string>();
                foreach(var item in invoiceCreated)
                {
                    lstOrder.Add(StringHelper.Left(item.OrderNo, 4));
                }
                foreach(var item in lstOrder)
                {
                    if(invoiceCreated.FirstOrDefault().SiteNo != item)
                    {
                        return (false, $"Danh sách phiếu tính tiền xuất hóa đơn không hợp lệ", JsonConvert.SerializeObject(lstOrder));
                    }
                }
                return _dataRawJsonRepository.InsertInvoiceCreated(invoiceCreated, storeInfo.ConnectionString);
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("InvoiceCreated.Exception", ex);
                return (false, ex.Message, JsonConvert.SerializeObject(ex));
            }
        }
        public (bool, string, object, HttpStatusCode) ValidateTransaction(string orderNo, string appCode = "WCM")
        {
            try
            {
                var storeInfo = _memoryCacheService.GetStores().FirstOrDefault(x=>x.StoreNo == StringHelper.Left(orderNo, 4));
                if(storeInfo == null)
                {
                    return (false, $"Thông tin phiếu tính tiền {orderNo} không hợp lệ", null, HttpStatusCode.Conflict);
                }
                var storeSetup = _memoryCacheService.GetStoreSetup();
                if(storeSetup != null && storeSetup.Count > 0)
                {
                    if (storeSetup.FirstOrDefault(x => x.Code.ToUpper() == "PRINTEINVOICENUMBER" && x.Value.ToUpper() == "ALL") == null) 
                    {
                        var checkStore = storeSetup.FirstOrDefault(x => x.Code.ToUpper() == "PRINTEINVOICENUMBER" && x.StoreNo.ToUpper() == storeInfo.StoreNo.ToUpper() && x.Value.ToUpper() == "YES" && x.Status == true);
                        if (checkStore == null)
                        {
                            return (false, $"Mã cửa hàng {storeInfo.StoreNo} " + _dataRawJsonRepository.GetMessageWarningsVATCheck(VATCheckEnum.NotExistsStore.ToString()), null, HttpStatusCode.BadRequest);
                        }
                    }
                }
                return _dataRawJsonRepository.ValidateTransaction(storeInfo, storeInfo.ConnectionString, orderNo, appCode);
            }
            catch (Exception ex) 
            {
                FileHelper.WriteExpLogs($"{orderNo} DataRawService.ValidateTransaction.Exception", ex);
                return (false, ex.Message, ex, HttpStatusCode.ExpectationFailed);
            }
        }
        public void ProcessDataRawToDB(string connectStringDb, List<DataRawJsonDto> request)
        {
            var check = _dataRawJsonRepository.InsertDataRawJson(connectStringDb, request);
            if (!check.Item1)
            {
                FileHelper.WriteLogs("Error " + check.Item2 + " ====> " + JsonConvert.SerializeObject(request));
                foreach(var item in request)
                {
                    string keyCheck = item.OrderNo;
                    var _redisHelper = new RedisConnectionHelper();
                    var lstKey = _redisHelper.GetKeys(parentKeyDataRaw);
                    if (!lstKey.Contains(keyCheck))
                    {
                        _redisHelper.Set(keyCheck, request, parentKeyDataRaw);
                    }
                }
            }
        }
        public List<string> RetryInsDataRawToDB(string connectStringDb)
        {
            var result = new List<string>();
            try
            {
                var _redisHelper = new RedisConnectionHelper();
                var lstKey = _redisHelper.GetKeys(parentKeyDataRaw);
                if (lstKey != null && lstKey.Count > 0)
                {
                    foreach(var item in lstKey)
                    {
                        var data = _redisHelper.Get<List<DataRawJsonDto>>(item);
                        ProcessDataRawToDB(connectStringDb, data);
                        result.Add(item);
                        Thread.Sleep(1000);
                    }
                }
            }
            catch(Exception ex)
            {
                FileHelper.WriteExpLogs("RetryInsDataRawToDB", ex);
            }
            return result;
        }
        public List<StagingDBConfigDto> GetStagingDBConfig(string connectStringDb)
        {
            return _memoryCacheService.GetStagingDBConfig(connectStringDb);
        }
        public async Task<bool> PushSalesToKafka(string bootstrapKafka, string storeNo, string posNo, string key, string data)
        {
            try
            {
                var getStoreSet = _memoryCacheService.GetStoreSetConfig();
                if (getStoreSet != null) 
                {
                    var dataStore = getStoreSet.FirstOrDefault(x => x.StoreNo == storeNo);
                    if(dataStore != null)
                    {
                       if(await KafkaProducerFactory.PushMessage(bootstrapKafka, dataStore.Name, data, key, storeNo, posNo))
                       {
                            _kibana.LogResponse("", posNo, 0, "", data);
                            return true;
                       }
                       else
                       {
                            await Task.Delay(1000);
                            await KafkaProducerFactory.PushMessage(bootstrapKafka, dataStore.Name, data, key, storeNo, posNo);
                            _kibana.LogResponse("", posNo, 0, "", data);
                        }
                    }
                }
                return false;
            }
            catch(Exception ex)
            {
                FileHelper.WriteLogs($"{key} PushSalesToKafka.Exception: {ex.Message}");
                return false;
            }
        }
        public async void ProcessFileToStagingDB(string connectKafka,string pathFile, string fileName, string extension, string pathBackup)
        {
            try
            {
                string fullFile = pathFile + $"/" + fileName;
                if (File.Exists(fullFile))
                {
                    long milliseconds = 0;
                    bool flg = true;
                    string[] parts1 = StringHelper.ConverStringToArray(fileName, '_');
                    var store = parts1[1].Substring(0, 4);
                    var posNo = parts1[1].Substring(0, 6);
                    var dataType = parts1[0];
                    int count_file_tran = 0;
                    var st1 = new Stopwatch();
                    st1.Start();
                    if (extension != null && extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var stream = ZipFile.OpenRead(fullFile))
                        {
                            foreach (var entry in stream.Entries)
                            {
                                string entryName = entry.FullName;
                                using (var entryStream = entry.Open())
                                {
                                    using (var memoryEntryStream = new MemoryStream())
                                    {
                                        await entryStream.CopyToAsync(memoryEntryStream);
                                        byte[] entryData = memoryEntryStream.ToArray();
                                        string datajson = Encoding.UTF8.GetString(entryData);
                                        if (await PushSalesToKafka(connectKafka, store, posNo, entryName, datajson))
                                        {
                                            count_file_tran++;
                                        }
                                        else
                                        {
                                            flg = false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    if (flg)
                    {
                        _kibana.LogResponse("PushSaleFileToKafka", posNo, milliseconds, "", $"processed file: {fileName}");
                        FileHelper.MoveFileToDestination(fullFile, pathBackup);
                    }
                }
            }
            catch (Exception ex)
            {
                _kibana.SendMessageSMS($"{ComputerHelper.GetHostName()} proccess {fileName} Exception: {ex.Message}", "Error", $"PushDataRawToStagingDB.Exception fileName {fileName}");
                _kibana.LogException(@"PushDataRawToStagingDB.Exception", ComputerHelper.GetHostName(), 0, "", fileName + "_|_" + JsonConvert.SerializeObject(ex));
            }
        }
        public async Task<bool> PushDataRawToStagingDB(string connectKafka, string connectStringDb, HttpPostedFile httpPostedFile, string ipClient, string fileName, string extension, byte[] fileBytes)
        {
            try
            {
                bool flg = true;
                var checkConfig = GetStagingDBConfig(connectStringDb).FirstOrDefault(x => x.Action.ToUpper() == parentKeySyncDataPos.ToUpper());
                if (checkConfig == null)
                {
                    return false;
                }
                var extension_file_process = StringHelper.ConverStringToArray(checkConfig.Execution, ';');
                string[] parts1 = StringHelper.ConverStringToArray(fileName, '_');
                var store = parts1[1].Substring(0, 4);
                var posNo = parts1[1].Substring(0, 6);
                var dataType = parts1[0];
                List<DataRawJsonDto> dataRawJson = new List<DataRawJsonDto>();
                int count_file_tran = 0;
                if (extension != null && extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using (Stream stream = httpPostedFile.InputStream)
                    {
                        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                string entryName = entry.FullName;
                                var arr_type_file = StringHelper.ConverStringToArray(entryName, '_');

                                //filter file type process
                                //if (!extension_file_process.Contains(arr_type_file[0]))
                                //{
                                //    continue;
                                //}
                                count_file_tran++;
                                using (var entryStream = entry.Open())
                                {
                                    using (var memoryEntryStream = new MemoryStream())
                                    {
                                        await entryStream.CopyToAsync(memoryEntryStream);
                                        byte[] entryData = memoryEntryStream.ToArray();
                                        string datajson = Encoding.UTF8.GetString(entryData);

                                        flg = await PushSalesToKafka(connectKafka, store, posNo, entryName, datajson);
                                        
                                        dataRawJson.Add(new DataRawJsonDto()
                                        {
                                            StoreNo = store,
                                            Type = arr_type_file[0],
                                            PosNo = posNo,
                                            DataJson = datajson,
                                            FileName = entryName,
                                            BatchFile = httpPostedFile.FileName,
                                            Srv = ipClient ?? ""
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                if (dataRawJson.Count > 0)
                {
                    ProcessDataRawToDB(connectStringDb, dataRawJson);
                }
                else 
                {
                    if (count_file_tran > 0)
                    {
                        RedisConnectionHelper redisConnectionHelper = new RedisConnectionHelper();
                        redisConnectionHelper.Set(fileName, fileBytes, parentKeySyncDataPos, TimeSpan.FromMinutes(60));
                        _kibana.LogRequest("UnzipFileFromRedis", posNo, "", fileName);
                        
                        if(UnzipFileFromRedis(connectStringDb, redisConnectionHelper, fileName, extension_file_process))
                        {
                            _kibana.LogResponse("UnzipFileFromRedis", posNo,0 ,"", fileName);
                        }
                    }                   
                }
                return flg;
            }
            catch (Exception ex)
            {
                _kibana.LogException(@"PushDataRawToStagingDB.Exception", ComputerHelper.GetHostName(), 0, "", JsonConvert.SerializeObject(ex));
                return false;
            }
        }
        private bool UnzipFileFromRedis(string connectStringDb, RedisConnectionHelper redisConnectionHelper, string key, string[] extension_file_process)
        {
            try
            {
                List<DataRawJsonDto> dataRawJson = new List<DataRawJsonDto>();
                var zipBytes = redisConnectionHelper.Get<byte[]>(parentKeySyncDataPos +":" + key);
                string[] parts1 = StringHelper.ConverStringToArray(key, '_');
                var store = parts1[1].Substring(0, 4);
                var posNo = parts1[1].Substring(0, 6);

                using (MemoryStream zipStream = new MemoryStream(zipBytes))
                {
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            string entryName = entry.FullName;
                            var arr_type_file = StringHelper.ConverStringToArray(entryName, '_');

                            //filter file type process
                            if (!extension_file_process.Contains(arr_type_file[0]))
                            {
                                continue;
                            }
                            using (var entryStream = entry.Open())
                            {
                                using (var memoryEntryStream = new MemoryStream())
                                {
                                    entryStream.CopyToAsync(memoryEntryStream);
                                    byte[] entryData = memoryEntryStream.ToArray();
                                    string datajson = Encoding.UTF8.GetString(entryData);
                                    dataRawJson.Add(new DataRawJsonDto()
                                    {
                                        StoreNo = store,
                                        Type = arr_type_file[0],
                                        PosNo = posNo,
                                        DataJson = datajson,
                                        FileName = entryName,
                                        BatchFile = key,
                                        Srv = ComputerHelper.GetHostName()??""
                                    });
                                }
                            }
                        }
                    }
                }

                if (dataRawJson.Count > 0)
                {
                    ProcessDataRawToDB(connectStringDb, dataRawJson);
                    redisConnectionHelper.Delete(parentKeySyncDataPos + ":" + key);
                }
                return true;
            }
            catch (Exception ex) 
            {
                FileHelper.WriteExpLogs("UnzipFileFromRedis", ex);
                return false;
            }
        }
    }
}