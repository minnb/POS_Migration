using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Home
{
    public class HealthCheckModel
    {
        public string IPServer { get; set; }
        public string LinkAPI { get; set; }
        public bool DbConnection { get; set; }
        public long Milliseconds { get; set; }
        public string TypeService { get; set; }
        public string Message { get; set; }
    }
    public class CheckServiceRequest
    {
        public string LinkF5 { get; set; }
        public string LinkMircro { get; set; }
        public string TypeService { get; set; }
    }
}
