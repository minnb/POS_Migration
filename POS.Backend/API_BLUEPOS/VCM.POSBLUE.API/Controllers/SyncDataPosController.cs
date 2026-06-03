using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using TCX.API.Common.Constants;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.WebApiCore;
using TichHopSAP;
using VCM.POSBLUE.API.Helpers;
using VCM.POSBLUE.API.Models;
using VCM.POSBLUE.Business.Common;
using VCM.POSBLUE.Model.Common;
using VCM.POSBLUE.Model.FileModel;
using VCM.POSBLUE.Model.Request;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api/posblue")]
    public class SyncDataPosController : BaseController
    {
        readonly string username = @"" + ConfigurationManager.AppSettings["remoteSvrUser"];
        readonly string password = @"" + ConfigurationManager.AppSettings["remoteSvrPass"];
        readonly string updFTP = @"" + ConfigurationManager.AppSettings["UploadFileFTP"];
        readonly string rootFolderShare = @"" + ConfigurationManager.AppSettings["FolderShare"];
        readonly string rootfolderShareUpdSource = @"" + ConfigurationManager.AppSettings["FolderShareUpdSource"];
        readonly string folderShareAPIBluePOS = @"" + ConfigurationManager.AppSettings["FolderShareAPIBluePOS"];
        readonly string ConnectStrStagingDB = @"" + ConfigurationManager.ConnectionStrings["DAPPER_STAGINGDB"].ConnectionString;
        readonly string BootstrapServersString = @"" + ConfigurationManager.ConnectionStrings["BootstrapServers"].ConnectionString;
        static readonly string ftpRoot = @"FTPBLUEPOS";
        private readonly string pathFileBackup = HostingEnvironment.MapPath($"/{ftpRoot}/SyncDataPos/Sale/BackupFiles/");
        private readonly CommonBLO _CommonBLO;
        private readonly RedisCacheService _redisService;
        private readonly KibanaService _kibanaService;
        private readonly DataRawService _dataRawService;
        private readonly MemoryCacheService _memoryCacheService;
        public SyncDataPosController()
        {
            _CommonBLO = new CommonBLO();
            _redisService = new RedisCacheService();
            _kibanaService = new KibanaService();
            _dataRawService = new DataRawService();
            _memoryCacheService = new MemoryCacheService();
        }

        [HttpGet]
        [Route("WriteFileByManual")]
        public HttpResponseMessage WriteFileByManual(string storeNo, string posTerminal)
        {
            try
            {
                if (string.IsNullOrEmpty(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "CH/ST đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posTerminal))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "POSTerminal đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                //var isUpdFTP = syncAPI == "NO" ? true : false;
                SyncDataToPos sync = new SyncDataToPos();
                var writeFile = sync.writeFileByManual(storeNo, posTerminal, true, true);

                if (writeFile == "OK")
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = writeFile,
                        Message = writeFile,
                        Status = HttpStatusCode.OK
                    });
                }

                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = writeFile,
                    Status = HttpStatusCode.BadRequest
                });

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API WriteFileByManual: {ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet]
        [Route("GetFileFromFTP")]
        [ValidateModel]
        public HttpResponseMessage GetFileFromFTP([Required(ErrorMessage = "siteCode is required")]  string siteCode,
                                                [Required(ErrorMessage = "posTerminal is required")] string posTerminal, 
                                                string folderFile, string pathSync, string syncAPI, string typeSync)
        {
            _kibanaService.LogRequest($"GetFileFromFTP_{typeSync}", posTerminal, "", $"GetFileFromFTP: {siteCode}_{posTerminal}_{folderFile}_{pathSync}_{syncAPI}_{typeSync}");
            var ipServer = GetIpAddressClient();
            var listResult = new List<PathFileAPIModel>();           
            try
            {
                int limit = 10;
                var checkRequestConfig = _memoryCacheService.GetPOSDataSetup("WWW_LIMIT_REQUEST_MD");
                var linkAPI_DOWN = _memoryCacheService.GetPOSDataSetup("LINKAPI_DOWN").Value ?? "";
                var pathFile = HostingEnvironment.MapPath($"/{ftpRoot}/{pathSync}/{folderFile}");
                FileHelper.CreateFolder(pathFile);
                if (typeSync == "ALL")
                {
                    listResult = GetFileFromServerAPI(pathSync, folderFile, linkAPI_DOWN, typeSync, syncAPI, ipServer);
                    if (listResult != null && listResult.Count > 0)
                    {
                        return ResponseData.HttpResponseData(HttpStatusCode.OK, $"Response from IPServer {ipServer}", listResult);
                    }

                    if (checkRequestConfig != null)
                    {
                        limit = int.Parse(checkRequestConfig.Value);
                    }

                    //check số lượng request tạo file đầu ngày
                    var ipSrv = StringHelper.ReplaceString(GetIpServer(),".", "");
                    int request_processing = 0;
                    if (!_redisService.CheckQueueRequestInRedis(RedisConst.Redis_Key_GetFileFromFTP, ipSrv, limit, ref request_processing))
                    {
                        _kibanaService.LogResponse($"GetFileFromFTP_{typeSync}", posTerminal, 0, "", string.Format("QUEUE: {2} requetst to srv {0} đang xử lý {1} process, vui lòng thử lại sau", ipSrv, request_processing, posTerminal));
                        if(!_redisService.CheckExistsKey(RedisConst.Redis_Key_GetFileFromFTP + ":GetFileFromFTP") && _dataRawService.CreateFile_SOD_Fake(siteCode, pathFile).Item1)
                        {
                            Task.Delay(500).Wait();
                            listResult = GetFileFromServerAPI(pathSync, folderFile, linkAPI_DOWN, typeSync, syncAPI, ipServer);
                            if (listResult != null && listResult.Count > 0)
                            {
                                _kibanaService.LogResponse($"GetFileFromFTP_{typeSync}", posTerminal, 0, "", $"SOD fake: {JsonConvert.SerializeObject(listResult)}");
                                return ResponseData.HttpResponseData(HttpStatusCode.OK, $"Response from IPServer {ipServer}", listResult);
                            }
                        }

                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = "Hệ thống đang xử lý, vui lòng thử lại sau 5 phút",
                            Status = HttpStatusCode.BadRequest,
                            MessageTechnical = $"GetFileFromFTP_ALL_QUEUE {ipServer} đang xử lý {request_processing} tạo file đầu ngày, vui lòng thử lại sau 5 phút"
                        });
                    }

                    //write file SOD
                    string key = StringHelper.InitRandomString(ipSrv);
                    _redisService.Set(key, posTerminal, RedisConst.Redis_Key_GetFileFromFTP, TimeSpan.FromMinutes(10));
                    
                    long time = 0;
                    var st1 = new Stopwatch();
                    st1.Start();
                    SyncDataToPos sync = new SyncDataToPos();
                    var writeFile = sync.writeFileByManual(siteCode, posTerminal, true, false);
                    time = st1.ElapsedMilliseconds;
                    st1.Stop();
                    _kibanaService.LogResponse($"GetFileFromFTP_{typeSync}", posTerminal, time, "", $"{posTerminal} WriteFileByManual {writeFile}, server {ipServer} is processing {request_processing} request");
                    
                    _redisService.Delete(RedisConst.Redis_Key_GetFileFromFTP +":"+key);
                    if (writeFile == "OK")
                    {
                        listResult = GetFileFromServerAPI(pathSync, folderFile, linkAPI_DOWN, typeSync, syncAPI, ipServer);
                        return ResponseData.HttpResponseData(HttpStatusCode.OK, $"Response from IPServer {ipServer} Success {writeFile}", listResult);
                    }
                    else
                    {
                        return ResponseData.HttpResponseData(HttpStatusCode.BadGateway, $"Response from IPServer {ipServer}, Response writeFileByManual: {writeFile}", null);
                    }
                }
                else // ChangeFirst or Change
                {
                    if (syncAPI == "YES")
                    {
                        listResult = GetFileFromServerAPI(rootFolderShare, folderFile, linkAPI_DOWN, typeSync, syncAPI, ipServer);
                    }
                    _kibanaService.LogResponse($"GetFileFromFTP_{typeSync}", typeSync, 0, "", $"Path: {rootFolderShare}\\{typeSync}\\{folderFile} listFileResult:{JsonConvert.SerializeObject(listResult)}");
                    return ResponseData.HttpResponseData(HttpStatusCode.OK, $"Response from IPServer {ipServer}", listResult);
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("GetFileFromFTP_ALL", posTerminal, 0, "", $"GetFileFromFTP_ALL {pathSync}{folderFile} Exception: " + ex.Message.ToString());
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, $"Response from IPServer {ipServer}({JsonConvert.SerializeObject(ex)})", null);
            }
        }

        [HttpPost]
        [Route("UploadFileLogJob")]
        public HttpResponseMessage UploadFileLogJob()
        {
            try
            {
                var httpContext = HttpContext.Current;
                if (httpContext.Request.Files.Count > 0)
                {
                    for (int i = 0; i < httpContext.Request.Files.Count; i++)
                    {
                        HttpPostedFile httpPostedFile = httpContext.Request.Files[i];
                        var folder = httpContext.Request.Files.GetKey(i);

                        var pathFile = HostingEnvironment.MapPath($"/{ftpRoot}/SyncDataPos/General/LogJob/{folder}");
                        FileHelper.CreateFolder(pathFile);

                        if (httpPostedFile != null)
                        {
                            var fileSavePath = Path.Combine(pathFile, httpPostedFile.FileName);
                            if (File.Exists(fileSavePath))
                                File.Delete(fileSavePath);

                            httpPostedFile.SaveAs(fileSavePath);// Upload to Server API                          

                            //SyncDataPos/General/LogJob/
                            //SyncDataPos/TestFile/
                            try
                            {
                                if (updFTP.ToUpper() == "YES")
                                {
                                    UploadFileLogWinSCP(pathFile, $"SyncDataPos/General/LogJob/{folder}/");
                                }
                            }
                            catch (Exception ex)
                            {
                                UtilsAPI.Info("UploadFileLogJob", $"Lỗi upload log job {JsonConvert.SerializeObject(ex)}");
                            }
                        }
                    }
                }

                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", "");
            }
            catch (Exception ex)
            {
                UtilsAPI.Info("UploadFileLogJob", $"Error:{JsonConvert.SerializeObject(ex)}");
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }
        
        [HttpGet]
        [Route("process/sales/retry")]
        public HttpResponseMessage RetryProcessSales()
        {
            try
            {
                var pathFile = HostingEnvironment.MapPath($"/{ftpRoot}/SyncDataPos/Sale/Kafka");
                FileHelper.CreateFolder(pathFileBackup);
                
                if (!Directory.Exists(pathFile))
                {
                    FileHelper.WriteLogs($"NotFound dir {pathFile}");
                    return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, $"NotFound dir {pathFile}", null);
                }

                string[] files = Directory.GetFiles(pathFile);
                FileHelper.WriteLogs($"Start retryProcessSales {files.Length} files ");
                if (files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        FileHelper.WriteLogs($"processing fileName: {file}");
                        string extension = Path.GetExtension(file);
                        string fileName = Path.GetFileName(file);
                        byte[] fileBytes = File.ReadAllBytes(file);
                        _dataRawService.ProcessFileToStagingDB(BootstrapServersString, pathFile, fileName, extension, pathFileBackup);
                        Task.Delay(1000);
                    }
                }
                return ResponseData.HttpResponseData(HttpStatusCode.OK, $"RetryProcessSales {files.Length} file Successfully", "");
            }
            catch(Exception ex)
            {
                FileHelper.WriteLogs($"RetryProcessSales.Exception: " + JsonConvert.SerializeObject(ex));
                return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, ex.Message, null);
            }
        }

        [HttpPost]
        [Route("UploadFileSale")]
        public HttpResponseMessage UploadFileSale()
        {
            var ipServer = GetIpAddressClient();
            List<string> lstFile = new List<string>();
            //var pathFileBackup = HostingEnvironment.MapPath($"/{ftpRoot}/SyncDataPos/Sale/Backup");
            FileHelper.CreateFolder(pathFileBackup);
            try
            {
                var httpContext = HttpContext.Current;
                if (httpContext.Request.Files.Count > 0)
                {
                    for (int i = 0; i < httpContext.Request.Files.Count; i++)
                    {
                        HttpPostedFile httpPostedFile = httpContext.Request.Files[i];
                        var folder = httpContext.Request.Files.GetKey(i);

                        //var pathFile = HostingEnvironment.MapPath($"/{ftpRoot}/SyncDataPos/Sale/DataPos/{folder}");
                        var pathFile = HostingEnvironment.MapPath($"/{ftpRoot}/SyncDataPos/Sale/Kafka");
                        FileHelper.CreateFolder(pathFile);

                        if (httpPostedFile != null)
                        {
                            var fileSavePath = Path.Combine(pathFile, httpPostedFile.FileName);
                            if (File.Exists(fileSavePath))
                                File.Delete(fileSavePath);
                            httpPostedFile.SaveAs(fileSavePath);                    
                            lstFile.Add(httpPostedFile.FileName);

                            string extension = Path.GetExtension(fileSavePath);
                            string fileName = Path.GetFileName(fileSavePath);
                            byte[] fileBytes = File.ReadAllBytes(fileSavePath);

                            Task.Run(() => _dataRawService.ProcessFileToStagingDB(BootstrapServersString, pathFile, fileName, extension, pathFileBackup));
                            //if(_dataRawService.PushDataRawToStagingDB(BootstrapServersString, ConnectStrStagingDB, httpPostedFile, ipServer, fileName, extension, fileBytes).Result)
                            //{
                            //    FileHelper.MoveFileToDestination(fileSavePath, pathFileBackup);
                            //}
                        }
                    }
                }
                _kibanaService.LogRequest("UploadFileSale", ipServer, "", JsonConvert.SerializeObject(lstFile));
                return ResponseData.HttpResponseData(HttpStatusCode.OK, $"Updload file to IPServer {ipServer} Success", "");
            }
            catch (Exception ex)
            {
                _kibanaService.LogException(@"UploadFileSale", ipServer, 0, "", JsonConvert.SerializeObject(ex));
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, $" Response IPServer:{ipServer}({JsonConvert.SerializeObject(ex)})", null);
            }
        }
    
        [HttpPost]
        [Route("DeleteFileFromAPI")]
        public HttpResponseMessage DeleteFileFromAPI(DeleteFileModel model)
        {
            try
            {
                //UrlServer:/SyncDataPos/POS/ALL/1535/153501
                var combineLocal = Path.Combine($"{model.UrlServer}/", model.FileName);
                var filePathDel = HostingEnvironment.MapPath($"/{combineLocal}");

                if (File.Exists(filePathDel))
                {
                    try
                    {
                        File.Delete(filePathDel);
                        //utils.Info("DeleteFileFromAPI", $"Delete file {filePathDel} success");
                        return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", "");
                    }
                    catch (Exception ex)
                    {
                        UtilsAPI.Info("DeleteFileFromAPI", $"Delete file {filePathDel} error {JsonConvert.SerializeObject(ex)}");
                        return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, "Fail", $"Error {JsonConvert.SerializeObject(ex)}");
                    }
                }
                else
                {
                    UtilsAPI.Info("DeleteFileFromAPI", $"Không tồn tại file {model.FileName} trên server API");
                    return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, $"Không tồn tại file {model.FileName} trên server API", "");
                }

            }
            catch (Exception ex)
            {
                UtilsAPI.Info("DeleteFileFromAPI", $"Error {JsonConvert.SerializeObject(ex)}");
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }

        [HttpGet]
        [Route("GetFileScriptFromFTP")]
        public HttpResponseMessage GetFileScriptFromFTP(string siteCode, string folderFile, string pathSync)
        {
            siteCode = "";
            //siteCode : 1535           
            //folderFile : 1535/153501
            //pathSync : SyncDataPos/POS/ScriptDBAuto           
            var listResult = new List<PathFileAPIModel>();
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                var ipServer = addr.LastOrDefault()?.ToString();

                var dataSetup = _CommonBLO.GetDataSetup();
                var pathFile = HostingEnvironment.MapPath($"/{ftpRoot}/{pathSync}/{folderFile}");
                // CreateFolder(pathFile);
                //listResult = utils.DowloadFileFromWinSCPV2(ftpRoot, pathFile, pathSync, folderFile, dataSetup, ipServer);

                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", listResult);
            }
            catch (Exception ex)
            {
                UtilsAPI.Info("GetFileScriptFromFTP", $"Error {JsonConvert.SerializeObject(ex)}");
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, $"{JsonConvert.SerializeObject(ex)}", null);
            }
        }

        [HttpGet]
        [Route("GetFileUpgradeToolFromFTP")]
        public HttpResponseMessage GetFileUpgradeToolFromFTP(string pathSync)
        {

            //pathSync : BluePosUpgrade/Upgrade/UpgradeTool        
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            var ipServer = addr.LastOrDefault()?.ToString();

            var listResult = new List<PathFileAPIModel>();
            try
            {
                var dataSetup = _CommonBLO.GetDataSetup();

                var pathFile = HostingEnvironment.MapPath($"/{ftpRoot}/{pathSync}");

                //pathFile=D:\MASAN\PROJECT\APIBLUEPOS\VCM.POSBLUE.API\FTPBLUEPOS\BluePosUpgrade\Upgrade\UpgradeTool
                FileHelper.CreateFolder(pathFile);

                //listResult = utils.DowloadFileFromShareUpgradeTool(ftpRoot, pathFile, pathSync, dataSetup, ipServer);

                //listResult = utils.DowloadFileFromShareUpgradeTool(rootfolderShareUpdSource, pathFile, "Upgrade", dataSetup, ipServer);
                listResult = UtilsAPI.DowloadFileUpgradeToolShareFolder(rootfolderShareUpdSource, pathFile, "Upgrade", dataSetup, ipServer);

                
                return ResponseData.HttpResponseData(HttpStatusCode.OK, $"Response from IPServer {ipServer} success", listResult);

            }
            catch (Exception ex)
            {
                UtilsAPI.Info("GetFileUpgradeToolFromFTP", $"Response from IPServer {ipServer}({JsonConvert.SerializeObject(ex)})");

                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, $"Response from IPServer {ipServer}({JsonConvert.SerializeObject(ex)})", null);
            }
        }

        [HttpGet]
        [Route("DeleteFileFromRemote")]
        public HttpResponseMessage DeleteFileFromRemote(string pathDisk, string filePath, string ipServer)
        {
            //pathDisk:\\10.233.9.121\C$
            //filePath:\\10.235.19.91\C$\POSBLUE\API\FTPBLUEPOS\SyncDataPos\POS\ALL\1535\153501\153501_20210505161728.zip

            //UtilsAPI utils = new UtilsAPI();
            //try
            //{

            //    var host = Dns.GetHostEntry(Dns.GetHostName());
            //    var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            //    var ipServerHost = addr.LastOrDefault()?.ToString();                

            //    if (ipServer == ipServerHost)
            //    {
            //        var indexOfLength = filePath.IndexOf("FTPBLUEPOS");
            //        var pathDelete = filePath.Substring(indexOfLength, filePath.Length - indexOfLength);//FTPBLUEPOS\SyncDataPos\POS\ALL\1535\153501\153501_20210505161728.zip

            //        var filePathDel = HostingEnvironment.MapPath($@"\{pathDelete}");
            //        try
            //        {
            //            File.Delete(filePathDel);
            //        }
            //        catch (Exception ex)
            //        {
            //            utils.Info("DeleteFileFromRemote", $"Delete localServer file {JsonConvert.SerializeObject(ex)}");

            //        }
            //        return ResponseData.HttpResponseData(HttpStatusCode.OK, $"Delete file local server {ipServer}:{filePathDel}", "");
            //    }

            //    else
            //    {
            //        NetworkCredential credentials = new NetworkCredential(username, password);

            //        using (new ConnectToSharedFolderAPI(pathDisk, credentials))
            //        {
            //            try
            //            {
            //                File.Delete(filePath);
            //            }
            //            catch (Exception ex)
            //            {
            //                utils.Info("DeleteFileFromRemote", $"Delete server SharedFolder file {JsonConvert.SerializeObject(ex)}");

            //            }
            //            return ResponseData.HttpResponseData(HttpStatusCode.OK, $"Delete file SharedFolder:{filePath}", "");
            //        }
            //    }
            //}

            //catch (Exception ex)
            //{
            //    utils.Info("DeleteFileFromRemote", $"Delete download file {JsonConvert.SerializeObject(ex)}");

            //    return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, "Fail", $"Disk:{pathDisk}; PathFile:{filePath}; Error: {JsonConvert.SerializeObject(ex)}");

            //}

            var host = Dns.GetHostEntry(Dns.GetHostName());
            var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            var ipServerHost = addr.LastOrDefault()?.ToString();

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return ResponseData.HttpResponseData(HttpStatusCode.OK, $"Delete file IPserver {ipServerHost} success", "");
                }
                else
                {
                    UtilsAPI.Info("DeleteFileFromRemote", $"Không tồn tại file xóa {filePath}");
                    return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, $"Delete file IPserver {ipServerHost}:{filePath} fail, do không tồn tại file trên FTP", "");
                }
            }

            catch (Exception ex)
            {
                UtilsAPI.Info("DeleteFileFromRemote", $"Response Server {ipServerHost}({JsonConvert.SerializeObject(ex)})");
                return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, $"Response Server {ipServerHost},Disk:{filePath}; PathFile:{filePath}; Error: {JsonConvert.SerializeObject(ex)}", null);

            }
        }

        [HttpGet]
        [Route("DeleteFileFromFTP")]
        public HttpResponseMessage DeleteFileFromFTP(string filePath)
        {
            //pathDisk:\\10.233.9.121\C$
            //filePath:\\10.235.19.91\C$\POSBLUE\API\FTPBLUEPOS\SyncDataPos\POS\ALL\1535\153501\153501_20210505161728.zip
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            var ipServerHost = addr.LastOrDefault()?.ToString();

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return ResponseData.HttpResponseData(HttpStatusCode.OK, $"Delete file IPserver {ipServerHost} success", "");
                }
                else
                {
                    UtilsAPI.Info("DeleteFileFromFTP", $"Không tồn tại file xóa {filePath}");
                    return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, $"Delete file IPserver {ipServerHost}:{filePath} fail, do không tồn tại file trên FTP", "");
                }                
            }
            catch (Exception ex)
            {
                UtilsAPI.Info("DeleteFileFromFTP", $"Response Server {ipServerHost}({JsonConvert.SerializeObject(ex)})");
                return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, $"Response Server {ipServerHost},Disk:{filePath}; PathFile:{filePath}; Error: {JsonConvert.SerializeObject(ex)}", null);
            }
        }

        [HttpPost]
        [Route("DeleteFileExist")]
        public HttpResponseMessage DeleteFileExist(List<PathFileAPIModel> model)
        {
            //pathDisk:\\10.233.9.121\C$
            //filePath:\\10.235.19.91\C$\POSBLUE\\API\FTPBLUEPOS\SyncDataPos\POS\ALL\1535\153501\153501_20210505161728.zip
            try
            {
                // var model = JsonConvert.DeserializeObject<List<PathFileAPIModel>>(JsonModel);

                if (model != null && model.Count > 0)
                {
                    NetworkCredential credentials = new NetworkCredential(username, password);
                    var pathDisk = model.FirstOrDefault().FolderAPI;
                    var disk = model.FirstOrDefault().NetworkPathDisc;

                    var ipServer = model.FirstOrDefault().IPServer;
                    var filePath = model.FirstOrDefault().FilePath;

                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                    var ipServerHost = addr.LastOrDefault()?.ToString();

                    UtilsAPI.Info("DeleteFileFromRemote", $"IPServerClient={ipServer}; IPServerHost={ipServerHost}");

                    if (ipServer == ipServerHost)
                    {
                        var indexOfLength = filePath.IndexOf("FTPBLUEPOS");
                        var pathDelete = filePath.Substring(indexOfLength, filePath.Length - indexOfLength);//FTPBLUEPOS\SyncDataPos\POS\ALL\1535\153501\153501_20210505161728.zip

                        var filePathDel = HostingEnvironment.MapPath($@"\{pathDelete}");
                        try
                        {
                            File.Delete(filePathDel);
                        }
                        catch (Exception ex)
                        {
                            UtilsAPI.Info("DeleteFileExist", $"Delete serverLocal file {JsonConvert.SerializeObject(ex)}");

                        }
                    }
                    else
                    {
                        using (new ConnectToSharedFolderAPI(pathDisk, credentials))
                        {
                            var getListFiles = Directory.GetFiles(pathDisk).Where(f => !f.StartsWith("~$")).Select(f => Path.GetFileName(f)).OrderBy(d => d.ToString()).ToList();

                            foreach (var file in model)
                            {
                                if (getListFiles.Any(d => d.ToString() == file.FileName))
                                {
                                    try
                                    {
                                        File.Delete(file.PathFileIPServer);
                                    }
                                    catch (Exception ex)
                                    {
                                        UtilsAPI.Info("DeleteFileExist", $"Delete server SharedFolder file {JsonConvert.SerializeObject(ex)}");

                                    }
                                }
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                UtilsAPI.Info("DeleteFileExist", $"Error {JsonConvert.SerializeObject(ex)}");

                return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, "Fail", JsonConvert.SerializeObject(ex));
            }

            return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", null);
        }

        [HttpGet]
        [Route("DowloadFileStream")]
        public HttpResponseMessage DowloadFileStream(string fileName, string pathDisk, string filePath)
        {
            try
            {
                long milliseconds = 0;
                var st1 = new Stopwatch();
                st1.Start();

                if (!File.Exists(filePath))
                {
                    return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, $"File: {fileName} không tồn tại", null);
                }
                var readFile = File.ReadAllBytes(filePath);
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(new MemoryStream(readFile))
                };
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-zip-compressed");
                
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                _kibanaService.LogResponse("DowloadFileStream", GetIpServer(), milliseconds, "", $"StatusCode:{response.StatusCode} download file {filePath}{fileName}");
                return response;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs($"DowloadFileStream.Exception",ex);
                return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, $"download {filePath}{fileName} BadRequest from server {GetIpServer()}({JsonConvert.SerializeObject(ex)})", null);
            }
        }

        [HttpGet]
        [Route("ListFile")]
        public HttpResponseMessage ListFile(string fullPath, string extension)
        {
            var listResult = new List<ListFileNameModel>();
            try
            {
                var getFilesZip = Directory.GetFiles(fullPath).Select(f => Path.GetFileName(f)).Where(f => !f.StartsWith("~$") && (f.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase) || extension == "*")).ToList();
                if (getFilesZip.Count > 0)
                {
                    listResult.AddRange(getFilesZip.Select(item => new ListFileNameModel
                    {
                        FileName = item
                    }));
                }

                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", listResult);
            }
            catch (Exception ex)
            {
                UtilsAPI.Info("ListFile", $"Error {JsonConvert.SerializeObject(ex)}");
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, $"{JsonConvert.SerializeObject(ex)}", null);
            }
        }

        [HttpGet]
        [Route("RetryProcessDataRaw")]
        public HttpResponseMessage GetRetryProcessDataRaw()
        {
            return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", _dataRawService.RetryInsDataRawToDB(ConnectStrStagingDB));
        }
        List<PathFileAPIModel> GetFileFromServerAPI(string pathSync, string folderFile, string linkAPI_DOWN, string typeSync, string syncAPI, string IPServer)
        {
            var result = new List<PathFileAPIModel>();
            try
            {
                //ftpRoot = @"FTPBLUEPOS"
                if (syncAPI == "YES")
                {
                    var pathFile = $"{pathSync}\\{typeSync}\\{folderFile}";
                    var pathFileIPServer = $@"{pathSync}\{typeSync}\{folderFile.Replace(@"/", @"\")}";
                    var folderAPI = $@"{pathFile}";
                    var networkPathDisc = $@"{pathSync}";
                    var filePath = $"{linkAPI_DOWN}{ftpRoot}/syncdatapos/pos/{typeSync}/{folderFile}";
                    if (typeSync =="ALL")
                    {
                        //dev-qas-uat
                        if (!string.IsNullOrEmpty(folderShareAPIBluePOS))
                        {
                            IPServer = folderShareAPIBluePOS;
                        }
                        pathFile = HostingEnvironment.MapPath($"/{ftpRoot}/{pathSync}/{folderFile}");
                        filePath = $"{linkAPI_DOWN}{ftpRoot}/{pathSync}/{folderFile}";
                        pathFileIPServer = $@"\\{IPServer}\{ftpRoot}\{pathSync.Replace(@"/", @"\")}\{folderFile.Replace(@"/", @"\")}";
                        networkPathDisc = $@"\\{IPServer}\{ftpRoot}";
                        folderAPI = pathFileIPServer;
                    }
                    var getFilesZip = Directory.GetFiles(pathFile).Select(f => Path.GetFileName(f)).Where(f => !f.StartsWith("~$") && f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
                    if (getFilesZip.Count > 0)
                    {
                        FileHelper.WriteLogs($"GetFiles.pathFile: {getFilesZip.Count} files on {pathFile}");
                        CheckDeleteFileOld(getFilesZip, pathFile);
                        result.AddRange(getFilesZip.Select(item => new PathFileAPIModel
                        {
                            FileName = item,
                            FilePath = $"{filePath}/{item}",
                            IPServer = IPServer,
                            PathFileIPServer = $@"{pathFileIPServer}\{item}",
                            NetworkPathDisc = networkPathDisc,
                            FolderAPI = folderAPI.Replace(@"/", @"\")
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("GetFileFromFTP_CHANGE", folderFile, 0, "", $"GetFileFromFTP_CHANGE.GetFileFromServerAPI {pathSync}{folderFile} Exception: " + ex.Message.ToString());
            }
            return result;
        }
        void UploadFileLogWinSCP(string pathFileAPI, string pathFtpServer)
        {
            try
            {
                var dataSetup = _CommonBLO.GetDataSetup();
                var linkAPI = dataSetup.FirstOrDefault(d => d.Code == "LINKAPI")?.Value ?? "";
                var ftpServer = dataSetup.FirstOrDefault(d => d.Code == "FTPSERVER").Value ?? "";
                var ftpUser = dataSetup.FirstOrDefault(d => d.Code == "FTPUSERNAME").Value ?? "";
                var ftpPass = dataSetup.FirstOrDefault(d => d.Code == "FTPPASSWORD").Value ?? "";

                var getFilesZip = Directory.GetFiles(pathFileAPI).Select(f => Path.GetFileName(f)).Where(f => !f.StartsWith("~$") && f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
                if (getFilesZip.Count > 0)
                {
                    UtilsAPI.UploadFilesLogJobWinSCP(pathFileAPI, pathFtpServer, ftpServer, ftpUser, ftpPass);
                }
            }
            catch (Exception ex)
            {
                UtilsAPI.Info("Log", $"Lỗi upload Log: {JsonConvert.SerializeObject(ex)}");
            }
        }
        private void CheckDeleteFileOld(List<string> getFilesZip, string pathFile)
        {
            try
            {
                for (var i = 0; i < getFilesZip.Count; i++)
                {
                    if (FileHelper.CheckAndDeleteFile(pathFile + @"\" + getFilesZip[i], 2))
                    {
                        getFilesZip.Remove(getFilesZip[i]);
                        FileHelper.WriteLogs($"{getFilesZip[i]}");
                    }
                }
            }
            catch(Exception ex)
            {
               FileHelper.WriteExpLogs("CheckDeleteFileOld.Exception", ex);
            }
        }
    }
}
