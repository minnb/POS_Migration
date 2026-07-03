using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.ServiceLog;
using VCM.BLUEPOS.Data;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.ServiceLog;


namespace VCM.BLUEPOS.Business.ServiceLog
{
    public interface IServiceLogBLO
    {
        ResultResponseModel WriteStackTraceLog(StackTraceLogModel req);
    }
    public class ServiceLogBLO : IServiceLogBLO
    {
        private readonly IServiceLogData _data;
        public ServiceLogBLO(IServiceLogData data)
        {
            _data = data;
        }
        public ResultResponseModel WriteStackTraceLog(StackTraceLogModel req)
        {
            return _data.WriteStackTraceLog(req);
        }
    }

}
