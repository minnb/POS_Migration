using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using VCM.BLUEPOS.Data.Common;
using VCM.BLUEPOS.Data.MonitorPOS;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.MonitorPOS;


namespace VCM.BLUEPOS.Business.MonitorPOS
{
    public interface IMonitorPOSBLO
    {
        List<POSMonitorModel> LoadMonitorPOS(string storeNo, string IpAddress, string computerName, string StatusDB, string StatusPOS, string StatusOnl, string JobVersion, string POSVersion, string OperationPOS, string OperationJOB, out int totalRecord, int skip = 0, int take = 10);
        Tuple<int, string> DeleteMonitorIP(string IP);
        List<SignalStoreModel> ListSignalStore(string storeNo, string ipAddress, string computerName, string status, out int totalRecord, int skip, int take);



    }


    public class MonitorPOSBLO : IMonitorPOSBLO
    {
        private MonitorPOSData _data { get; set; }
        public MonitorPOSBLO()
        {
            _data = new MonitorPOSData();
        }

        public List<POSMonitorModel> LoadMonitorPOS(string storeNo, string IpAddress, string computerName, string StatusDB, string StatusPOS, string StatusOnl, string JobVersion, string POSVersion, string OperationPOS, string OperationJOB, out int totalRecord, int skip = 0, int take = 10)
        {
            return _data.LoadMonitorPOS(storeNo, IpAddress, computerName, StatusDB, StatusPOS, StatusOnl, JobVersion, POSVersion, OperationPOS, OperationJOB, out totalRecord, skip, take);
        }

        public Tuple<int, string> DeleteMonitorIP(string IP)
        {
            return _data.DeleteMonitorIP(IP);
        }

        public List<SignalStoreModel> ListSignalStore(string storeNo, string ipAddress, string computerName, string status, out int totalRecord, int skip, int take)
        {
            return _data.ListSignalStore(storeNo, ipAddress, computerName, status, out totalRecord, skip, take);
        }



















    }
}
