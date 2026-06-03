using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
    public class POSMonitorInsertRequest
    {

        public string StoreNo { get; set; }
        public string IpAddress { get; set; }
        public string ComputerName { get; set; }
        public string PosTerminalID { get; set; }
        public string BluePosVersion { get; set; }
        public DateTime? BluePosVersionUpdate { get; set; }
        public int BluePosDatabaseStatus { get; set; }
        public int IsOpenBluePos { get; set; }
        public DateTime? DateTimePos { get; set; }
        public int IntervalJob { get; set; }
        public DateTime? LastTimeInsertAll { get; set; }
        public DateTime? LastTimeInsertChange { get; set; }
        public string JobVersion { get; set; }
        public string ScriptVersion { get; set; }
    }
}
