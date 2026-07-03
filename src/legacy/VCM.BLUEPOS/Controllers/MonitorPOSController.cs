using System;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Text;
using System.Data;
using System.Net.NetworkInformation;
using System.Data.OleDb;
using Spire.Xls;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Helpers;
//using VMC.BLUEPOS.Common;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Enums;
using System.Text.RegularExpressions;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.MonitorPOS;
using VCM.BLUEPOS.Business.MonitorPOS;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Common;

namespace PLG.Controllers
{
    public class MonitorPOSController : BaseController
    {
        private IMonitorPOSBLO _monitorPOSBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;
        public MonitorPOSController(IMonitorPOSBLO monitorPOSBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO)
        {
            _monitorPOSBLO = monitorPOSBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
        }

        // GET: MonitorPOS
        [DisplayName("Trạng thái máy POS")]
        public ActionResult MonitorPOS()
        {
            return View();
        }

        [HttpPost]
        public JsonResult MonitorPOSLoad()
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
            var slStatusDB = Request?.Form["BluePosDatabaseStatus"];
            var slStatusPOS = Request?.Form["IsOpenBluePos"];
            var slStatusOnl = Request?.Form["IsOnline"];

            var posVersion = Request?.Form["BluePosVersion"];
            var jobVersion = Request?.Form["JobVersion"];

            var opePOS = Request?.Form["OperationPOS"];
            var opeJOB = Request?.Form["OperationJOB"];
            var dateT = DateTime.Now;

            var data = _monitorPOSBLO.LoadMonitorPOS(storeNo, ipAddress, computerName, slStatusDB, slStatusPOS, slStatusOnl, jobVersion, posVersion, opePOS, opeJOB, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<POSMonitorModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        public ActionResult DeleteMonitorIP(string IP)
        {
            try
            {
                var data = _monitorPOSBLO.DeleteMonitorIP(IP);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(false);
            }
        }

        [HttpPost]
        public ActionResult PingIP(string IPAddress)
        {
            var pingModel = new PingResponse();
            try
            {
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(IPAddress.ToString());
                if (pingReply.Status == IPStatus.Success)
                {
                    pingModel.IPAddress = pingReply.Address.ToString();
                    pingModel.Message = pingReply.Status.ToString();
                    pingModel.Status = true;
                }
                else
                {
                    pingModel.IPAddress = pingReply.Address.ToString();
                    pingModel.Message = "Destination host unreachable";
                    pingModel.Status = false;
                }
            }
            catch (Exception ex)
            {
                pingModel.IPAddress = IPAddress;
                pingModel.Message = ex.Message;
                pingModel.Status = false;
            }
            return Json(pingModel);
        }

        [HttpPost]
        public ActionResult CheckService(string IPAddress, string ServiceName)
        {
            var result = new PingResponse();
            var refMessage = "";
            try
            {
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 135, ref refMessage))
                {
                    result.IPAddress = IPAddress;
                    result.Message = $"{IPAddress} không telnet được Port 135";
                    result.Status = false;
                }
                else
                {
                    RemoteService svr = new RemoteService();
                    var statusCheck = svr.GetStatusServiceByRemote(IPAddress, ServiceName);
                    result.IPAddress = IPAddress;
                    result.Message = $"Status service {ServiceName} : {statusCheck}";
                    result.Status = true;
                }
            }
            catch (Exception ex)
            {
                result.IPAddress = IPAddress;
                result.Message = ex.Message;
                result.Status = false;
            }
            return Json(result);
        }

        [HttpPost] 
        public ActionResult StartServiceByRemote(string IPAddress, string Action, string ServiceName)
        {
            var result = new PingResponse();
            var refMessage = "";
            try
            {
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 135, ref refMessage))
                {
                    result.IPAddress = IPAddress;
                    result.Message = $"{IPAddress} không telnet được Port 135";
                    result.Status = false;
                }
                else
                {
                    RemoteService svr = new RemoteService();
                    var statusCheck = svr.StartServiceByRemote(Action.ToUpper(), IPAddress, ServiceName);
                    result.IPAddress = IPAddress;
                    result.Message = $"{Action} service {ServiceName} success";
                    result.Status = true;
                }
            }
            catch (Exception ex)
            {
                result.IPAddress = IPAddress;
                result.Message = ex.Message;
                result.Status = false;
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult KillProcess(string IPAddress, string ServiceName)
        {
            var result = new PingResponse();
            RemoteService svr = new RemoteService();       
            try
            {
                var refMessage = "";
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 135, ref refMessage))
                {
                    result.IPAddress = IPAddress;
                    result.Status = false;
                    result.Message = $"{IPAddress} không telnet được Port 135";
                    result.ServiceName = ServiceName;
                }
                else
                {
                    var statusCheck = svr.KillProcess(IPAddress, ServiceName);
                    result.IPAddress = IPAddress;
                    result.ServiceName = ServiceName;
                    result.Message = $"{statusCheck}";
                    result.Status = true;
                }
            }
            catch (Exception ex)
            {
                result.IPAddress = IPAddress;
                result.Status = false;
                result.Message = ex.Message;
                result.ServiceName = ex.Message;
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult CheckPeformance(string IPAddress)
        {
            var result = new PingResponse();
            RemoteService svr = new RemoteService();
            try
            {
                var refMessage = "";
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 135, ref refMessage))
                {
                    result.IPAddress = IPAddress;
                    result.Status = false;
                    result.Message = $"{IPAddress} không telnet được Port 135";
                }
                else
                {
                    var statusCheck = svr.checkPerformance(IPAddress);
                    result.IPAddress = IPAddress;
                    result.CheckPeformance = statusCheck;
                    result.Status = true;
                }
            }
            catch (Exception ex)
            {
                result.IPAddress = IPAddress;
                result.Status = false;
                result.Message = $"{ex.Message}";
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult GetListProcess(string IPAddress, string ProcessName)
        {
            var result = new PingResponse();
            RemoteService svr = new RemoteService();
            try
            {
                var refMessage = "";
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 135, ref refMessage))
                {
                    result.IPAddress = IPAddress;
                    result.Status = false;
                    result.Message = $"{IPAddress} không telnet được Port 135";
                }
                else
                {
                    var listProcess = svr.GetProcessListByRemote(IPAddress, ProcessName);
                    var convertJson = JsonConvert.SerializeObject(listProcess);
                    var listResult = new List<ListProcessModel>();
                    var listData = JsonConvert.DeserializeObject<List<ListProcessModel>>(convertJson).OrderByDescending(d => d.OrderBy).ToList();
                    foreach (var item in listData)
                    {
                        if (item.Name == "BLUEPOS.exe" || item.Name == "ServiceAPI.exe" || item.Name == "sqlservr.exe")
                        {
                            listResult.Add(item);
                        }
                    }
                    var listFinal = listData.Except(listResult).ToList();
                    listResult.AddRange(listFinal);
                    result.ListProcess = listResult;
                    result.IPAddress = IPAddress;
                    result.Status = true;
                }
            }
            catch (Exception ex)
            {
                result.IPAddress = IPAddress;
                result.Status = false;
                result.Message = $"{ex.Message}";
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult RestartPC(string IPAddress)
        {
            var result = new PingResponse();
            RemoteService svr = new RemoteService();
            try
            {
                var refMessage = "";
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 135, ref refMessage))
                {
                    result.IPAddress = IPAddress;
                    result.Status = false;
                    result.Message = $"{IPAddress} không telnet được Port 135";
                }
                else
                {
                    var message = svr.RestartComputer(IPAddress);
                    result.IPAddress = IPAddress;
                    result.Message = message;
                    result.Status = true;
                }
            }
            catch (Exception ex)
            {
                result.IPAddress = IPAddress;
                result.Status = false;
                result.Message = $"{ex.Message}";
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult FilePOS(string IPAddress)
        {
            var result = new PingResponse();
            var refMessage = "";
            try
            {
                if (!VCM.BLUEPOS.Helpers.Utils.CheckPort(IPAddress, 135, ref refMessage))
                {
                    result.IPAddress = IPAddress;
                    result.Message = $"{IPAddress} không telnet được Port 135";
                    result.Status = false;
                }
                else
                {
                    RemoteService svr = new RemoteService();
                    svr.FileUpload(IPAddress, "Application");
                    result.IPAddress = IPAddress;
                    result.Message = $"Hãy đợi đấy";
                    result.Status = true;
                }
            }
            catch (Exception ex)
            {
                result.IPAddress = IPAddress;
                result.Message = ex.Message;
                result.Status = false;
            }
            return Json(result);
        }

        // GET: Danh sách máy POS bắt đầu ngày

        [DisplayName("Máy POS bắt đầu ngày")]
        public ActionResult SignalStoreList()
        {
            return View();
        }

        [HttpPost]
        public JsonResult LoadSignalStoreList()
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
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }

            var ipAddress = Request?.Form["IpAddress"];
            if (!string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = ipAddress.Trim();
            }

            var computerName = Request?.Form["ComputerName"];
            if (!string.IsNullOrEmpty(computerName))
            {
                computerName = computerName.Trim();
            }

            var status = Request?.Form["StatusBeginDate"];
            var data = _monitorPOSBLO.ListSignalStore(storeNo, ipAddress, computerName, status, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<SignalStoreModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }















    }
}