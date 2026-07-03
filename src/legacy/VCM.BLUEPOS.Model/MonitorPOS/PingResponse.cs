using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.MonitorPOS
{
    public class PingResponse
    {
        public string IPAddress { get; set; }
        public string ServiceName { get; set; }
        public bool Status { get; set; }
        public string Message { get; set; }
        public List<string> CheckPeformance { get; set; }
        public List<ListProcessModel> ListProcess { get; set; }
    }



}
