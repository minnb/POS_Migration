using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Serialization;
using TCX.API.Common.Helpers;
using TCX.WebApiCore;
using VCM.POSBLUE.Model.Common;
using VCM.POSBLUE.Model.FileModel;
using WinSCP;

namespace VCM.POSBLUE.API.Models
{
    public static class UtilsAPI
    {
        private static readonly object _syncObject = new object();
        public static string Serialize<T>(T value)
        {

            if (value == null)
            {
                return null;
            }

            var serializer = new XmlSerializer(typeof(T));

            var settings = new XmlWriterSettings
            {
                Encoding = new UnicodeEncoding(false, false),
                Indent = false,
                OmitXmlDeclaration = false
            };
            // no BOM in a .NET string

            using (var textWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(xmlWriter, value);
                }
                return textWriter.ToString();
            }
        }
        public static T Deserialize<T>(string xml)
        {

            if (string.IsNullOrEmpty(xml))
            {
                return default(T);
            }

            var serializer = new XmlSerializer(typeof(T));

            var settings = new XmlReaderSettings();

            using (var textReader = new StringReader(xml))
            {
                using (var xmlReader = XmlReader.Create(textReader, settings))
                {
                    return (T)serializer.Deserialize(xmlReader);
                }
            }
        }

        public static SessionOptions createSessionOptions(string FtpServer, string ftpUsername, string ftpPassword, bool isSFTP)
        {

            SessionOptions sessionOptions = new SessionOptions
            {
                Protocol = isSFTP == false ? Protocol.Ftp : Protocol.Sftp,
                HostName = FtpServer,
                UserName = ftpUsername,
                Password = ftpPassword,
                GiveUpSecurityAndAcceptAnySshHostKey = isSFTP
            };

            return sessionOptions;
        }
        public static void UploadFilesSaleWinSCP(string sourcePathFolder, string destPathFolderFTP, string FtpServer, string ftpUsername, string ftpPassword)
        {
            try
            {
                string[] files = Directory.GetFiles(sourcePathFolder, @"*.zip").ToArray();
                //FtpServer = "10.233.9.125";
                SessionOptions sessionOptions = createSessionOptions(FtpServer, ftpUsername, ftpPassword, false);
                Session session = new Session();
                session.Open(sessionOptions);

                if (session.Opened == true)
                {
                    TransferOptions transferOptions = new TransferOptions
                    {
                        TransferMode = TransferMode.Binary
                    };
                    transferOptions.ResumeSupport.State = TransferResumeSupportState.Off;

                    if (!session.FileExists(destPathFolderFTP))
                    {
                        session.CreateDirectory(destPathFolderFTP);
                    }
                    var countFile = 0;
                    foreach (string srcFile in files)
                    {
                        TransferOperationResult transferOperationResult = session.PutFiles(srcFile, destPathFolderFTP, false, transferOptions);

                        FileInfo fi = new FileInfo(srcFile);
                        if (transferOperationResult.IsSuccess)
                        {
                            countFile++;
                            try
                            {
                                //Info("Sale", $"Upload file sale {srcFile} Finish");

                                if (File.Exists(srcFile))
                                {
                                    File.Delete(srcFile);
                                    // Info("Sale", $"Delete file sale {srcFile} Finish");
                                }

                            }
                            catch (Exception ex)
                            {
                                Info("Sale", $"File {srcFile} {JsonConvert.SerializeObject(ex)}");

                            }
                        }
                    }

                    //Info("Sale", $"Upload file sale success {countFile} file");
                }
                session.Close();
            }
            catch (Exception ex)
            {
                Info("Sale", $"Upload file sale {JsonConvert.SerializeObject(ex)}");
            }
        }
        public static void UploadFilesLogJobWinSCP(string sourcePathFolder, string destPathFolderFTP, string FtpServer, string ftpUsername, string ftpPassword)
        {
            try
            {
                string[] files = Directory.GetFiles(sourcePathFolder, @"*.zip").ToArray();
                SessionOptions sessionOptions = createSessionOptions(FtpServer, ftpUsername, ftpPassword, false);
                Session session = new Session();
                session.Open(sessionOptions);

                if (session.Opened == true)
                {
                    TransferOptions transferOptions = new TransferOptions();
                    transferOptions.TransferMode = TransferMode.Binary;
                    transferOptions.ResumeSupport.State = TransferResumeSupportState.Off;

                    if (!session.FileExists(destPathFolderFTP))
                    {
                        session.CreateDirectory(destPathFolderFTP);
                    }
                    var countFile = 0;
                    foreach (string srcFile in files)
                    {
                        TransferOperationResult transferOperationResult = session.PutFiles(srcFile, destPathFolderFTP, false, transferOptions);

                        FileInfo fi = new FileInfo(srcFile);
                        if (transferOperationResult.IsSuccess)
                        {
                            countFile++;
                            try
                            {
                                // Info("LogJob", $"Upload file log job {srcFile} Finish");

                                if (File.Exists(srcFile))
                                {
                                    File.Delete(srcFile);
                                    //Info("LogJob", $"Delete file log job {srcFile} Finish");
                                }

                            }
                            catch (Exception ex)
                            {
                                Info("LogJob", $"File log job {srcFile} {JsonConvert.SerializeObject(ex)}");

                            }
                        }
                    }

                    // Info("LogJob", $"Upload file log job success {countFile} file");
                }
                session.Close();
            }
            catch (Exception ex)
            {
                Info("LogJob", $"Upload file log job {JsonConvert.SerializeObject(ex)}");
            }
        }
        public static List<PathFileAPIModel> DowloadFileFromWinSCPV2(string ftpRoot, string pathFileLocal, string pathSync, string folderFile, List<POSDataSetupModel> dataSetup, string IPServer)
        {
            //ftpRoot: FTPBLUEPOS
            //pathFileLocal:D:\MASAN\PROJECT\APIBLUEPOS\VCM.POSBLUE.API\FTPBLUEPOS\SyncDataPos\POS\Change\1535\153501
            var result = new List<PathFileAPIModel>();
            try
            {
                // var linkAPI = dataSetup.FirstOrDefault(d => d.Code == "LINKAPI")?.Value ?? "";

                var linkAPI_DOWN = dataSetup.FirstOrDefault(d => d.Code == "LINKAPI_DOWN")?.Value ?? "";

                var ftpServer = dataSetup.FirstOrDefault(d => d.Code == "FTPSERVER").Value ?? "";
                var ftpUser = dataSetup.FirstOrDefault(d => d.Code == "FTPUSERNAME").Value ?? "";
                var ftpPass = dataSetup.FirstOrDefault(d => d.Code == "FTPPASSWORD").Value ?? "";


                //Info("Download", $"Link api {linkAPI}, ftpServer{ftpServer}, ftpUser{ftpUser}, ftpPass{ftpPass}");

                SessionOptions sessionOptions = createSessionOptions(ftpServer, ftpUser, ftpPass, false);
                Session session = new Session();
                session.Open(sessionOptions);

                if (session.Opened == true)
                {
                    var pathFileFTP = $"/{pathSync}/{folderFile}/";

                    TransferOptions transferOptions = new TransferOptions
                    {
                        TransferMode = TransferMode.Binary,
                        FilePermissions = null,
                        PreserveTimestamp = false
                    };
                    transferOptions.ResumeSupport.State = TransferResumeSupportState.Off;
                    transferOptions.FileMask = "*.zip";

                    TransferOperationResult transferOperationResult = session.GetFiles(pathFileFTP, pathFileLocal, false, transferOptions);
                    foreach (TransferEventArgs transfer in transferOperationResult.Transfers)
                    {
                        //transfer.FileName = /{pathSync}/{folderFile}/153501_20210331.zip
                        var pathDisk = HostingEnvironment.MapPath($"/").Substring(0, 2).Replace(":", "$");//D:\MASAN\PROJECT\APIBLUEPOS\VCM.POSBLUE.API\
                        var pathFileDel = pathFileLocal.Replace(":", "$");
                        var fileName = transfer.FileName.Replace(pathFileFTP, "");

                        result.Add(new PathFileAPIModel
                        {
                            FileName = fileName,
                            FilePath = $"{linkAPI_DOWN}{ftpRoot}{transfer.FileName}",
                            IPServer = IPServer,
                            PathFileIPServer = $@"\\{IPServer}\{pathFileDel}\{fileName}",
                            NetworkPathDisc = $@"\\{IPServer}\{pathDisk}",
                            //FolderAPI = $"{ftpRoot}/{pathSync}/{folderFile}"
                            FolderAPI = $@"\\{IPServer}\{pathFileLocal.Replace(":", "$")}"
                        });

                    }

                    var totalSuccess = transferOperationResult.Transfers.Count();
                    //Info("Download", $"List Download file({totalSuccess}): {JsonConvert.SerializeObject(result)}");

                    int i = 0;
                    foreach (TransferEventArgs transfer in transferOperationResult.Transfers)
                    {
                        RemovalOperationResult removalResult = session.RemoveFiles(RemotePath.EscapeFileMask(transfer.FileName));
                        i++;
                    }
                    //Info("Download", $"Remove file ftp when download success: {i}");
                }
                else
                {
                    Info("Download", $"Download file không kết nối được FTP");
                }
                session.Close();
            }
            catch (Exception ex)
            {
                Info("Download", $"Download file {JsonConvert.SerializeObject(ex)}");
            }
            return result;
        }
        public static List<PathFileAPIModel> DowloadFileFromShare(string rootFolderShare, string pathFileLocal, string typeSync, string folderFile, List<POSDataSetupModel> dataSetup, string IPServer)
        {
            //siteCode : 1535
            //posTerminal : 153501
            //folderFile : 1535/153501
            //typeSync :All ; Change 

            //rootFolderShare:\\Dev-fitweb01\pos

            //\\Dev-fitweb01\pos\ALL\1535\153501

            //pathFileLocal:D:\MASAN\PROJECT\APIBLUEPOS\VCM.POSBLUE.API\FTPBLUEPOS\SyncDataPos\POS\Change\1535\153501

            var result = new List<PathFileAPIModel>();
            try
            {

                var linkAPI_DOWN = dataSetup.FirstOrDefault(d => d.Code == "LINKAPI_DOWN")?.Value ?? "";              

                var pathFileShare = $"/{typeSync}/{folderFile}/";
                var pathFolderDownload = $"FTPBLUEPOS/SyncDataPos/POS/{typeSync}/{folderFile.Replace(@"\", "/")}/";
                var pathFileDel = pathFileLocal.Replace(":", "$");
                var pathDisk = HostingEnvironment.MapPath($"/").Substring(0, 2).Replace(":", "$");

                DirectoryInfo dir = new DirectoryInfo(pathFileLocal);

                foreach (var file in dir.GetFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch { }
                }

                DownFolderToShare(rootFolderShare, pathFileLocal, pathFileShare, true);               

                foreach (var fi in dir.GetFiles())
                {
                    //result.Add(new PathFileAPIModel
                    //{
                    //    FileName = fi.Name,
                    //    FilePath = $"{linkAPI_DOWN}{pathFolderDownload}{fi.Name}",
                    //    IPServer = IPServer,
                    //    PathFileIPServer = $@"\\{IPServer}\{pathFileDel}\{fi.Name}",
                    //    NetworkPathDisc = $@"\\{IPServer}\{pathDisk}",
                    //    FolderAPI = $@"\\{IPServer}\{pathFileLocal.Replace(":", "$")}"
                    //});

                    result.Add(new PathFileAPIModel
                    {
                        FileName = fi.Name,
                        FilePath = $"{linkAPI_DOWN}{pathFolderDownload}{fi.Name}",
                        IPServer = IPServer,
                        PathFileIPServer = $@"\\{IPServer}\ftpbluepos\SyncDataPos\POS\{typeSync}\{folderFile.Replace(@"/", @"\")}\{fi.Name}",
                        NetworkPathDisc = $@"\\{IPServer}\ftpbluepos",
                        FolderAPI = $@"\\{IPServer}\ftpbluepos\SyncDataPos\POS\{typeSync}\{folderFile.Replace(@"/", @"\")}"
                    });
                }

            }
            catch (Exception ex)
            {
                Info("Download", $"Download file {JsonConvert.SerializeObject(ex)}");
            }
            return result;
        }
        public static List<PathFileAPIModel> ListFileFromFTP(KibanaService kibanaService, string rootFolderShare, string pathFileLocal, string typeSync, string folderFile, string linkAPI_DOWN, string IPServer)
        {
            var result = new List<PathFileAPIModel>();
            try
            {
                var pathFolderDownload = $"FTPBLUEPOS/SyncDataPos/POS/{typeSync}/{folderFile.Replace(@"\", "/")}/";               
                var listFileFromFTP = $@"{rootFolderShare}\{typeSync}\{folderFile.Replace(@"/", @"\")}";
                DirectoryInfo dirLocal = new DirectoryInfo(listFileFromFTP);
                foreach (var fi in dirLocal.GetFiles())
                {
                    if (fi.CreationTime >= DateTime.Now.AddDays(-7))
                    {
                        result.Add(new PathFileAPIModel
                        {
                            FileName = fi.Name,
                            FilePath = $"{linkAPI_DOWN}{pathFolderDownload}{fi.Name}",
                            IPServer = IPServer,
                            PathFileIPServer = $@"{rootFolderShare}\{typeSync}\{folderFile.Replace(@"/", @"\")}\{fi.Name}",
                            NetworkPathDisc = $@"{rootFolderShare}",
                            FolderAPI = $@"{rootFolderShare}\{typeSync}\{folderFile.Replace(@"/", @"\")}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                kibanaService.LogException("ListFileFromFTP", $"{rootFolderShare}-{typeSync}", 0, "", $"ListFileFromFTP.Exception: {JsonConvert.SerializeObject(ex)}");
            }
            return result;
        }
        public static List<PathFileAPIModel> DowloadFileFromShareUpgradeTool(string rootFolderShare, string pathFileLocal, string folder, List<POSDataSetupModel> dataSetup, string IPServer)
        {
            //folder :Upgrade
            //rootFolderShare:\\DEV-FITWEB01\BluePosUpgrade
            //pathFileLocal: C:\POSBLUE\API\FTPBLUEPOS\BluePosUpgrade\Upgrade\UpgradeTool

            var result = new List<PathFileAPIModel>();
            try
            {

                var linkAPI_DOWN = dataSetup.FirstOrDefault(d => d.Code == "LINKAPI_DOWN")?.Value ?? "";
                var pathFileShare = $"/{folder}/UpgradeTool/";
                var pathFolderDownload = $"FTPBLUEPOS/{folder}/UpgradeTool/";
                var pathFileDel = pathFileLocal.Replace(":", "$");
                var pathDisk = HostingEnvironment.MapPath($"/").Substring(0, 2).Replace(":", "$");

                DirectoryInfo dir = new DirectoryInfo(pathFileLocal);

                foreach (var file in dir.GetFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch { }
                }

                DownFolderToShare(rootFolderShare, pathFileLocal, pathFileShare, false);

                foreach (var fi in dir.GetFiles())
                {
                    //result.Add(new PathFileAPIModel
                    //{
                    //    FileName = fi.Name,
                    //    FilePath = $"{linkAPI_DOWN}{pathFolderDownload}{fi.Name}",
                    //    IPServer = IPServer,
                    //    PathFileIPServer = $@"\\{IPServer}\{pathFileDel}\{fi.Name}",
                    //    NetworkPathDisc = $@"\\{IPServer}\{pathDisk}",
                    //    FolderAPI = $@"\\{IPServer}\{pathFileLocal.Replace(":", "$")}"
                    //});


                    result.Add(new PathFileAPIModel
                    {
                        FileName = fi.Name,
                        FilePath = $"{linkAPI_DOWN}{pathFolderDownload}{fi.Name}",
                        IPServer = IPServer,
                        PathFileIPServer = $@"\\{IPServer}\ftpbluepos\BluePosUpgrade\Upgrade\UpgradeTool\{fi.Name}",
                        NetworkPathDisc = $@"\\{IPServer}\ftpbluepos",
                        FolderAPI = $@"\\{IPServer}\ftpbluepos\BluePosUpgrade\Upgrade\UpgradeTool"
                    });

                }

            }
            catch (Exception ex)
            {
                Info("Download", $"Download file upgrade tool {JsonConvert.SerializeObject(ex)}");
            }
            return result;
        }
        public static List<PathFileAPIModel> DowloadFileUpgradeToolShareFolder(string rootFolderShare, string pathFileLocal, string folder, List<POSDataSetupModel> dataSetup, string IPServer)
        {
            //folder :Upgrade
            //rootFolderShare:\\DEV-FITWEB01\BluePosUpgrade
            //pathFileLocal: C:\POSBLUE\API\FTPBLUEPOS\BluePosUpgrade\Upgrade\UpgradeTool

            var result = new List<PathFileAPIModel>();
            try
            {

                var linkAPI_DOWN = dataSetup.FirstOrDefault(d => d.Code == "LINKAPI_DOWN")?.Value ?? "";              
              
                var pathFolderDownload = $"FTPBLUEPOS/BluePosUpgrade/{folder}/UpgradeTool/";

                var listFileFromFTP = $@"{rootFolderShare}\Upgrade\UpgradeTool";

                DirectoryInfo dir = new DirectoryInfo(listFileFromFTP);

                foreach (var fi in dir.GetFiles())
                {      
                    result.Add(new PathFileAPIModel
                    {
                        FileName = fi.Name,
                        FilePath = $"{linkAPI_DOWN}{pathFolderDownload}{fi.Name}",
                        IPServer = IPServer,
                        PathFileIPServer = $@"{rootFolderShare}\Upgrade\UpgradeTool\{fi.Name}",
                        NetworkPathDisc = $@"{rootFolderShare}",
                        FolderAPI = $@"{rootFolderShare}\Upgrade\UpgradeTool"
                    });
                }
            }
            catch (Exception ex)
            {
                Info("Download", $"Download file upgrade tool {JsonConvert.SerializeObject(ex)}");
            }
            return result;
        }
        public static List<PathFileAPIModel> DowloadFileUpgradeToolWinSCP(string ftpRoot, string pathFileLocal, string pathSync, List<POSDataSetupModel> dataSetup, string ipServer)
        {
            var result = new List<PathFileAPIModel>();
            try
            {
                //var linkAPI = dataSetup.FirstOrDefault(d => d.Code == "LINKAPI")?.Value ?? "";
                var linkAPI_DOWN = dataSetup.FirstOrDefault(d => d.Code == "LINKAPI_DOWN")?.Value ?? "";
                var ftpServer = dataSetup.FirstOrDefault(d => d.Code == "FTPSERVER").Value ?? "";
                var ftpUser = dataSetup.FirstOrDefault(d => d.Code == "FTPUSERNAME").Value ?? "";
                var ftpPass = dataSetup.FirstOrDefault(d => d.Code == "FTPPASSWORD").Value ?? "";

                //if (!string.IsNullOrEmpty(linkAPIPOS))
                //    linkAPI = linkAPIPOS;

                //var getFilesZip = Directory.GetFiles(pathFileLocal).Select(f => Path.GetFileName(f)).Where(f => !f.StartsWith("~$")).ToList();
                //if (getFilesZip.Count > 0)
                //{
                //    result.AddRange(getFilesZip.Select(a => new PathFileAPIModel
                //    {
                //        FileName = a.ToString(),
                //        FilePath = $"{linkAPI}{ftpRoot}/{pathSync}/{a.ToString()}",
                //        FolderAPI = $"{ftpRoot}/{pathSync}"

                //    }));
                //}
                //else
                //{
                //    Info("Download", $"Không có file từ {linkAPI}{ftpRoot}/{pathSync}");
                //}


                //Info("DownloadUpgrade", $"Link api {linkAPI}, ftpServer{ftpServer}, ftpUser{ftpUser}, ftpPass{ftpPass}");

                SessionOptions sessionOptions = createSessionOptions(ftpServer, ftpUser, ftpPass, false);
                Session session = new Session();
                session.Open(sessionOptions);

                if (session.Opened == true)
                {
                    var pathFileFTP = $"/{pathSync}/";

                    TransferOptions transferOptions = new TransferOptions
                    {
                        TransferMode = TransferMode.Binary,
                        FilePermissions = null,
                        PreserveTimestamp = false
                    };
                    transferOptions.ResumeSupport.State = TransferResumeSupportState.Off;
                    //transferOptions.FileMask = "*.zip";

                    TransferOperationResult transferOperationResult = session.GetFiles(pathFileFTP, pathFileLocal, false, transferOptions);

                    var pathDisk = HostingEnvironment.MapPath($"/").Substring(0, 2).Replace(":", "$");//D:\MASAN\PROJECT\APIBLUEPOS\VCM.POSBLUE.API\
                    var pathFileDel = pathFileLocal.Replace(":", "$");

                    foreach (TransferEventArgs transfer in transferOperationResult.Transfers)
                    {

                        var fileName = transfer.FileName.Replace(pathFileFTP, "");


                        //transfer.FileName:/BluePosUpgrade/Upgrade/UpgradeTool/Upgrade.exe
                        //result.Add(new PathFileAPIModel
                        //{
                        //    FileName = transfer.FileName.Replace(pathFileFTP, ""),
                        //    FilePath = $"{linkAPI}{ftpRoot}{transfer.FileName}",
                        //    FolderAPI = $"{ftpRoot}/{pathSync}"

                        //});

                        result.Add(new PathFileAPIModel
                        {
                            FileName = fileName,
                            FilePath = $"{linkAPI_DOWN}{ftpRoot}{transfer.FileName}",
                            IPServer = ipServer,
                            PathFileIPServer = $@"\\{ipServer}\{pathFileDel}\{fileName}",
                            NetworkPathDisc = $@"\\{ipServer}\{pathDisk}",
                            FolderAPI = $"{ftpRoot}/{pathSync}"

                        });
                    }
                    //var totalSuccess = transferOperationResult.Transfers.Count();
                    //Info("DownloadUpgrade", $"List Download file({totalSuccess}): {JsonConvert.SerializeObject(result)}");
                }
                else
                {
                    Info("DownloadUpgrade", $"Download file không kết nối được FTP");
                }
                session.Close();
            }
            catch (Exception ex)
            {
                Info("DownloadUpgrade", $"Download file {JsonConvert.SerializeObject(ex)}");
            }
            return result;
        }
        public static void Info(string typeFile, string message)
        {
            lock (_syncObject)
            {
                var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff");
                var content = $"{now} {typeFile}       {message}";
                var pathLog = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\debug";
                if (!Directory.Exists(pathLog))
                {
                    Directory.CreateDirectory(pathLog);
                }
                var filepath = pathLog + "\\" + typeFile + "_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".txt";

                if (!File.Exists(filepath))
                {
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sw.WriteLine(content);
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(filepath))
                    {
                        sw.WriteLine(content);
                    }
                }
                if (Environment.UserInteractive)
                {
                    Console.WriteLine(content);
                }
            }
        }

        #region Process File By Share
        public static bool DeleteFileShare(String FolderShare, String PathFile)
        {

            try
            {
                String FullPath = FolderShare + @"\" + PathFile;
                if (File.Exists(FullPath))
                {
                    File.Delete(FullPath);
                }
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        public static void DeleteFilesFolderShare(String FolderShare, string PathFolderName, bool IsRecursive = true)
        {
            String FullPath = FolderShare + @"\" + PathFolderName;
            DirectoryInfo dir = new DirectoryInfo(FullPath);

            foreach (FileInfo fi in dir.GetFiles())
            {
                fi.Delete();
            }
            if (IsRecursive == true)
            {
                foreach (DirectoryInfo di in dir.GetDirectories())
                {
                    DeleteFilesFolderShare(FolderShare, di.FullName);
                    di.Delete();
                }
            }

        }

        public static bool UpFolderToShare(String FolderShare, String PathFolderLocal, String PathFolderShare, bool IsMove = false)
        {
            try
            {
                String FullPathShare = FolderShare + @"\" + PathFolderShare;
                if (!Directory.Exists(PathFolderLocal))
                {
                    return false;
                }

                DirectoryInfo dir = new DirectoryInfo(PathFolderLocal);
                if (IsMove == false)
                {
                    foreach (FileInfo fi in dir.GetFiles())
                    {
                        string DescFileShare = FullPathShare + @"\" + fi.Name;
                        fi.CopyTo(DescFileShare, true);
                    }
                }
                else
                {
                    foreach (FileInfo fi in dir.GetFiles())
                    {
                        string DescFileShare = FullPathShare + @"\" + fi.Name;
                        if (File.Exists(DescFileShare))
                        {
                            File.Delete(DescFileShare);
                        }
                        fi.MoveTo(DescFileShare);
                    }
                }
                return true;
            }
            catch (Exception)
            {

                return false;
            }

        }
        public static bool UpFileToShare(String FolderShare, String PathFullocal, String PathFileShare, bool IsMove = false)
        {
            try
            {
                String FullPathShare = FolderShare + @"\" + PathFileShare;
                if (!File.Exists(PathFullocal))
                {
                    return false;
                }
                if (File.Exists(FullPathShare))
                {
                    File.Delete(FullPathShare);
                }
                FileInfo file = new FileInfo(PathFullocal);
                string FullFileShare = FullPathShare + @"\" + file.Name;
                if (IsMove == false)
                {
                    File.Copy(PathFullocal, FullFileShare, true);
                }
                else
                {
                    if (File.Exists(FullFileShare))
                    {
                        File.Delete(FullFileShare);
                    }
                    File.Move(PathFullocal, FullFileShare);
                }

                return true;
            }
            catch
            {

                return false;
            }

        }

        public static bool DownFileFromShare(String FolderShare, String PathFullocal, String PathFileShare, bool IsMove = false)
        {
            try
            {
                String FullPathShare = FolderShare + @"\" + PathFileShare;
                if (!File.Exists(FullPathShare))
                {
                    return false;
                }

                FileInfo fileShare = new FileInfo(FullPathShare);

                String FileLocal = PathFullocal + @"\" + fileShare.Name;
                if (File.Exists(FileLocal))
                {
                    File.Delete(FileLocal);
                }
                if (IsMove == false)
                {
                    //File.Copy(FullPathShare,PathFullocal, true);
                    fileShare.CopyTo(FileLocal, true);
                }
                else
                {
                    fileShare.MoveTo(FileLocal);
                }

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Information("Exception: " + ex.Message.ToString());
                return false;
            }

        }

        public static bool DownFolderToShare(String FolderShare, String PathFolderLocal, String PathFolderShare, bool IsMove = false)
        {
            try
            {
                String FullPathShare = FolderShare + @"\" + PathFolderShare;
                if (!Directory.Exists(FullPathShare))
                {
                    return false;
                }

                DirectoryInfo dir = new DirectoryInfo(FullPathShare);
                if (IsMove == false)
                {
                    foreach (FileInfo fi in dir.GetFiles())
                    {
                        try
                        {
                            string DescFileLocal = PathFolderLocal + @"\" + fi.Name;
                            fi.CopyTo(DescFileLocal, true);
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                }
                else
                {
                    foreach (FileInfo fi in dir.GetFiles())
                    {
                        try
                        {
                            string DescFileLocal = PathFolderLocal + @"\" + fi.Name;
                            if (File.Exists(DescFileLocal))
                            {
                                File.Delete(DescFileLocal);
                            }
                            fi.MoveTo(DescFileLocal);
                        }
                        catch (Exception)
                        {

                            throw;
                        }

                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Information("Exception: " + ex.Message.ToString());
                return false;
            }

        }
        #endregion
    }
}