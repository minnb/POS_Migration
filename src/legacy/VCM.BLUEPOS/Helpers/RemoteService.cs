using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Security;
using System.Web;
using TichHopSAP;

namespace VCM.BLUEPOS.Helpers
{
    public class RemoteService
    {
        string username = @"" + ConfigurationManager.AppSettings["remoteSvrUser"];
        string password = @"" + ConfigurationManager.AppSettings["remoteSvrPass"];
        public string StartServiceByRemote(string Action, string IP, string serviceName)
        {
            string Status = "Not found";
            try
            {
                //string username = "wmt.blpadmin"; // remote username  
                //string password = "BluePOS@2021"; // remote password  
                ConnectionOptions connectoptions = new ConnectionOptions();
                connectoptions.Username = username;
                connectoptions.Password = password;
                ManagementScope scope = new ManagementScope("\\\\" + IP + "\\root\\cimv2");
                scope.Options = connectoptions;

                //WMI query to be executed on the remote machine  
                SelectQuery query = new SelectQuery("select * from Win32_Service where name = '" + serviceName + "'");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    foreach (ManagementObject service in collection.OfType<ManagementObject>())
                    {
                        if (Action.ToUpper() == "START")
                        {
                            if (service["started"].Equals(false))
                            {
                                //Start the service  
                                service.InvokeMethod("StartService", null);
                            }
                        }
                        else if (Action.ToUpper() == "STOP")
                        {
                            if (service["started"].Equals(true))
                            {
                                //Start the service  
                                service.InvokeMethod("StopService", null);
                            }
                        }
                        else if (Action.ToUpper() == "RESTART")
                        {
                            if (service["started"].Equals(true))
                            {
                                //Start the service  
                                service.InvokeMethod("StopService", null);
                            }
                            service.InvokeMethod("StartService", null);
                        }
                    }
                }
                Status = GetStatusServiceByRemote(IP, serviceName);
            }
            catch (NullReferenceException ex)
            {
                Status = ex.Message;
            }
            return Status;
        }
        public string GetStatusServiceByRemote(string IP, string serviceName)
        {
            string Status = "Not found";
            try
            {
                ConnectionOptions connectoptions = new ConnectionOptions();
                connectoptions.Username = username;
                connectoptions.Password = password;
                ManagementScope scope = new ManagementScope("\\\\" + IP + "\\root\\cimv2");
                scope.Options = connectoptions;

                //WMI query to be executed on the remote machine  
                SelectQuery query = new SelectQuery("select * from Win32_Service where name = '" + serviceName + "'");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    foreach (ManagementObject service in collection.OfType<ManagementObject>())
                    {
                        if (service["started"].Equals(true))
                        {
                            //Start the service  
                            Status = "Started";
                        }
                        else
                        {
                            //Stop the service  
                            Status = "Stopped";
                        }
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                Status = ex.Message;
            }
            return Status;
        }

        public void getService()
        {
            try
            {
                ConnectionOptions con = new ConnectionOptions();
                con.Impersonation = ImpersonationLevel.Impersonate;
                con.Authentication = AuthenticationLevel.Default;
                con.Username = username;
                con.Password = password;
                // con.Authority = "masan:10.220.24.160";
                con.EnablePrivileges = true;

                ManagementScope scope = new ManagementScope(new ManagementPath(@"\\10.220.24.160\root\cimv2"), con);
                scope.Connect();
                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Service WHERE Name = 'MSSQL$SQLEXPRESS'");

                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
                string Status = "";
                foreach (ManagementObject qObj in searcher.Get())
                {
                    PropertyDataCollection serviceProperties = qObj.Properties;
                    foreach (PropertyData serviceProperty in serviceProperties)
                    {
                        if (serviceProperty.Value != null)
                        {
                            if (serviceProperty.Name.ToUpper() == "Started".ToUpper())
                            {
                                Status = serviceProperty.Value.ToString();
                                break;
                            }
                            //Console.WriteLine("Windows service property name: {0}", serviceProperty.Name);
                            //Console.WriteLine("Windows service property value: {0}", serviceProperty.Value);
                        }
                    }
                }
                string StatusJob = Status;
            }
            catch (ManagementException e)
            {
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void QueryService()
        {
            try
            {
                SecureString ss = new SecureString();
                foreach (char c in "BluePOS@2021") ss.AppendChar(c);
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = false;
                proc.RedirectStandardOutput = true;
                proc.Verb = "runas";
                proc.UserName = "wmt.blpadmin";
                proc.Domain = "masan";
                proc.Password = ss;
                proc.CreateNoWindow = true;
                proc.WorkingDirectory = "";
                // proc.FileName = "cmd.exe";
                // proc.Arguments = "sc \\10.220.24.160 start MSSQL$SQLEXPRESS";
                proc.FileName = @"D:\Source\TichHopSAP\bin\Debug\test.bat";
                Process.Start(proc);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public string getStatusServerviceByRemote(string IP, string ServiceName)
        {
            string Status = "Not found";
            try
            {
                ConnectionOptions con = new ConnectionOptions();
                con.Impersonation = ImpersonationLevel.Impersonate;
                con.Authentication = AuthenticationLevel.Default;
                con.Username = username;
                con.Password = password;
                con.EnablePrivileges = true;
                ManagementScope scope = new ManagementScope(new ManagementPath(@"\\" + IP + @"\root\cimv2"), con);
                scope.Connect();
                ObjectQuery query = new ObjectQuery(string.Format("SELECT * FROM Win32_Service WHERE Name = '{0}'", ServiceName));

                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
                foreach (ManagementObject qObj in searcher.Get())
                {
                    PropertyDataCollection serviceProperties = qObj.Properties;
                    foreach (PropertyData serviceProperty in serviceProperties)
                    {
                        if (serviceProperty.Value != null)
                        {
                            if (serviceProperty.Name.ToUpper() == "Started".ToUpper())
                            {
                                Status = serviceProperty.Value.ToString();
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
            return Status;
        }

        public void FileUpload(string IP, string UploadURL)
        {
            string networkPath = $@"\\{IP}\c$\Application";
            //string networkPath = @"\\20-4281-P1\c$";
            NetworkCredential credentials = new NetworkCredential("wmt.blpadmin", "BluePOS@2021");
            string myNetworkPath = string.Empty;
            try
            {
                var pathFolder = $@"{networkPath}\MasterData\Log";
                using (var sharedF = new ConnectToSharedFolder(pathFolder, credentials))
                {
                    var fileList = Directory.GetFiles(pathFolder);
                    foreach (var item in fileList)
                    {
                        if (item.Contains("{ClientDocument}")) { myNetworkPath = item; }
                    }
                    myNetworkPath = myNetworkPath + UploadURL;
                    OpenFolder($@"\\{IP}\c$\Application");
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void OpenFolder(string folderPath)
        {
            try
            {
                var pass = "BluePOS@2021";
                var sspw = new SecureString();

                foreach (var c in pass)
                    sspw.AppendChar(c);

                if (Directory.Exists(folderPath))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        //Arguments = folderPath,
                        UserName = "wmt.blpadmin",
                        Password = sspw,
                        Domain = "masan",
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        FileName = folderPath
                    };

                    Process.Start(startInfo);

                    //ProcessStartInfo viewDir = new System.Diagnostics.ProcessStartInfo(folderPath);
                    //viewDir.UseShellExecute = false;
                    ////viewDir.Domain = netCred.Domain;
                    //viewDir.UserName = "blpadmin";
                    //viewDir.PasswordInClearText = pass;
                    ////viewDir.Arguments = uncPath;
                    //Process.Start(viewDir);


                };
            }
            catch (Exception ex)
            {
            }
        }

        public byte[] DownloadFileByte(string DownloadURL)
        {
            string networkPath = @"\\10-1597-P6\c$";
            NetworkCredential credentials = new NetworkCredential("wmt.blpadmin", "BluePOS@2021");
            string myNetworkPath = string.Empty;
            byte[] fileBytes = null;
            using (new ConnectToSharedFolder(networkPath, credentials))
            {
                var fileList = Directory.GetDirectories(networkPath);
                foreach (var item in fileList) { if (item.Contains("ClientDocuments")) { myNetworkPath = item; } }
                myNetworkPath = myNetworkPath + DownloadURL;
                try
                {
                    fileBytes = File.ReadAllBytes(myNetworkPath);
                }
                catch (Exception ex)
                {
                    string Message = ex.Message.ToString();
                }
            }
            return fileBytes;
        }

        public string KillProcess(string IP, string ProcessName)
        {
            string Status = "Not found";
            try
            {
                SelectQuery query = new SelectQuery("select * from Win32_Process WHERE Name='" + ProcessName + "'");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(CreateScope(IP), query))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    foreach (ManagementObject theCurObject in collection)
                    {
                        theCurObject.InvokeMethod("Terminate", null);
                    }
                    return "OK";
                }
            }
            catch (NullReferenceException ex)
            {
                Status = ex.Message;
            }
            return Status;
        }
        public string RestartComputer(string IP)
        {
            string Status = "Not found";
            try
            {
                SelectQuery query = new SelectQuery("select * from Win32_OperatingSystem");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(CreateScope(IP), query))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    foreach (ManagementObject os in collection)
                    {
                        object result = os.InvokeMethod("Reboot", new object[] { });
                        uint returnValue = (uint)result;

                        if (returnValue != 0)
                        {
                            Status = "Failed to reboot host: " + IP;
                        }
                    }
                    return "OK";
                }
            }
            catch (NullReferenceException ex)
            {
                Status = ex.Message;
            }
            return Status;
        }
        public List<string> checkPerformance(string IP)
        {
            List<string> Result = new List<string>();
            try
            {
                string PhysicalMemory = "";
                SelectQuery query = new SelectQuery("SELECT Capacity FROM Win32_PhysicalMemory");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(CreateScope(IP), query))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    foreach (ManagementObject obj in collection.OfType<ManagementObject>())
                    {
                        PhysicalMemory = Reformat_TB_GB_MB(Convert.ToInt64(obj.GetPropertyValue("Capacity").ToString()));
                    }
                }
                query = new SelectQuery("Select * FROM Win32_ComputerSystem ");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(CreateScope(IP), query))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    foreach (ManagementObject obj in collection.OfType<ManagementObject>())
                    {
                        var Memory = Reformat_TB_GB_MB(Convert.ToInt64(obj.GetPropertyValue("TotalPhysicalMemory").ToString()));
                        var UserName = obj.GetPropertyValue("UserName");
                        Result.Add("User Login :" + UserName);
                        Result.Add("RAM Used :" + Memory + " / " + PhysicalMemory);
                    }
                }

                //Get thong tin cpu
                query = new SelectQuery("SELECT * FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name=\"_Total\"");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(CreateScope(IP), query))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    foreach (ManagementObject obj in collection.OfType<ManagementObject>())
                    {
                        uint cpu_usage = 100 - Convert.ToUInt32(obj["PercentIdleTime"]);
                        Result.Add("CPU used :" + cpu_usage.ToString() + " %");
                    }
                }

                //Get disk Type
                query = new SelectQuery("SELECT * FROM Win32_PerfFormattedData_PerfDisk_LogicalDisk");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(CreateScope(IP), query))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    foreach (ManagementObject obj in collection.OfType<ManagementObject>())
                    {
                        if (obj.GetPropertyValue("Name").ToString() == "C:")
                        {
                            uint disk_usage = 100 - Convert.ToUInt32(obj["PercentIdleTime"]);
                            //PercentIdleTime
                            Result.Add("Disk Percent :" + disk_usage.ToString() + " %");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Result.Add(ex.Message);
            }
            return Result;
        }
        public DataTable GetProcessListByRemote(string IP, string ProcessName = "")
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Caption");
            dt.Columns.Add("Name");
            dt.Columns.Add("CreationDate");
            dt.Columns.Add("CSName");
            dt.Columns.Add("Processid");
            dt.Columns.Add("Memory");
            dt.Columns.Add("OrderBy");
            string Status = "Not found";
            try
            {
                string strQuery = "";
                ProcessName = "";
                if (ProcessName != "")
                {
                    strQuery = "select * from Win32_Process WHERE Name='" + ProcessName + "'";
                }
                else
                {
                    strQuery = "select * from Win32_Process order by Name";
                }
                SelectQuery query = new SelectQuery(strQuery);
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(CreateScope(IP), query))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    string[] sep = { "\n", "\t" };
                    foreach (ManagementObject obj in collection.OfType<ManagementObject>())
                    {
                        string caption = obj.GetText(TextFormat.Mof);
                        string[] split = caption.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        DataRow dr = dt.NewRow();
                        for (int i = 0; i < split.Length; i++)
                        {
                            if (split[i].Split('=').Length > 1)
                            {
                                string par1 = split[i].Split('=')[0];
                                string par2 = split[i].Split('=')[1];
                                par2 = par2.Replace(@"""", "");
                                par2 = par2.Replace(" ", "");
                                par2 = par2.Replace(";", "");
                                switch (par1.Trim().ToLower())
                                {
                                    case "caption":
                                        dr["Caption"] = par2;
                                        break;
                                    case "csname":
                                        dr["CSName"] = par2;
                                        break;
                                    case "name":
                                        dr["Name"] = par2;
                                        break;
                                    case "processid":
                                        dr["Processid"] = par2;
                                        break;
                                    case "creationdate":
                                        dr["CreationDate"] = par2;
                                        break;
                                    case "workingsetsize":
                                        dr["Memory"] = (par2 != "0" ? Reformat_TB_GB_MB(Convert.ToInt64(par2)) : "").ToString();
                                        dr["OrderBy"] = par2 != "0" ? Convert.ToInt64(par2) : 0;
                                        break;
                                }
                            }
                        }
                        dt.Rows.Add(dr);
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                Status = ex.Message;
            }
            return dt;
        }
        private string Reformat_TB_GB_MB(long Value)
        {
            if (Value.ToString().Length > 12)
                return (Value / (double)1024 / 1024 / 1024 / 1024).ToString("##0.00 TB");
            else if (Value.ToString().Length > 9)
                return (Value / (double)1024 / 1024 / 1024).ToString("###,##0.00 GB");
            else if (Value.ToString().Length > 6)
                return (Value / (double)1024 / 1024).ToString("###,###,##0.00 MB");
            else if (Value.ToString().Length > 3)
                return (Value / (double)1024).ToString("###,###,###,##0.00 KB");
            else
                return Value.ToString("###,###,###,###,##0.00 Bytes");
        }
        public ManagementScope CreateScope(string IP)
        {
            ConnectionOptions connectoptions = new ConnectionOptions();
            connectoptions.Username = username;
            connectoptions.Password = password;
            connectoptions.Impersonation = ImpersonationLevel.Impersonate;
            connectoptions.EnablePrivileges = true;
            ManagementScope scope = new ManagementScope("\\\\" + IP + "\\root\\cimv2");
            scope.Options = connectoptions;
            return scope;
        }











    }
}