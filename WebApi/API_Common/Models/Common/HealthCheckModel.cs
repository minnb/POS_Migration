using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
    public class HealthCheckModel
    {
        public string IPServer { get; set; }
        public bool DbConnection { get; set; }
        public string Version { get; set; }
    }
}
