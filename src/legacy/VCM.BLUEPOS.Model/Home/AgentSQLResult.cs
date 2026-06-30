using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Home
{
   public class AgentSQLResult
    {
        public string IPServer { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public List<JobSQLAgentModel> ListJobAgent { get; set; }
    }
}
