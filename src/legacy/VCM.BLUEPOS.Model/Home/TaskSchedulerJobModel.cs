using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Home
{
    public class TaskSchedulerJobModel
    {
        public string Folder { get; set; }
        public string IPServer { get; set; }
        public string NameJob { get; set; }
        public string Trigger { get; set; }
        public string Status { get; set; }
        public DateTime NextRunTime { get; set; }
        public DateTime LastRunTime { get; set; }
        public string LastRunResult { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
