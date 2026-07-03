using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Newtonsoft.Json;
using System.Globalization;
using System.Threading.Tasks;
using TichHopSAP;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.SyncData;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Business.SyncData;
using VCM.BLUEPOS.Business.Common;
using System.Data.SqlClient;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Store;
using VCM.BLUEPOS.Model.Store;
using System.Text;
using System.Data;
using System.Net.NetworkInformation;
using System.Configuration;
using System.Net;
using VCM.BLUEPOS.Business.SalesStaging;
using VCM.BLUEPOS.Model.StoreActivities;
using VCM.BLUEPOS.Model.SalesStaging;
using PLG.Controllers;


namespace PLG.Controllers
{
    public class SyncDataController : BaseController
    {
        private ISyncDataBLO _syncDataBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;
        private IStoreBLO _storeBLO;
        private ISalesStagingBLO _stagingBLO;

        private string POSUser = @"" + ConfigurationManager.AppSettings["POSUser"];
        private string POSPass = @"" + ConfigurationManager.AppSettings["POSPass"];

        private string agentUser = @"" + ConfigurationManager.AppSettings["AgentUser"];
        private string agentPass = @"" + ConfigurationManager.AppSettings["AgentPass"];

        public SyncDataController(ISyncDataBLO syncDataBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO, IStoreBLO storeBLO, ISalesStagingBLO stagingBLO)
        {
            _syncDataBLO = syncDataBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
            _storeBLO = storeBLO;
            _stagingBLO = stagingBLO;
        }


        [DisplayName("Đồng bộ dữ liệu đầu ngày")]
        public ActionResult SyncDataByDate()
        {
            ViewBag.ListStore = _storeBLO.ListStorePartner();
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> DeleteSynDateLog(string PosTerminal)
        {
            var result = new List<Tuple<string, string, bool>>();
            try
            {
                var listIP = PosTerminal.Split(new char[] { ',' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                if (!string.IsNullOrEmpty(PosTerminal))
                {
                    var tasks_delete = new List<Task>();
                    foreach (var item in listIP)
                    {
                        tasks_delete.Add(Task.Run(() => result.Add(DeleteSynDate(item))));
                    }

                    Task taskDel = Task.WhenAll(tasks_delete.ToArray());
                    await taskDel;

                    if (taskDel.Status == TaskStatus.RanToCompletion)
                    {
                    }
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                result.Add(new Tuple<string, string, bool>("", "", false));
            }
            return Json(result);
        }
        public Tuple<string, string, bool> DeleteSynDate(string IPAddress)
        {
            var messagePort = "";
            string SqlScript = "TRUNCATE TABLE SyncDateLog";
            if (VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 1433, ref messagePort))
            {
                string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectStr))
                    {
                        SqlCommand command = new SqlCommand(SqlScript, connection);
                        try
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                            connection.Close();
                            return new Tuple<string, string, bool>(IPAddress, "Success", true);
                        }
                        catch (Exception ex)
                        {
                            return new Tuple<string, string, bool>(IPAddress, ex.Message, false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new Tuple<string, string, bool>(IPAddress, ex.Message, false);
                }
            }
            else
            {
                return new Tuple<string, string, bool>(IPAddress, "Không telnet được Port 1433", false);
            }
        }

        [DisplayName("Đồng bộ dữ liệu theo bảng")]
        public ActionResult SyncDataByTable()
        {
            ViewBag.ListStore = _storeBLO.ListStorePartner();
            ViewBag.ListTableSync = _commonBLO.ListTableSync();
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> GetSyncDataByTable(string StoreNo, string GroupName, string TypeCounter, string Counter)
        {
            var result = new List<Tuple<string, string, bool>>();
            var dataResult = new List<SyncJobLogModel>();
            try
            {
                var listStore = StoreNo.Split(new char[] { ',' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                if (listStore.Count > 0)
                {
                    string ProcessID = DateTime.Now.ToString("ddMMyyyyHHmmss");
                    var tasks = new List<Task>();
                    var isALL = TypeCounter == "ALL" ? true : false;
                    var counter = TypeCounter == "ALL" ? "0" : TypeCounter == "LAST" ? "0" : Counter;

                    foreach (var item in listStore)
                    {
                        tasks.Add(Task.Run(() => result.Add(WriteFile(item, GroupName, isALL, true, counter, ProcessID))));
                    }
                    Task taskW = Task.WhenAll(tasks.ToArray());
                    await taskW;
                    dataResult = _commonBLO.ListLogSyncJob(ProcessID);
                }
            }
            catch (Exception ex)
            { }
            return Json(dataResult);
        }

        public Tuple<string, string, bool> WriteFile(string StoreNo, string GroupName, bool isAll, bool isFTP, string Counter, string ProcessID)
        {
            if (!string.IsNullOrEmpty(StoreNo) && !string.IsNullOrEmpty(GroupName))
            {
                try
                {
                    SyncDataToPos sync = new SyncDataToPos();
                    var result = sync.WriteFileByManualV2(StoreNo, GroupName, isAll, true, Counter, ProcessID);
                    //var result = sync.(StoreNo, GroupName, isAll, true, Counter, ProcessID);
                    if (result == "OK")
                    {
                        return new Tuple<string, string, bool>(StoreNo, "Success", true);
                    }
                    else
                    {
                        return new Tuple<string, string, bool>(StoreNo, result, false);
                    }
                }
                catch (Exception ex)
                {
                    return new Tuple<string, string, bool>(StoreNo, ex.Message, false);
                }
            }
            else
            {
                return new Tuple<string, string, bool>(StoreNo, $"Không tìm thấy ST/CH hoặc groupName", false);
            }
        }

        [DisplayName("Đồng bộ dữ liệu SAP")]
        public ActionResult SyncDataBySAP()
        {
            ViewBag.ListConfigXMLSAP = _commonBLO.ListConfigXMLSAP();
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> GetSyncDataBySAP(string ListTransType)
        {
            var result = new List<SAPLogModel>();
            try
            {
                var pathDisk = AppDomain.CurrentDomain.BaseDirectory + "\\WriteSAP";
                if (!Directory.Exists(pathDisk))
                {
                    Directory.CreateDirectory(pathDisk);
                }

                var listTrans = ListTransType.Split(new char[] { ',' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                if (listTrans.Count > 0)
                {
                    var tasks = new List<Task>();
                    string ProcessID = DateTime.Now.ToString("ddMMyyyyHHmmss");
                    foreach (var item in listTrans)
                    {
                        tasks.Add(Task.Run(() => WriteXML(item, pathDisk, ProcessID)));
                    }

                    Task taskW = Task.WhenAll(tasks.ToArray());
                    await taskW;
                    result = _syncDataBLO.ListLogSAP(ProcessID);
                }
            }
            catch {
            }
            return Json(result);
        }

        public void WriteXML(string TransType, string pathDisk, string ProcessID)
        {
            try
            {
                TichHopSAP.Utils util = new TichHopSAP.Utils();
                WriteXML writexml = new WriteXML();
                if (util.CheckJobFinish("SAP", TransType) == true)
                {
                    util.JobRunningUpdate("SAP", TransType, 1);
                    var result = writexml.readXML(TransType, pathDisk, "1", ProcessID);
                }
                util.JobRunningUpdate("SAP", TransType, 0);
            }
            catch (Exception ex) {
            }
        }


        [DisplayName("Query script")]
        public ActionResult QueryScript()
        {
            var listStoreNo = new List<StoreModel>();
            try
            {
                listStoreNo = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName).ToList();
            }
            catch
            {
            }
            return View(listStoreNo);
        }

        [HttpPost]
        [ParentAuthorize("QueryScript")]
        public ActionResult LoadTerminalByStoreNo(string StoreNo)
        {
            try
            {
                if (!string.IsNullOrEmpty(StoreNo))
                {
                    var data = _storeBLO.LoadTerminalByStoreNo(StoreNo);
                    return Json(data);
                }
            }
            catch
            {
            }
            return Json(new List<IPAddressModel>());
        }

        [HttpPost]
        [ParentAuthorize("QueryScript")]
        public ActionResult LoadStoreNo()
        {
            try
            {
                var data = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName).ToList();
                return Json(data);
            }
            catch
            {
            }
            return Json(new List<ArraySiteNoModel>());
        }

        [HttpPost]
        [ParentAuthorize("QueryScript")]
        public ActionResult LoadTableByPOS(string IPAddress)
        {
            var result = new List<DataTableModel>();
            try
            {
                if (!string.IsNullOrEmpty(IPAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");
                    var SqlScript = "SELECT * FROM information_schema.tables order by TABLE_NAME";
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            SqlCommand command = new SqlCommand(SqlScript, connection);
                            try
                            {
                                StringBuilder sb = new StringBuilder();
                                connection.Open();

                                var reader = command.ExecuteReader();
                                try
                                {
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    if (countData > 0)
                                    {
                                        var dataTableJson = JsonConvert.SerializeObject(dataTable);
                                        result = JsonConvert.DeserializeObject<List<DataTableModel>>(dataTableJson);
                                    }

                                }
                                finally
                                {
                                    reader.Close();
                                }

                                connection.Close();

                            }
                            catch
                            {

                            }
                        }
                    }
                    catch
                    {

                    }
                    return Json(result);
                }
            }
            catch
            {

            }
            return Json(result);
        }

        [HttpPost]
        [ParentAuthorize("QueryScript")]
        public ActionResult LoadColumnByTable(string IPAddress, string TableName)
        {
            var result = new List<ColumnNameModel>();
            try
            {
                if (!string.IsNullOrEmpty(IPAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");
                    var SqlScript = string.Format("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}'", TableName);
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            SqlCommand command = new SqlCommand(SqlScript, connection);
                            try
                            {
                                //var dataTableJson = "";
                                StringBuilder sb = new StringBuilder();
                                connection.Open();

                                var reader = command.ExecuteReader();
                                try
                                {
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    if (countData > 0)
                                    {
                                        var dataTableJson = JsonConvert.SerializeObject(dataTable);
                                        result = JsonConvert.DeserializeObject<List<ColumnNameModel>>(dataTableJson);
                                    }
                                }
                                finally
                                {
                                    reader.Close();
                                }
                                connection.Close();
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch
                    {
                    }
                    return Json(result);
                }
            }
            catch
            {

            }
            return Json(result);
        }

        [HttpPost]
        [ParentAuthorize("QueryScript")]
        public ActionResult LoadServerSql()
        {
            try
            {
                var data = _commonBLO.ListSQLServerExcute();
                return Json(data);
            }
            catch
            {
            }
            return Json(new List<SQLServerModel>());
        }

        [HttpPost]
        [ParentAuthorize("QueryScript")]
        public ActionResult LoadDatabaseServer(string IPServer, string UserSA, string Password)
        {
            var result = new List<DataTableSvrModel>();
            try
            {
                if (!string.IsNullOrEmpty(IPServer))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPServer, UserSA, Password, "master");
                    var SqlScript = "SELECT a.name as DatabaseName FROM sys.databases(nolock)a where a.database_id>4";
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            SqlCommand command = new SqlCommand(SqlScript, connection);
                            try
                            {
                                StringBuilder sb = new StringBuilder();
                                connection.Open();

                                var reader = command.ExecuteReader();
                                try
                                {
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    if (countData > 0)
                                    {
                                        var dataTableJson = JsonConvert.SerializeObject(dataTable);
                                        result = JsonConvert.DeserializeObject<List<DataTableSvrModel>>(dataTableJson);
                                    }

                                }
                                finally
                                {
                                    reader.Close();
                                }

                                connection.Close();

                            }
                            catch
                            {

                            }
                        }
                    }
                    catch
                    {

                    }
                    return Json(result);
                }
            }
            catch
            {

            }
            return Json(result);
        }

        [HttpPost]
        [ParentAuthorize("QueryScript")]
        public ActionResult LoadTableByDatabaseSvr(string IPServer, string UserSA, string Password, string Database)
        {
            var result = new List<DataTableModel>();
            try
            {
                if (!string.IsNullOrEmpty(Database))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPServer, UserSA, Password, Database);
                    var SqlScript = "SELECT * FROM information_schema.tables order by TABLE_NAME";
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            SqlCommand command = new SqlCommand(SqlScript, connection);
                            try
                            {
                                StringBuilder sb = new StringBuilder();
                                connection.Open();

                                var reader = command.ExecuteReader();
                                try
                                {
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    if (countData > 0)
                                    {
                                        var dataTableJson = JsonConvert.SerializeObject(dataTable);
                                        result = JsonConvert.DeserializeObject<List<DataTableModel>>(dataTableJson);
                                    }

                                }
                                finally
                                {
                                    reader.Close();
                                }

                                connection.Close();

                            }
                            catch
                            {

                            }
                        }
                    }
                    catch
                    {

                    }
                    return Json(result);
                }
            }
            catch
            {

            }
            return Json(result);
        }

        [HttpPost]
        [ParentAuthorize("QueryScript")]
        public ActionResult LoadColumnServerByTable(string IPServer, string UserSA, string Password, string Database, string TableName)
        {
            var result = new List<ColumnNameModel>();
            try
            {
                if (!string.IsNullOrEmpty(TableName))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPServer, UserSA, Password, Database);
                    var SqlScript = string.Format("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}'", TableName);
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            SqlCommand command = new SqlCommand(SqlScript, connection);
                            try
                            {
                                StringBuilder sb = new StringBuilder();
                                connection.Open();

                                var reader = command.ExecuteReader();
                                try
                                {
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    if (countData > 0)
                                    {
                                        var dataTableJson = JsonConvert.SerializeObject(dataTable);
                                        result = JsonConvert.DeserializeObject<List<ColumnNameModel>>(dataTableJson);
                                    }
                                }
                                finally
                                {
                                    reader.Close();
                                }
                                connection.Close();
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch
                    {

                    }
                    return Json(result);
                }
            }
            catch
            {

            }
            return Json(result);
        }

        [HttpPost]
        [ParentAuthorize("QueryScript")]
        public ActionResult ExcuteScriptToPOS(string IPAddress, string SqlScript)
        {
            ResultExecuteModel result = new ResultExecuteModel();
            try
            {
                bool bIsConnected = false;

                var messagePort = "";
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 1433, ref messagePort))
                {
                    bIsConnected = false;
                    result.Status = 0;
                    result.IPAddress = IPAddress;
                    result.Message = $"Không telnet được Port 1433";
                }
                else
                {
                    bIsConnected = true;
                }

                if (bIsConnected)
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {

                            SqlCommand command = new SqlCommand(SqlScript, connection);
                            try
                            {
                                //var dataTableJson = "";
                                StringBuilder sb = new StringBuilder();
                                connection.Open();

                                var reader = command.ExecuteReader();
                                try
                                {
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    if (countData > 0)
                                    {
                                        string[] columnNames = (from dc in dataTable.Columns.Cast<DataColumn>()
                                                                select dc.ColumnName).ToArray();
                                        var dataRow = (from row in dataTable.Rows.Cast<DataRow>()
                                                       select row.ItemArray).ToArray();
                                        sb.Append("");
                                        sb.Append("<table id=\"tableResult\" class=\"table table-bordered table-striped\" style=\"\">");
                                        sb.Append("<thead>");
                                        sb.Append("<tr>");
                                        foreach (var header in columnNames)
                                        {
                                            sb.Append("<th>" + header + "</th>");
                                        }
                                        sb.Append("</tr>");
                                        sb.Append("</thead>");
                                        sb.Append("</tbody>");
                                        for (int i = 0; i < dataRow.Length; i++)
                                        {
                                            var item = dataRow[i];

                                            sb.Append("<tr>");
                                            for (int j = 0; j < item.Length; j++)
                                            {
                                                sb.Append("<td>" + item[j] + "</td>");
                                            }
                                            sb.Append("</tr>");
                                        }
                                        sb.Append("</tbody>");
                                        sb.Append("</table>");
                                    }
                                    else
                                    {
                                        sb.Append("Không có data");
                                    }

                                }
                                finally
                                {
                                    reader.Close();
                                }
                                result.Status = 1;
                                result.IPAddress = IPAddress;
                                result.Message = sb.ToString();
                                connection.Close();

                            }
                            catch (Exception ex)
                            {
                                result.Status = 0;
                                result.IPAddress = IPAddress;
                                result.Message = ex.Message;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = 0;
                        result.IPAddress = IPAddress;
                        result.Message = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Status = 0;
                result.IPAddress = IPAddress;
                result.Message = ex.Message;
            }
            return Json(result);
        }

        [HttpGet]
        [ParentAuthorize("QueryScript")]
        public ActionResult ExcuteScriptToPOSExcel(string IPAddress, string SqlScript)
        {
            try
            {
                bool bIsConnected = false;

                var messagePort = "";
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 1433, ref messagePort))
                {
                    bIsConnected = false;
                }
                else
                {
                    bIsConnected = true;
                }

                if (bIsConnected)
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");


                    using (SqlConnection connection = new SqlConnection(connectStr))
                    {

                        SqlCommand command = new SqlCommand(SqlScript, connection);

                        StringBuilder sb = new StringBuilder();
                        connection.Open();

                        var reader = command.ExecuteReader();
                        try
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);


                            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                            using (var package = new ExcelPackage())
                            {
                                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

                                worksheet.Cells["A1"].LoadFromDataTable(dataTable, true).AutoFitColumns();


                                string fileName = $"ExcelQueryPOS_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                                using (var memoryStream = new MemoryStream())
                                {
                                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                                    Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                                    package.SaveAs(memoryStream);
                                    memoryStream.WriteTo(Response.OutputStream);
                                    Response.Flush();
                                    Response.End();
                                }
                            }

                        }
                        finally
                        {
                            reader.Close();
                            connection.Close();
                        }

                    }

                }
            }
            catch
            {

            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ParentAuthorize("QueryScript")]
        public ActionResult ExcuteScriptToServer(string IPServer, string UserSA, string Password, string Database, string SqlScript)
        {
            ResultExecuteModel result = new ResultExecuteModel();            
            try
            {
                var ipServer = IPServer.Split('\\')[0]; // IPServer = 10.235.55.124\PLH
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(ipServer);
                bool bIsConnected = false;

                if (pingReply.Status == IPStatus.Success)
                {
                    bIsConnected = true;
                }
                else
                {
                    result.Status = 0;
                    result.IPAddress = ipServer;
                    result.Message = $"IP không Ping được";
                }

                var messagePort = "";
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(ipServer, 1433, ref messagePort))
                {
                    bIsConnected = false;
                    result.Status = 0;
                    result.IPAddress = ipServer;
                    result.Message = $"Không telnet được Port 1433";
                }
                else
                {
                    bIsConnected = true;
                }

                if (bIsConnected)
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPServer, UserSA, Password, Database);
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            SqlCommand command = new SqlCommand(SqlScript, connection);
                            try
                            {
                                //var dataTableJson = "";
                                StringBuilder sb = new StringBuilder();
                                connection.Open();
                                var reader = command.ExecuteReader();

                                try
                                {
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    //dataTableJson = JsonConvert.SerializeObject(dataTable);
                                    if (countData > 0)
                                    {
                                        string[] columnNames = (from dc in dataTable.Columns.Cast<DataColumn>()
                                                                select dc.ColumnName).ToArray();
                                        var dataRow = (from row in dataTable.Rows.Cast<DataRow>()
                                                       select row.ItemArray).ToArray();
                                        sb.Append("");
                                        sb.Append("<table id=\"tableResult\" class=\"table table-bordered table-striped\" style=\"\">");
                                        sb.Append("<thead>");
                                        sb.Append("<tr>");
                                        foreach (var header in columnNames)
                                        {
                                            sb.Append("<th>" + header + "</th>");
                                        }
                                        sb.Append("</tr>");
                                        sb.Append("</thead>");
                                        sb.Append("</tbody>");
                                        for (int i = 0; i < dataRow.Length; i++)
                                        {
                                            var item = dataRow[i];

                                            sb.Append("<tr>");
                                            for (int j = 0; j < item.Length; j++)
                                            {
                                                sb.Append("<td>" + item[j] + "</td>");
                                            }
                                            sb.Append("</tr>");
                                        }
                                        sb.Append("</tbody>");
                                        sb.Append("</table>");
                                    }
                                    else
                                    {
                                        sb.Append("Không có data");
                                    }
                                }
                                finally
                                {
                                    reader.Close();
                                }

                                result.Status = 1;
                                result.IPAddress = ipServer;
                                result.Message = sb.ToString();
                                connection.Close();

                            }
                            catch (Exception ex)
                            {
                                result.Status = 0;
                                result.IPAddress = ipServer;
                                result.Message = ex.Message;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = 0;
                        result.IPAddress = ipServer;
                        result.Message = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Status = 0;
                result.IPAddress = IPServer;
                result.Message = ex.Message;
            }

            return Json(result);
        }

        [ParentAuthorize("QueryScript")]
        public ActionResult ExcuteScriptToServerExcel(string IPServer, string UserSA, string Password, string Database, string SqlScript)
        {
            ResultExecuteModel result = new ResultExecuteModel();
            bool bIsConnected = false;
            var messagePort = "";
            var ipServer = IPServer.Split('\\')[0]; // IPServer = 10.235.55.124\PLH

            if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(ipServer, 1433, ref messagePort))
            {
                bIsConnected = false;
                result.Status = 0;
                result.IPAddress = ipServer;
                result.Message = $"Không telnet được Port 1433";
            }
            else
            {
                bIsConnected = true;
            }

            if (bIsConnected)
            {
                string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPServer, UserSA, Password, Database);

                try
                {
                    using (SqlConnection connection = new SqlConnection(connectStr))
                    {
                        SqlCommand command = new SqlCommand(SqlScript, connection);
                        StringBuilder sb = new StringBuilder();
                        connection.Open();
                        var reader = command.ExecuteReader();

                        try
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                            using (var package = new ExcelPackage())
                            {
                                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                                worksheet.Cells["A1"].LoadFromDataTable(dataTable, true).AutoFitColumns();
                                string fileName = $"ExcelQueryServer_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                                using (var memoryStream = new MemoryStream())
                                {
                                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                                    Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                                    package.SaveAs(memoryStream);
                                    memoryStream.WriteTo(Response.OutputStream);
                                    Response.Flush();
                                    Response.End();
                                }
                            }
                        }
                        finally
                        {
                            reader.Close();
                            connection.Close();
                        }
                    }
                }
                catch
                {
                }
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        } 

        [DisplayName("Excute data bằng Script")]
        public ActionResult ExcuteScript()
        {
            return View();
        }

        [HttpPost]
        [ParentAuthorize("ExcuteScript")]
        public JsonResult LoadListIP()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 0;
            var skip = start != null ? Convert.ToInt32(start) : 0;
            var recordsTotal = 0;
            var storeNo = Request?.Form["StoreNo"];
            var ipAddress = Request?.Form["IpAddress"];
            var computerName = Request?.Form["ComputerName"];
            var data = _commonBLO.GetListIP(storeNo, ipAddress, computerName, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<IPAddressModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        [HttpPost]
        [ParentAuthorize("ExcuteScript")]
        public ActionResult ListTableSync()
        {
            try
            {
                var result = _commonBLO.ListTableSync();
                return Json(result);
            }
            catch (Exception ex)
            {
            }
            return Json(new List<SyncTableListModel>());
        }

        [HttpPost]
        [ParentAuthorize("ExcuteScript")]
        public async Task<ActionResult> SyncDataBySQL(string ListIPLocal, string GroupTable, int Action, string TypeCounter, long Counter)
        {
            var result = new List<Tuple<string, string, bool>>();
            try
            {
                var listIP = ListIPLocal.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                if (listIP.Count > 0)
                {
                    string ProcessID = DateTime.Now.ToString("ddMMyyyyHHmmss");

                    var tasks = new List<Task>();

                    var counter = TypeCounter == "ALL" ? 0 : TypeCounter == "LAST" ? 0 : Counter;

                    foreach (var info in listIP)
                    {
                        var split = info.Split(new char[] { '_' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        var ip = split[1];
                        var store = split[0];
                        //ip = "10.233.9.123";

                        var refMessage = "";
                        if (VCM.BLUEPOS.Helpers.Utils.CheckPort(ip, 1433, ref refMessage))
                        {
                            string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ip, POSUser, POSPass, "StorePLH");

                            //result.Add(WriteDataToPOSByStoreSQL(info, connectStr, store, GroupTable, Action, TypeCounter, counter));
                            tasks.Add(Task.Run(() => result.Add(WriteDataToPOSByStoreSQL(info, connectStr, store, GroupTable, Action, TypeCounter, counter, ip))));
                        }
                        else
                        {
                            result.Add(new Tuple<string, string, bool>(info, "Không telnet được Port 1433", false));
                        }

                    }
                    Task taskW = Task.WhenAll(tasks.ToArray());
                    await taskW;

                }
            }
            catch (Exception ex)
            {
                result.Add(new Tuple<string, string, bool>("Error", ex.Message, false));
            }
            return Json(result);
        }

        [ParentAuthorize("ExcuteScript")]
        public Tuple<string, string, bool> WriteDataToPOSByStoreSQL(string info, string connectStringStore, string storeNo, string tableGroup, int actionType, string typeCounter, long counter, string IPPOS)
        {
            bool Result = true;
            var message = "Success";
            try
            {
                List<DataTable> lstTable = _commonBLO.getDataByStore(storeNo, tableGroup, typeCounter, counter);
                if (actionType == 0 || actionType == 1)
                {
                    if (lstTable.Count > 0)
                    {
                        if (actionType == 0)
                        {
                            _commonBLO.DeleteAll(connectStringStore, lstTable);
                        }

                        if (actionType == 1)
                        {
                            _commonBLO.DeleteByPkey(connectStringStore, lstTable);
                        }

                        foreach (DataTable table in lstTable)
                        {
                            int RowsInserted = _commonBLO.InsertDataBySQL(table, connectStringStore, storeNo, IPPOS);
                            if (RowsInserted == 0)
                            {
                                Result = false;
                                message = "Không có data để insert tới POS";
                            }
                        }
                    }
                    else
                    {
                        Result = false;
                        message = "Không có data để insert tới POS";
                    }
                }
                else
                {
                    if (actionType == 2)
                    {
                        _commonBLO.DeleteAll(connectStringStore, lstTable);
                    }

                    if (actionType == 3)
                    {
                        _commonBLO.DeleteByPkey(connectStringStore, lstTable);
                    }
                }
            }
            catch (Exception ex)
            {
                Result = false;
                message = ex.Message;
            }
            return new Tuple<string, string, bool>(info, message, Result);
        }

        [HttpPost]
        [ParentAuthorize("ExcuteScript")]
        public async Task<ActionResult> _ExcuteScript(string ListIP, string SqlScript)
        {
            var data = new List<ResultExecuteModel>();
            try
            {
                var lstItemIP = ListIP.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                var lstTask = new List<Task>();
                foreach (var item in lstItemIP)
                {
                    if (item.ToString().Split('.').Length == 4)
                    {
                        lstTask.Add(Task.Run(() => data.Add(ExecuteIPV2(item, SqlScript))));
                    }
                }
                Task t = Task.WhenAll(lstTask.ToArray());
                await t;
            }
            catch
            {
            }
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = $"Success",
                ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SyncData/_ExcuteScript.cshtml", data),
                Data = data
            };
            return Json(result);
        }

        [ParentAuthorize("Index")]
        ResultExecuteModel ExecuteIP(string IPAddress, string SqlScript, int TypeExecute, string dbName)
        {
            ResultExecuteModel result = new ResultExecuteModel();
            try
            {
                bool bIsConnected = false;
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(IPAddress);

                if (pingReply.Status == IPStatus.Success)
                {
                    bIsConnected = true;
                }
                else
                {
                    result.Status = 0;
                    result.IPAddress = IPAddress;
                    result.Message = $"IP không Ping được";
                }
                var messagePort = "";
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 1433, ref messagePort))
                {
                    bIsConnected = false;
                    result.Status = 0;
                    result.IPAddress = IPAddress;
                    result.Message = $"Không telnet được Port 1433";
                }
                else
                {
                    bIsConnected = true;
                }

                if (bIsConnected)
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, dbName);

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlCommand command = new SqlCommand(SqlScript, connection);
                            try
                            {
                                StringBuilder sb = new StringBuilder();
                                var reader = command.ExecuteReader();
                                try
                                {
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    if (countData > 0)
                                    {
                                        string[] columnNames = (from dc in dataTable.Columns.Cast<DataColumn>()
                                                                select dc.ColumnName).ToArray();
                                        var dataRow = (from row in dataTable.Rows.Cast<DataRow>()
                                                       select row.ItemArray).ToArray();
                                        sb.Append("");
                                        sb.Append("<table class=\"table table-bordered table-striped table-responsive\">");
                                        sb.Append("<thead>");
                                        foreach (var header in columnNames)
                                        {
                                            sb.Append("<th>" + header + "</th>");
                                        }
                                        sb.Append("</thead>");
                                        sb.Append("</tbody>");
                                        for (int i = 0; i < dataRow.Length; i++)
                                        {
                                            var item = dataRow[i];

                                            sb.Append("<tr>");
                                            for (int j = 0; j < item.Length; j++)
                                            {
                                                sb.Append("<td>" + item[j] + "</td>");
                                            }
                                            sb.Append("</tr>");
                                        }
                                        sb.Append("</tbody>");
                                        sb.Append("</table>");
                                    }

                                }
                                finally
                                {
                                    reader.Close();
                                }
                                var message = "Success";
                                result.Status = 1;
                                result.IPAddress = IPAddress;
                                //result.Message = dataTableJson == "[]" ? message : dataTableJson;
                                result.Message = string.IsNullOrEmpty(sb.ToString()) ? message : sb.ToString();
                                //result.HtmlData = string.IsNullOrEmpty(sb.ToString()) ? message : sb.ToString();

                                connection.Close();
                            }
                            catch (Exception ex)
                            {
                                result.Status = 0;
                                result.IPAddress = IPAddress;
                                result.Message = ex.Message;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = 0;
                        result.IPAddress = IPAddress;
                        result.Message = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Status = 0;
                result.IPAddress = IPAddress;
                result.Message = ex.Message;
            }
            return result;
        }

        [ParentAuthorize("Index")]
        ResultExecuteModel ExecuteIPV2(string IPAddress, string SqlScript)
        {
            //var listResult = new List<ResultExecuteModel>();
            var result = new ResultExecuteModel();
            try
            {
                bool bIsConnected = false;
                //Ping ping = new Ping();
                //PingReply pingReply = ping.Send(IPAddress);

                //if (pingReply.Status == IPStatus.Success)
                //{
                //    bIsConnected = true;
                //}
                //else
                //{
                //    result.Status = 0;
                //    result.IPAddress = IPAddress;
                //    result.Message = $"IP không Ping được";
                //}
                var messagePort = "";
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 1433, ref messagePort))
                {
                    bIsConnected = false;
                    result.Status = 0;
                    result.IPAddress = IPAddress;
                    result.Message = $"Không telnet được Port 1433";
                }
                else
                {
                    bIsConnected = true;
                }

                if (bIsConnected)
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");

                    using (SqlConnection connection = new SqlConnection(connectStr))
                    {
                        connection.Open();
                        var resultText = "";
                        connection.InfoMessage += delegate (object sender, SqlInfoMessageEventArgs e)
                        {
                            resultText += e.Message + "</br>";
                        };
                        var sqlqueries = SqlScript.Split(new[] { "GO ", "GO" }, StringSplitOptions.RemoveEmptyEntries).Where(x => !string.IsNullOrEmpty(x));

                        var listGetData = new List<ResultDataTable>();
                        foreach (var query in sqlqueries)
                        {
                            result = new ResultExecuteModel();

                            SqlCommand command = new SqlCommand(query, connection);

                            var reader = command.ExecuteReader();

                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            var countData = dataTable.Rows.Count;

                            if (countData > 0)
                            {
                                string[] columnNames = (from dc in dataTable.Columns.Cast<DataColumn>()
                                                        select dc.ColumnName).ToArray();
                                var dataRow = (from row in dataTable.Rows.Cast<DataRow>()
                                               select row.ItemArray).ToArray();

                                var getData = new ResultDataTable
                                {
                                    TableName = dataTable.TableName,
                                    DataColumn = columnNames,
                                    DataRow = dataRow
                                };

                                listGetData.Add(getData);
                                result.GetDataTable = listGetData;
                                result.IPAddress = IPAddress;
                                result.Message = "OK";
                                result.Status = 1;
                            }
                            else
                            {
                                result.IPAddress = IPAddress;
                                result.Message = resultText;
                                result.Status = 1;
                            }
                            reader.Close();
                        }
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Status = 0;
                result.IPAddress = IPAddress;
                result.Message = ex.Message;
            }
            return result;
        }

        [DisplayName("Đồng bộ sale lên central")]
        public ActionResult SyncSalePosToCentral()
        {
            var listStoreNo = new List<StoreModel>();
            try
            {
                listStoreNo = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName).ToList();
            }
            catch
            {
            }
            return View(listStoreNo);
        }

        [HttpPost]
        [ParentAuthorize("SyncSalePosToCentral")]
        public ActionResult GetSaleMisCentral(string StoreNo, string POSID, string BussinessDate, string IPAddress)
        {
            try
            {
                if (!string.IsNullOrEmpty(StoreNo) && !string.IsNullOrEmpty(POSID) && !string.IsNullOrEmpty(BussinessDate) && !string.IsNullOrEmpty(IPAddress))
                {
                    DateTime ngayKD = DateTime.ParseExact(BussinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    var date = ngayKD.Date.ToString("MM/dd/yyyy");
                    var messagePort = "";
                    var result = new List<SalePosMisModel>();
                    if (VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 1433, ref messagePort))
                    {
                        string connectPOS = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");
                        var listOrderCentral = _syncDataBLO.ListOrderCentral(StoreNo, POSID, ngayKD).Select(a => a.OrderNo).ToList();
                        
                        using (SqlConnection connection = new SqlConnection(connectPOS))
                        {
                            string queryDB = string.Format(@"SELECT a.OrderNo ,a.CustomerName,a.CashierID,a.AmountInclVAT as Amount
                                                        FROM TransHeader(NOLOCK) a 
                                                        WHERE convert(date, a.OrderDate)='{0}' and a.StoreNo='{1}' and a.POSTerminalNo='{2}' ", date, StoreNo, POSID);

                            SqlCommand command = new SqlCommand(queryDB, connection);
                            try
                            {
                                connection.Open();
                                var reader = command.ExecuteReader();
                                try
                                {
                                    var dt = new DataTable();
                                    dt.Load(reader);
                                    var convertObj = JsonConvert.SerializeObject(dt);
                                    var listOrderPos = JsonConvert.DeserializeObject<List<SalePosMisModel>>(convertObj);
                                    result = listOrderPos.Where(d => !listOrderCentral.Any(a => a == d.OrderNo)).ToList();
                                }
                                finally
                                {
                                    reader.Close();
                                }
                                connection.Close();
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }                   
                    return Json(new {
                        Data = result,
                        Status = messagePort.ToLower()
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new List<SalePosMisModel>());
            }
            return Json(new List<SalePosMisModel>());
        }

        [HttpPost]
        [ParentAuthorize("SyncSalePosToCentral")]
        public async Task<ActionResult> SyncSale(string ListOrder, string StoreNo, string PosTerminal, string IPAddress, string BussinessDate)
        {
            var data = new List<Tuple<bool, string, string>>();
            DateTime ngayKD = DateTime.ParseExact(BussinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var listOrderPOS = ListOrder.Split(new char[] { ',' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            try
            {
                string connectPOS = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");
                var tasks = new List<Task>();
                var dtListTable = _stagingBLO.StagingListTableFromPOS(StoreNo);
                foreach (var orderNo in listOrderPOS)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        data.Add(UploadOrderNoToCentral(StoreNo, PosTerminal, orderNo, connectPOS, dtListTable));
                    }));
                }
                Task taskW = Task.WhenAll(tasks.ToArray());
                await taskW;
            }
            catch (Exception ex)
            {
                data.Add(new Tuple<bool, string, string>(false, "OrderNo", ex.Message));
            }

            var resultOK = data.Where(d => d.Item1 == true).Select(a => new { Status = a.Item1, OrderNo = a.Item2, Message = a.Item3 }).ToList();
            var resultFail = data.Where(d => d.Item1 == false).Select(a => new { Status = a.Item1, OrderNo = a.Item2, Message = a.Item3 }).ToList();

            if (resultOK.Count > 0)
            {
                _syncDataBLO.UpdateEOD(new POSEODModel
                {
                    StoreNo = StoreNo,
                    POSTerminalNo = PosTerminal,
                    BussinessDate = ngayKD,
                    TotalSale = resultOK.Count,
                    CreatedDate = DateTime.Now
                });
            }
            var result = new { OK = resultOK, Fail = resultFail };
            return Json(result);
        }

        [ParentAuthorize("SyncSalePosToCentral")]
        public Tuple<bool, string, string> UploadOrderNoToCentral(string storeNo, string posTerminal, string orderNo, string connectionPOS, List<StagingSyncTableFromPOSModel> dtListTable)
        {
            var Result = new Tuple<bool, string, string>(false, orderNo,"");
            try
            {                
                StringBuilder SQLSalePOS = new StringBuilder();
                foreach (var item in dtListTable)
                {
                    string TableName = item.TableName;
                    string DocumentNoName = item.DocumentNoName;
                    string SQLTablePOS = string.Format("SELECT '{3}' AS TableName,* FROM [{0}] (Nolock) WHERE {1}='{2}'", TableName, DocumentNoName, orderNo, TableName);

                    SQLSalePOS.AppendLine(SQLTablePOS);
                }

                bool IsHaveData = false;
                if (SQLSalePOS.Length > 0)
                {
                    using (SqlConnection connection = new SqlConnection(connectionPOS))
                    {
                        var result = new DataTable();
                        var SqlScript = SQLSalePOS.ToString();
                        SqlCommand command = new SqlCommand(SqlScript, connection);
                        using (var da = new SqlDataAdapter(command))
                        {
                            try
                            {
                                DataSet dsSalePOS = new DataSet();
                                da.Fill(dsSalePOS);
                                foreach (DataTable tblData in dsSalePOS.Tables)
                                {
                                    if (tblData.Rows.Count > 0)
                                    {
                                        // DataTable dtData = new DataTable();
                                        string TableNamePOS = tblData.Rows[0]["TableName"].ToString();
                                        tblData.TableName = TableNamePOS;

                                        //Insert vao Central
                                        DataTable tblDataFinal = VCM.BLUEPOS.Helpers.Utils.RemoveColumn(tblData, "TableName");
                                        string ResultInsert = _commonBLO.InsertBulk(tblDataFinal, TableNamePOS, storeNo, orderNo, posTerminal);

                                        //End Insert vao Central
                                        if (ResultInsert == "OK")
                                        {
                                            IsHaveData = true;
                                            Result = new Tuple<bool, string,string>(true, orderNo,"OK");
                                        }
                                        else
                                        {
                                            _commonBLO.InsertLogErrorWeb(new LogErrorWebModel
                                            {
                                                StoreNo = storeNo,
                                                PosTerminal = posTerminal,
                                                OrderNo = orderNo,
                                                Action = "UploadOrderNoToCentral",
                                                Description = $"Đồng bộ sale lên central:{TableNamePOS}",
                                                CreatedDate = DateTime.Now,
                                                CreatedUser = base.LoginUser.UserName,
                                                Msg = ResultInsert
                                            });
                                        }
                                    }
                                }
                                connection.Close();
                            }
                            catch (Exception ex)
                            {
                                Result = new Tuple<bool, string,string>(false,orderNo, ex.Message);
                                _commonBLO.InsertLogErrorWeb(new LogErrorWebModel
                                {
                                    StoreNo = storeNo,
                                    PosTerminal = posTerminal,
                                    OrderNo = orderNo,
                                    Action = "UploadOrderNoToCentral",
                                    Description = $"Đồng bộ sale lên central",
                                    CreatedDate = DateTime.Now,
                                    CreatedUser = base.LoginUser.UserName,
                                    Msg = JsonConvert.SerializeObject(ex)
                                });
                            }
                        }
                    }

                    if (IsHaveData == true)
                    {
                        _commonBLO.InsertTrans_Invoice(storeNo, orderNo);
                    }
                }
            }
            catch (Exception ex)
            {
                Result = new Tuple<bool, string,string>(false,orderNo, ex.Message);
                _commonBLO.InsertLogErrorWeb(new LogErrorWebModel
                {
                    StoreNo = storeNo,
                    PosTerminal = posTerminal,
                    OrderNo = orderNo,
                    Action = "UploadOrderNoToCentral",
                    Description = $"Đồng bộ sale lên central",
                    CreatedDate = DateTime.Now,
                    CreatedUser = base.LoginUser.UserName,
                    Msg = JsonConvert.SerializeObject(ex)
                });
            }
            return Result;
        }

        [DisplayName("Đồng bộ dữ liệu POS Web")]
        public ActionResult SyncDataByPOSWeb()
        {
            ViewBag.ListStore = _storeBLO.ListStorePartner();
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> GetSyncDataByPOSWEB(string StoreNo)
        {
            try
            {
                /*
                 * Tạo file trong thu muc Change (\\10.233.9.105\ftpbluepos\SyncDataToAppPOS)
                 */

                SyncDataToPos syncPos = new SyncDataToPos();
                if (StoreNo == "ALL") // Chọn tat ca
                {
                    syncPos.SyncDataToAllPos("POSWEB", "", 1, 10000, false, true, "", true); 
                }
                else // multi task
                {
                    var listStore = StoreNo.Split(new char[] { ',' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    if (listStore.Count > 0)
                    {
                        var tasks = new List<Task>();
                        foreach (var storeNo in listStore)
                        {
                            tasks.Add(Task.Run(() => syncPos.SyncDataToAllPos("POSWEB", "", 1, 1, false, true, storeNo, true)));
                        }
                        Task t = Task.WhenAll(tasks.ToArray());
                        await t;
                    }
                }
            }
            catch (Exception ex)
            {
                var resulterr = new ResultResponse
                {
                    Status = HttpStatusCode.ExpectationFailed,
                    Message = $"Có lỗi xảy ra"
                };
                return Json(resulterr);
            }
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK, // = 200
                Message = $"Success"
            };
            return Json(result);
        }


        // 05/07/2023

        [DisplayName("Đồng bộ File Sales POS To Central")]
        public ActionResult SyncFileSalesPosToCentral()
        {
            ViewBag.ListDocumentType = _commonBLO.GetComboxDocumentType();
            return View();
        }

        [HttpPost]
        [ParentAuthorize("SyncFileSalesPosToCentral")]
        public JsonResult LoadIPAddressList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 0;
            var skip = start != null ? Convert.ToInt32(start) : 0;
            var recordsTotal = 0;

            var storeNo = Request?.Form["StoreNo"];
            var ipAddress = Request?.Form["IpAddress"];
            var computerName = Request?.Form["ComputerName"];

            var data = _commonBLO.GetIPAddressList(storeNo, ipAddress, computerName, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<IPAddressModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        [ParentAuthorize("SyncFileSalesPosToCentral")]
        public async Task<ActionResult> _ExcuteScriptByFileSalesPOS(string ListIPAddress, string BussinessDate, string DocumentType, string ListOrderNo)
        {
            var data = new List<ResultExecuteModel>();
            try
            {
                var listIP =  ListIPAddress.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToList();                
                if (listIP.Count > 0)
                {
                    var lstTask = new List<Task>();
                    foreach (var info in listIP)
                    {
                        var split = info.Split(new char[] { '_' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        var ip = split[1];
                        var posTerminal = split[0];

                        if (ip.ToString().Split('.').Length == 4)
                        {
                            lstTask.Add(Task.Run(() => data.Add(ExecuteIPAddressByFileSalesPOS(ip, posTerminal, BussinessDate, DocumentType.TrimEnd(), ListOrderNo))));
                        }
                    }

                    Task t = Task.WhenAll(lstTask.ToArray());
                    await t;

                }               
            }
            catch (Exception ex)
            {
            }

            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,     
                Message = $"Đồng bộ file thành công",
                ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SyncData/_ExcuteScriptByFileSalesPOS.cshtml", data),
                Data = data
            };

            return Json(result);
        }


        [ParentAuthorize("SyncFileSalesPosToCentral")]
        ResultExecuteModel ExecuteIPAddressByFileSalesPOS(string ipAddress, string posTerminal, string bussinessDate, string documentType, string listOrderNo)
        {               
            var result = new ResultExecuteModel();
            try
            {
                bool bIsConnected = false;  
                var messagePort = "";

                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(ipAddress, 1433, ref messagePort))
                {
                    bIsConnected = false;
                    result.Status = 0;
                    result.IPAddress = ipAddress;
                    result.PosTerminal = posTerminal;
                    result.Message = $"Không telnet được Port 1433";
                }
                else
                {
                    bIsConnected = true;
                }

                if (bIsConnected)
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, POSUser, POSPass, "StorePLH");

                    using (SqlConnection connection = new SqlConnection(connectStr))
                    {
                        connection.Open();

                        var resultText = "";
                        connection.InfoMessage += delegate (object sender, SqlInfoMessageEventArgs e)
                        {
                            resultText += e.Message + "</br>";
                        };

                        SqlCommand cmd = connection.CreateCommand();

                        DateTime ngayKD = DateTime.ParseExact(bussinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                        var _bussinessDate = ngayKD.ToString("yyyy-MM-dd");

                        if (documentType == "SALE" || documentType == "VOID")
                        {
                            var listOrderPOS = listOrderNo.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();

                            foreach (var orderNo in listOrderPOS)
                            {
                                cmd.CommandText = "UPDATE [StorePLH].[dbo].[DocumentLog] SET IsRetrying = 0, IsCreatedFile = 0, IsFinish = 0, RetryTimes = 0 WHERE BussinessDate = '" + _bussinessDate + "' AND OrderNo IN ('" + orderNo + "') AND DocumentType = '" + documentType + "' ";
                                cmd.ExecuteNonQuery();
                            }
                            //cmd.CommandText = "UPDATE [StorePLH].[dbo].[DocumentLog] SET IsCreatedFile = 0, IsFinish = 0 WHERE BussinessDate = '" + _bussinessDate + "' AND OrderNo IN ('"+ listOrderPOS + "') AND DocumentType = '" + documentType + "' ";
                        }
                        else
                        {
                            cmd.CommandText = "UPDATE [StorePLH].[dbo].[DocumentLog] SET IsRetrying = 0, IsCreatedFile = 0, IsFinish = 0, RetryTimes = 0 WHERE BussinessDate = '" + _bussinessDate +"' AND DocumentType = '"+ documentType +"' ";
                            cmd.ExecuteNonQuery();
                        }

                        connection.Close();
                        result.IPAddress = ipAddress;
                        result.PosTerminal = posTerminal;
                        result.Message = "Thành công";
                        result.Status = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                result.IPAddress = ipAddress;
                result.PosTerminal = posTerminal;
                result.Message = ex.Message;
                result.Status = 0;
            }

            return result;
        }

        // 21/11/2024, tungnt8 : kiểm tra thiếu sales từ POS - Central
        [DisplayName("Kiểm tra đồng bộ sales POS - Central")]
        public ActionResult MissSalePosToCentralReport()
        {
            return View();
        }

        [HttpPost]
        [ParentAuthorize("MissSalePosToCentralReport")]
        public ActionResult GetMissSalesPOSToCentralReport(string StoreNo, string BussinessDate)
        {
            try
            {
                    DateTime ngayKD = DateTime.ParseExact(BussinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    var date = ngayKD.Date.ToString("MM/dd/yyyy");
                    var messagePort = "";
                    var result = new List<MissSalePosModel>();

                    var listData = _syncDataBLO.GetPOSTerminalByStore(StoreNo);
                    var listIPAddress = listData.Select(a => a.IpAddress).ToList();

                    foreach(var IPAddress in listIPAddress)
                    {
                        var storeNo = listData.Where(a =>a.IpAddress == IPAddress).FirstOrDefault()?.StoreNo;
                        var posID = listData.Where(a => a.IpAddress == IPAddress).FirstOrDefault()?.PosID;
                        var listOrderCentral = _syncDataBLO.ListOrderCentralV2(storeNo, posID, ngayKD).ToList();

                        var checkPort = VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 1433, ref messagePort);
                        if (checkPort)
                        {
                            string connectPOS = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");
                            using (SqlConnection connection = new SqlConnection(connectPOS))
                            {
                                string queryDB = string.Format(@"SELECT count(a.OrderNo) as TotalOrderNo,a.StoreNo,a.POSTerminalNo,Convert(date,a.OrderDate) as OrderDate
                                                            FROM TransHeader (NOLOCK) a 
                                                            WHERE convert(date, a.OrderDate)='{0}' and a.StoreNo='{1}' and a.POSTerminalNo='{2}' 
                                                            Group by a.StoreNo, a.POSTerminalNo, a.OrderDate", date, storeNo, posID);

                                SqlCommand command = new SqlCommand(queryDB, connection);
                                try
                                {
                                    connection.Open();
                                    var reader = command.ExecuteReader();
                                    try
                                    {
                                        var dt = new DataTable();
                                        dt.Load(reader);
                                        var convertObj = JsonConvert.SerializeObject(dt);
                                        var listOrderPos = JsonConvert.DeserializeObject<List<MissSalePosModel>>(convertObj);                                    
                                        result = listOrderPos.Where(d => !listOrderCentral.Any(a => a.TotalOrderCentral == d.TotalOrderPOS)).ToList();
                                    }
                                    finally
                                    {
                                        reader.Close();
                                    }
                                    connection.Close();
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                        }
                    }

                    return Json(new
                    {
                        Data = result,
                        Status = messagePort.ToLower()
                    });
                
            }
            catch (Exception ex)
            {
                return Json(new List<MissSalePosModel>());
            }

            //return Json(new List<SalePosMisModel>());

        }




    }
}