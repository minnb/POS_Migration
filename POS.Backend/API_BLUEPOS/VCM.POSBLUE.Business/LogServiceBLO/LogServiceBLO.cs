using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.POSBLUE.Data.LogServiceAPI;
using VCM.POSBLUE.Model.LogService;

namespace VCM.POSBLUE.Business.LogServiceBLO
{
    public interface ILogServiceBLO
    {
        void LogServiceInsert(LogServiceModel model);
    }
    public class LogServiceBLO : ILogServiceBLO
    {
        private LogServiceData data { get; set; }
        public LogServiceBLO()
        {
            data = new LogServiceData();
        }
        public void LogServiceInsert(LogServiceModel model)
        {
            data.LogServiceInsert(model);
        }
    }
}
