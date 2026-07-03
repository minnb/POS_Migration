using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Entity;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Common.Helpers;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.MonitorPOS;


namespace VCM.BLUEPOS.Data.MonitorPOS
{
    public interface IMonitorPOSData
    {
        List<POSMonitorModel> LoadMonitorPOS(string storeNo, string IpAddress, string computerName, string StatusDB, string StatusPOS, string StatusOnl, string JobVersion, string POSVersion, string OperationPOS, string OperationJOB, out int totalRecord, int skip = 0, int take = 10);
        Tuple<int, string> DeleteMonitorIP(string IP);
        List<SignalStoreModel> ListSignalStore(string storeNo, string ipAddress, string computerName, string status, out int totalRecord, int skip, int take);



    }

    public class MonitorPOSData : IMonitorPOSData
    {
        public List<POSMonitorModel> LoadMonitorPOS(string storeNo, string IpAddress, string computerName, string StatusDB, string StatusPOS, string StatusOnl, string JobVersion, string POSVersion, string OperationPOS, string OperationJOB, out int totalRecord, int skip = 0, int take = 10)
        {
            try
            {
                var result = new List<POSMonitorModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.POSMonitors.AsQueryable();

                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        data = data.Where(d => d.StoreNo == storeNo);
                    }

                    if (!string.IsNullOrEmpty(IpAddress))
                    {
                        data = data.Where(d => d.IpAddress.Contains(IpAddress));
                    }

                    if (!string.IsNullOrEmpty(computerName))
                    {
                        data = data.Where(d => d.ComputerName.Contains(computerName));
                    }

                    if (!string.IsNullOrEmpty(StatusDB))
                    {
                        var sDB = StatusDB == "1" ? 1 : 0;
                        data = data.Where(d => d.BluePosDatabaseStatus == sDB);
                    }

                    if (!string.IsNullOrEmpty(StatusPOS))
                    {
                        var sPos = StatusPOS == "1" ? true : false;
                        data = data.Where(d => d.IsOpenBluePos == sPos);
                    }

                    var dataList = data.ToList();
                    if (!string.IsNullOrEmpty(POSVersion))
                    {
                        if (OperationPOS == "=")
                        {
                            dataList = dataList.Where(d => d.BluePosVersion == POSVersion).ToList();
                        }
                        else if (OperationPOS == "#")
                        {
                            dataList = dataList.Where(d => d.BluePosVersion != POSVersion).ToList();
                        }
                        else if (OperationPOS == "&gt;") //>
                        {
                            var posVer = double.Parse(POSVersion);
                            dataList = dataList.Where(d => Convert.ToDouble(d.BluePosVersion) > posVer).ToList();
                        }
                        else if (OperationPOS == "&lt;") //<
                        {
                            var posVer = Convert.ToDouble(POSVersion);

                            dataList = dataList.Where(d => Convert.ToDouble(d.BluePosVersion) < posVer).ToList();
                        }
                    }

                    if (!string.IsNullOrEmpty(JobVersion))
                    {
                        if (OperationJOB == "=")
                        {
                            dataList = dataList.Where(d => d.JobVersion == JobVersion).ToList();
                        }
                        else if (OperationJOB == "#")
                        {
                            dataList = dataList.Where(d => d.JobVersion != JobVersion).ToList();
                        }
                        else if (OperationJOB == "&gt;") //>
                        {
                            var jobVer = double.Parse(JobVersion);
                            dataList = dataList.Where(d => Convert.ToDouble(d.JobVersion) > jobVer).ToList();
                        }
                        else if (OperationJOB == "&lt;") //<
                        {
                            var jobVer = Convert.ToDouble(JobVersion);

                            dataList = dataList.Where(d => Convert.ToDouble(d.JobVersion) < jobVer).ToList();
                        }
                    }

                    var temp = dataList.ToList().Select(a => new POSMonitorModel
                    {
                        ID = a.ID,
                        StoreNo = a.StoreNo,
                        IpAddress = a.IpAddress,
                        PosTerminalID = a.PosTerminalID,
                        ComputerName = a.ComputerName,
                        BluePosVersion = a.BluePosVersion,
                        JobVersion = a.JobVersion,
                        ScriptVersion = a.ScriptVersion,
                        BluePosVersionUpdate = a.BluePosVersionUpdate,
                        BluePosDatabaseStatus = a.BluePosDatabaseStatus,
                        IsOpenBluePos = a.IsOpenBluePos,
                        IsMonitor = a.IsMonitor,
                        DateTimePos = a.DateTimePos,
                        FirstTimeEvent = a.FirstTimeEvent,
                        LastTimeEvent = a.LastTimeEvent,
                        IntervalJob = a.IntervalJob,
                        LastTimeInsertAll = a.LastTimeInsertAll,
                        LastTimeInsertChange = a.LastTimeInsertChange,
                        SubtractTime = (int)DateTime.Now.Subtract(a.LastTimeEvent.Value).TotalMinutes,
                        JobOnline = (int)DateTime.Now.Subtract(a.LastTimeEvent.Value).TotalMinutes > a.IntervalJob + 10 ? false : true
                    }).ToList();

                    if (!string.IsNullOrEmpty(StatusOnl))
                    {
                        var sONL = StatusOnl == "1" ? true : false;
                        temp = temp.Where(d => d.JobOnline == sONL).ToList();
                    }
                    totalRecord = temp.Count();
                    result = temp.OrderByDescending(d => d.StoreNo).Skip(skip).Take(take).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return (default(List<POSMonitorModel>));
            }
        }

        public Tuple<int, string> DeleteMonitorIP(string IP)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkIP = db.POSMonitors.FirstOrDefault(d => d.IpAddress == IP);
                    if (checkIP != null)
                    {
                        db.POSMonitors.Remove(checkIP);
                        db.SaveChanges();
                        return new Tuple<int, string>(1, "Xóa thành công");
                    }
                    else
                    {
                        return new Tuple<int, string>(0, "Xóa thất bại");
                    }                       
                }
            }
            catch (Exception ex)
            {
                return new Tuple<int, string>(0, ex.Message);
            }
        }

        public List<SignalStoreModel> ListSignalStore(string storeNo, string ipAddress, string computerName, string status, out int totalRecord, int skip, int take)
        {
            try
            {
                var result = new List<SignalStoreModel>();
                var dateTime = DateTime.Now.Date;
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var signalList = db.SignalStores.Where(d => DbFunctions.TruncateTime(d.CreatedDate) == dateTime);
                    var listData = from a in db.POSMonitors
                               join b in signalList on a.PosTerminalID equals b.POSTerminalID into T
                               from c in T.DefaultIfEmpty()
                               select new SignalStoreModel
                               {
                                   IpAddress = a.IpAddress,
                                   ComputerName = a.ComputerName,
                                   StoreNo = a.StoreNo,
                                   POSTerminalID = a.PosTerminalID,
                                   BusinessDate = c.BusinessDate,
                                   CreatedDate = c.CreatedDate,
                                   StatusBeginDate = c.BusinessDate == null ? false : true,
                                   LastTimeEvent = a.LastTimeEvent,
                                   IntervalJob = a.IntervalJob
                               };

                    var re = listData.ToList();
                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        listData = listData.Where(d => d.StoreNo.Contains(storeNo));
                    }

                    if (!string.IsNullOrEmpty(ipAddress))
                    {
                        listData = listData.Where(d => d.IpAddress.Contains(ipAddress));
                    }

                    if (!string.IsNullOrEmpty(computerName))
                    {
                        listData = listData.Where(d => d.ComputerName.Contains(computerName));
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        listData = listData.Where(d => d.StatusBeginDate == (status == "1" ? true : false));
                    }

                    totalRecord = listData.Count();
                    result = listData.OrderByDescending(d => d.StoreNo).Skip(skip).Take(take).ToList();
                    return result;
                }
            }
            catch (Exception Ex)
            {
                totalRecord = 0;
                return (default(List<SignalStoreModel>));
            }
        }















    }
}
