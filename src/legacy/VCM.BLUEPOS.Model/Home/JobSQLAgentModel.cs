using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Home
{
   public class JobSQLAgentModel
    {
        public string jobName { get; set; }
        public string description { get; set; }
        public string Status { get; set; }
        public string RunDateAndTime { get; set; }
        public string RunStatus { get; set; }
        public string Run_duration { get; set; }
        public string message { get; set; }
        public string ScheduleName { get; set; }
        public string Frequency { get; set; }
        public string SubFrequency { get; set; }
        public string ScheduleTime { get; set; }
        public string NextRunDate { get; set; }
    }
}
