
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Model.Home;
using VCM.BLUEPOS.Models;
//using VMC.BLUEPOS.Common;

namespace PLG.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IAuthenBLO _authen;
        private readonly ICommonBLO _commonBLO;

        private string agentUser = @"" + ConfigurationManager.AppSettings["AgentUser"];
        private string agentPass = @"" + ConfigurationManager.AppSettings["AgentPass"];

        private string taskUser = @"" + ConfigurationManager.AppSettings["TaskUser"];
        private string taskPass = @"" + ConfigurationManager.AppSettings["TaskPass"];

        public HomeController(IAuthenBLO authenBLO, ICommonBLO common)
        {
            _authen = authenBLO;
            _commonBLO = common;
        }

        [DisplayName("Trang chủ")]
        public ActionResult Index()
        {
            var model = new HomeViewModel();
            try
            {
                var userName = base.LoginUser.UserName;
                var role = _authen.GetRoleByUser(userName);

                var host = Dns.GetHostEntry(Dns.GetHostName());
                var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                var ipServer = addr.LastOrDefault()?.ToString();

                if (role.RoleCode == "R0000" || role.RoleCode == "R0001")
                {
                    var listServerWeb = _commonBLO.ListServerWeb(userName, ipServer).Where(d => d.TYPESERVICE != "JOB").ToList();
                    model.IPServerHost = ipServer;
                    model.Role = role.RoleCode;
                    model.ListServerWeb = listServerWeb;
                }
            }
            catch (Exception ex)
            {
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult _Notify()
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {

                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Success",
                    ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/Home/_Notify.cshtml", ""),

                };

            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CheckService(string LinkMicro, string TypeService)
        {
            var model = new HealthCheckModel();
            try
            {
                model = CheckServiceModel(LinkMicro, TypeService);
            }
            catch (Exception ex)
            {
                model = new HealthCheckModel
                {
                    DbConnection = false,
                    IPServer = ex.Message,
                    TypeService = TypeService,
                    Milliseconds = 0
                };
            }
            return Json(model);
        }

        [AllowAnonymous]
        HealthCheckModel CheckServiceModel(string LinkMicro, string TypeService)
        {
            var model = new HealthCheckModel();
            try
            {
                var st1 = new Stopwatch();
                st1.Start();
                long milliseconds = 0;
                var htmlDoc = new HtmlDocument();
                var request = (HttpWebRequest)WebRequest.Create(LinkMicro);
                request.Method = "GET";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        htmlDoc.OptionFixNestedTags = true;
                        htmlDoc.Load(stream, Encoding.UTF8);
                    }
                    milliseconds = st1.ElapsedMilliseconds;
                }

                st1.Stop();
                var doc = htmlDoc.DocumentNode.InnerText;
                if (TypeService == "APIPARTNER" || TypeService == "APIPAYMENT" || TypeService == "APIPAYMENTSV")
                {
                    model.Message = doc;
                }
                else
                {
                    var resultDoc = doc.Split(new char[] { '\r', '\n', '\t' }).Where(x => !string.IsNullOrEmpty(x)).ToList()[1];
                    model = JsonConvert.DeserializeObject<HealthCheckModel>(resultDoc);
                }
                model.Milliseconds = milliseconds;
                model.LinkAPI = LinkMicro;
                model.TypeService = TypeService;
            }
            catch (Exception ex)
            {
                model = new HealthCheckModel
                {
                    DbConnection = false,
                    IPServer = ex.Message,
                    Milliseconds = 0,
                    LinkAPI = LinkMicro,
                    TypeService = TypeService,
                    Message = ex.Message
                };
            }
            return model;
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CheckServiceAll(string JsonModel)
        {
            var model = new List<CheckServiceRequest>();
            var result = new List<HealthCheckModel>();
            if (!string.IsNullOrEmpty(JsonModel))
            {
                model = JsonConvert.DeserializeObject<List<CheckServiceRequest>>(JsonModel);
            }
            if (model != null && model.Count > 0)
            {
                //var listLinkMulti = ArrayLink.Split(new char[] { ',' }).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                //var listLinkMicro = listLinkMulti.Select(a => a.Split('_')[0]).Distinct().ToList();
                //var listLinkF5 = listLinkMulti.Select(a => a.Split('_')[1]).Distinct().ToArray();
                //listLinkMicro.AddRange(listLinkF5);

                var tasks = new List<Task>();
                try
                {
                    //var listLinkMicro = model.Where(d => d.LinkMircro != "").ToList();
                    var linkF5 = model.Where(d => d.TypeService != "WEB").GroupBy(d => new { LinkF5 = d.LinkF5, TypeService = d.TypeService }).Select(a => new CheckServiceRequest { LinkMircro = a.Key.LinkF5, TypeService = a.Key.TypeService }).Distinct().ToList();

                    //listLinkMicro.AddRange(linkF5);

                    if (linkF5 != null && linkF5.Count > 0)
                    {
                        foreach (var item in linkF5)
                        {
                            result.Add(CheckServiceModel(item.LinkMircro, item.TypeService));
                            //tasks.Add(Task.Run(() => result.Add(CheckServiceModel(item.LinkF5, item.TypeService))));
                        }
                    }
                }
                catch
                {
                }
                //Task taskW = Task.WhenAll(tasks.ToArray());
                //await taskW;
            }
            return Json(result);
        }

        [AllowAnonymous]
        public ActionResult _ViewServerScheduler()
        {
            var model = new List<ServerWebModel>();
            try
            {
                model = _commonBLO.ListServerWeb("").Where(d => d.TYPESERVICE == "JOB").ToList();
            }
            catch (Exception ex)
            {
            }
            return PartialView(model);
        }

        [AllowAnonymous]
        public ActionResult _ViewTaskScheduler(string ipserver, string arrayFolder)
        {
            var model = new List<TaskSchedulerJobModel>();
            try
            {
                //var listFoderSvr = new List<string>(new string[] { "\\McAfee", "\\", "\\Microsoft\\Windows", "\\Microsoft\\XblGameSave" });//\Microsoft\XblGameSave
                //var listFoderSvr = new List<string>(new string[] { "POS" });//\Microsoft\XblGameSave
                if (string.IsNullOrEmpty(arrayFolder))
                {
                    model.Add(new TaskSchedulerJobModel
                    {
                        IPServer = ipserver,
                        ErrorCode = 0,
                        ErrorMessage = "Không tìm thấy folder TASK trong server"
                    });
                }
                else
                {
                    using (Microsoft.Win32.TaskScheduler.TaskService ts = new Microsoft.Win32.TaskScheduler.TaskService($"{ipserver}", taskUser, "masan", taskPass))
                    {
                        //var allTask = ts.AllTasks.Where(d => !listFoderSvr.Any(a => a == (d.Folder.Parent == null ? "\\" : d.Folder.Parent.Path) || a == d.Folder.Path)).ToList();
                        var listFolder = JsonConvert.DeserializeObject<List<string>>(arrayFolder);
                        var allTask = ts.AllTasks.Where(d => listFolder.Any(a => a == d.Folder.Name)).ToList();
                        if (allTask.Count > 0)
                        {
                            foreach (var task in allTask)
                            {
                                model.Add(new TaskSchedulerJobModel
                                {
                                    Folder = task.Folder.Name,
                                    IPServer = ipserver,
                                    NameJob = task.Name,
                                    Status = task.State.ToString(),
                                    Trigger = task.Definition.Triggers.ToString(),
                                    NextRunTime = task.NextRunTime,
                                    LastRunTime = task.LastRunTime,
                                    LastRunResult = VCM.BLUEPOS.Helpers.Utils.GetDescriptionResultTask("" + task.LastTaskResult),
                                    ErrorCode = 1
                                });
                            }
                        }
                        else
                        {
                            model.Add(new TaskSchedulerJobModel
                            {
                                IPServer = ipserver,
                                ErrorCode = 0,
                                ErrorMessage = $"Chưa setup TASK SCHEDULER"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                model.Add(new TaskSchedulerJobModel
                {
                    IPServer = ipserver,
                    ErrorCode = 0,
                    ErrorMessage = ex.Message
                });
            }
            return PartialView(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CheckActionTaskScheduler(string IPServer, string TaskName)
        {
            var model = new TaskSchedulerJobModel();
            try
            {
                using (Microsoft.Win32.TaskScheduler.TaskService ts = new Microsoft.Win32.TaskScheduler.TaskService($"{IPServer}"))
                {
                    var getStatusTask = ts.FindTask(TaskName);
                    //Task task = ts.OpenTask(TaskName);
                    // getStatusTask.Stop();
                    var state = getStatusTask.State;
                    if (state == Microsoft.Win32.TaskScheduler.TaskState.Ready)
                    {
                        //getStatusTask.Stop();
                        getStatusTask.Enabled = false;
                        getStatusTask.RegisterChanges();
                    }

                    if (getStatusTask.State.ToString() == "Running")
                    {
                        model.ErrorCode = 0;
                        model.ErrorMessage = "Hiện tại TASK đang Running, vui lòng đợi...";
                    }
                    else if (getStatusTask.State.ToString() == "Ready")
                    {
                        var runTask = ts.FindTask(TaskName).Run();
                        if (runTask.State.ToString() == "Running")
                        {
                            var runTaskStatus = ts.FindTask(TaskName);
                            if (runTaskStatus.State.ToString() == "Ready")
                            {
                                model.ErrorCode = 1;
                                model.ErrorMessage = "OK";
                            }
                        }
                    }
                    else
                    {
                        model.ErrorCode = 0;
                        model.ErrorMessage = "Trạng thái Task không sẳn sàng để Run";
                    }
                }
            }
            catch (Exception ex)
            {
                model.ErrorCode = 0;
                model.ErrorMessage = ex.Message;
            }
            return Json(model);
        }

        [AllowAnonymous]
        public ActionResult _ViewServerSQL()
        {
            var model = new List<VCM.BLUEPOS.Model.Common.SQLServerModel>();
            try
            {
                model = _commonBLO.ListSQLServerExcute();
            }
            catch (Exception ex)
            {
            }
            return PartialView(model);
        }

        [AllowAnonymous]
        public ActionResult _ViewJobAgentSQL(string ipserver)
        {
            string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipserver, agentUser, agentPass, "msdb");
            var model = new AgentSQLResult();
            try
            {
                if (string.IsNullOrEmpty(ipserver))
                {
                    model.IPServer = ipserver;
                    model.ErrorCode = 0;
                    model.ErrorMessage = $"Không tìm thấy SQL server {ipserver} trong DB";
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(connectStr))
                    {
                        SqlCommand command = new SqlCommand("GetListJobSQL", connection);
                        try
                        {
                            connection.Open();
                            var reader = command.ExecuteReader();
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            var countData = dataTable.Rows.Count;
                            if (countData > 0)
                            {
                                var dataTableJson = JsonConvert.SerializeObject(dataTable);
                                var data = JsonConvert.DeserializeObject<List<JobSQLAgentModel>>(dataTableJson);
                                model.IPServer = ipserver;
                                model.ErrorCode = 1;
                                model.ListJobAgent = data;
                            }
                            else
                            {
                                model.IPServer = ipserver;
                                model.ErrorCode = 0;
                                model.ErrorMessage = $"Không tìm thấy JOB Agent SQL server {ipserver}";
                            }
                            connection.Close();
                        }
                        catch (Exception ex)
                        {
                            model.IPServer = ipserver;
                            model.ErrorCode = 0;
                            model.ErrorMessage = $"{ex.Message}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                model.IPServer = ipserver;
                model.ErrorCode = 0;
                model.ErrorMessage = $"{ex.Message}";
            }
            return PartialView(model);
        }

        [AllowAnonymous]
        public ActionResult _ViewCheckList()
        {
            var result = new ViewCheckListDailyModel();
            //var model = new ViewCheckListDailyModel();
            try
            {
                var getData = _commonBLO.CheckList();
                //model = listCheck.ListUserCheck.Where(d => d.UserCheck == base.LoginUser.UserName).ToList();
                //var listUser = listCheck.GroupBy(a => new { UserCheck = a.UserCheck, DateTimeCheck = a.DateTimeCheck.Value }).Select(a => new UserCheckModel {UserCheck=a.Key.UserCheck,DateTimeCheck=a.Key.DateTimeCheck,CountCheck = a.Count() }).ToList();
                result.CheckList = getData.CheckList.ToList();
                result.ListUserCheck = getData.ListUserCheck;
                result.ListUserChecked = getData.ListUserCheck.Where(d => d.UserCheck == base.LoginUser.UserName).ToList();
            }
            catch (Exception ex)
            {
            }
            return PartialView(result);
        }

        [HttpPost]
        public ActionResult CheckListUser(string CheckListID)
        {
            try
            {
                var createUserName = base.LoginUser.UserName;
                var listID = JsonConvert.DeserializeObject<List<CheckListRequest>>(CheckListID);
                if (listID == null || listID.Count == 0)
                {
                    return Json(new Tuple<bool, string>(false, "Vui lòng chọn công việc cần thực hiện"));
                }

                var model = new CheckListUserRequest
                {
                    UserCheck = createUserName,
                    DateTimeCheck = DateTime.Now,
                    CheckListRequest = listID
                };

                var result = _commonBLO.CheckListUser(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new Tuple<bool, string>(false, ex.Message));
            }
        }

        [AllowAnonymous]
        public ActionResult _ViewReportCheckList()
        {
            return PartialView();
        }
        [AllowAnonymous]
        public ActionResult _ViewResponseReportCheckList(string fromDate, string toDate)
        {
            var result = new List<ReportCheckListResponse>();
            try
            {
                var tuNgay = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var denNgay = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var data = _commonBLO.ReportCheckList(tuNgay, denNgay);
                result = data.Item3;
            }
            catch (Exception ex)
            {
            }
            return PartialView(result);
        }
    }
}