using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Xml;

namespace TCX.API.Common.Helpers
{
    public static class FileHelper
    {
        public static string CreateZipFile(string content, string dirSource, string fileName, string extention)
        {
            string result = "";
            CreateFolder(dirSource);
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var demoFile = archive.CreateEntry(fileName + extention);

                    using (var entryStream = demoFile.Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                    {
                        streamWriter.Write(content);
                    }
                }

                result = dirSource + fileName + @".zip";
                using (FileStream fileStream = new FileStream(result, FileMode.Create))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);

                }

            }
            return result;
        }
        public static XmlDocument ReadXml(string fileName)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(File.ReadAllText(fileName));
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("FileName: " + fileName.ToString());
                FileHelper.WriteLogs("ReadXml Exception: " + ex.Message.ToString());
            }

            return doc;
        }
        public static List<string> GetFileFromDir(string path, string extention)
        {
            var fileName = (new DirectoryInfo(path))
                            .GetFiles(extention, SearchOption.AllDirectories)
                            .Select(a => a.Name)
                            .ToList();
            return fileName;
        }
        public static void WriteExpLogs(string function, Exception ex)
        {
            var path = HostingEnvironment.MapPath($"/") + @"Logs\Exception\";
            try
            {
                CreateFolder(path);
                string fileName = path + @"log-" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                string[] strLine = { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff") + ": " + function + "===>" + JsonConvert.SerializeObject(ex) };
                if (!File.Exists(fileName))
                {
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(path, fileName)))
                    {
                        foreach (string line in strLine)
                            outputFile.WriteLine(line);
                    }
                    DeleteFileHistory(path, 60);
                }
                else
                {
                    File.AppendAllLines(Path.Combine(path, fileName), strLine);
                }
            }
            catch
            {

            }
        }
        public static bool CreateFileMaster(string _fileType, string file_name, string _pathSaveFile, string _str)
        {
            CreateFolder(_pathSaveFile);
            var _filename = _pathSaveFile + file_name + "_" + _fileType + "_" + DateTime.Now.ToString("yyyyMMdd") + "_" + DateTime.Now.ToString("HHmmssfff") + ".txt";
            try
            {
                if (_str != null)
                {
                    File.Create(_filename).Dispose();
                    using (TextWriter tw = new StreamWriter(_filename))
                    {
                        tw.WriteLine(_str);
                        tw.Close();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static string ReadFileTxt(string fileText)
        {
            string line;
            string result = "";
            try
            {
                System.IO.StreamReader file = new System.IO.StreamReader(fileText);
                while ((line = file.ReadLine()) != null)
                {
                    result = line;
                }
                file.Close();
            }
            catch (IOException ex)
            {
                WriteLogs("IOException ReadFileTxt: " + ex.Message.ToString());
            }
            return result;
        }
        public static void WriteLogs(string message)
        {
            var path = HostingEnvironment.MapPath($"/") + @"Logs\debug\";
            try
            {
                CreateFolder(path);
                string fileName = path + @"log-" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                string[] strLine = { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff") + ": " + message };
                if (!File.Exists(fileName))
                {
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(path, fileName)))
                    {
                        foreach (string line in strLine)
                            outputFile.WriteLine(line);
                    }
                    DeleteFileHistory(path, 60);
                }
                else
                {
                    File.AppendAllLines(Path.Combine(path, fileName), strLine);
                }
            }
            catch
            {

            }
        }
        public static void Write2Logs(string func, string message)
        {
            var path = Directory.GetCurrentDirectory() + @"\logs\" + func + @"\";
            try
            {
                CreateFolder(path);
                string fileName = path + @"log-" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                string[] strLine = { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff") + ": " + message };
                if (!File.Exists(fileName))
                {
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(path, fileName)))
                    {
                        foreach (string line in strLine)
                            outputFile.WriteLine(line);
                    }
                    DeleteFileHistory(path, 60);
                }
                else
                {
                    File.AppendAllLines(Path.Combine(path, fileName), strLine);
                }
            }
            catch
            {

            }
        }
        public static bool MoveFileToFolder(string _destination, string _file)
        {
            try
            {
                CreateFolder(_destination);
                FileInfo file = new FileInfo(_file);
                var file_temp = _destination + file.Name;
                if (File.Exists(file_temp))
                {
                    System.IO.File.Delete(file_temp);
                }
                System.IO.File.Move(_file, file_temp);
                return true;
            }
            catch (IOException ex)
            {
                WriteLogs("IOException MoveFile: " + ex.Message.ToString());
                return false;
            }
        }
        public static bool MoveFileToDestination(string _file, string _destination)
        {
            try
            {
                CreateFolder(_destination);
                FileInfo file = new FileInfo(_file);
                var file_temp = _destination + file.Name;
                if (File.Exists(file_temp))
                {
                    System.IO.File.Delete(file_temp);
                }
                System.IO.File.Move(_file, file_temp);
                return true;
            }
            catch (IOException ex)
            {
                WriteLogs("IOException MoveFile: " + ex.Message.ToString());
                return false;
            }
        }
        public static int DeleteFileHistory(string _path, int _numberFile)
        {
            int fileNumber = 0;
            try
            {
                var fileArray = (new DirectoryInfo(_path))
                                    .GetFiles("*.*", SearchOption.AllDirectories).OrderBy(x => x.Name)
                                    .Select(a => a.Name)
                                    .ToList();
                fileNumber = fileArray.Count;
                var numberFileDelete = fileNumber - _numberFile;
                if (numberFileDelete > 0)
                {
                    int i = 0;
                    foreach (var file in fileArray)
                    {
                        if (i == numberFileDelete) return numberFileDelete;
                        if (File.Exists(_path + file)) File.Delete(_path + file);
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("DeleteFileHistory: " + ex.Message.ToString());
            }
            return fileNumber;
        }
        public static int DeleteFileByDateHistory(string _path, int _numberFile)
        {
            int fileNumber = 0;
            try
            {
                if (Directory.Exists(_path))
                {
                    var fileArray = (new DirectoryInfo(_path))
                                    .GetFiles("*.*", SearchOption.AllDirectories).OrderBy(x => x.CreationTime)
                                    .Select(a => a.Name)
                                    .ToList();

                    fileNumber = fileArray.Count;
                    var numberFileDelete = fileNumber - _numberFile;
                    if (numberFileDelete > 0)
                    {
                        int i = 0;
                        foreach (var file in fileArray)
                        {
                            if (i == numberFileDelete) return numberFileDelete;
                            if (File.Exists(_path + file)) File.Delete(_path + file);
                            i++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("DeleteFileHistory: " + ex.Message.ToString());
            }
            return fileNumber;
        }
        public static bool CreateFolder(string _folder)
        {
            try
            {
                if (!Directory.Exists(_folder))
                    Directory.CreateDirectory(_folder);
                return true;
            }
            catch (IOException ex)
            {
                FileHelper.WriteLogs("Create Folder: " + ex.Message.ToString());
                return false;
            }
        }
        public static List<string> GetFileByDir(string path, string filter, int number)
        {
            List<string> lsFile = new List<string>();
            if (path != null)
            {
                string[] files = Directory.GetFiles(path, filter);
                if (files.Length > 0)
                {
                    Array.Sort(files);
                    for (int i = 0; i < files.Length; i++)
                    {
                        lsFile.Add(Path.GetFileName(files[i]));
                        if (number != 0 && i + 1 == number)
                        {
                            break;
                        }
                    }
                }
                return lsFile;
            }
            else
            {
                return lsFile;
            }
        }
        public static bool WriteTxt(string path, string _fileName, string[] _lines)
        {
            try
            {
                if (!File.Exists(path + _fileName))
                {
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(path, _fileName)))
                    {
                        foreach (string line in _lines)
                            outputFile.WriteLine(line);
                    }
                }
                else
                {
                    File.AppendAllLines(Path.Combine(path, _fileName), _lines);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool CheckAndDeleteFile(string filePath, int numberDay)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    DateTime creationTime = fileInfo.LastWriteTime;
                    if ((DateTime.Now - creationTime).TotalDays > numberDay)
                    {
                        fileInfo.Delete();
                        return true;
                    }
                    return false;
                }
                else
                {
                    FileHelper.WriteLogs($"CheckAndDeleteFile: not found {filePath}");
                    return false;
                }
            }
            catch(Exception ex)
            {
                WriteExpLogs("FileHelper.CheckAndDeleteFile.Exception", ex);
                return false;
            }
        }
    }
}
