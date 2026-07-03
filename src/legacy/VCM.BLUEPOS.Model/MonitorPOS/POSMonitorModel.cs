using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.MonitorPOS
{
    public class POSMonitorModel
    {
        public int ID { get; set; }
        public int SubtractTime { get; set; }
        public string StoreNo { get; set; }
        public string IpAddress { get; set; }
        public string ComputerName { get; set; }
        public string PosTerminalID { get; set; }
        public string BluePosVersion { get; set; }
        public string JobVersion { get; set; }
        public string OperationPOS { get; set; }
        public string OperationJOB { get; set; }
        public string ScriptVersion { get; set; }
        public Nullable<System.DateTime> BluePosVersionUpdate { get; set; }
        public Nullable<short> BluePosDatabaseStatus { get; set; }
        public Nullable<bool> IsOpenBluePos { get; set; }
        public Nullable<System.DateTime> DateTimePos { get; set; }
        public Nullable<System.DateTime> FirstTimeEvent { get; set; }
        public Nullable<System.DateTime> LastTimeEvent { get; set; }
        public Nullable<bool> IsMonitor { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public int? IntervalJob { get; set; }
        public Nullable<System.DateTime> LastTimeInsertAll { get; set; }
        public Nullable<System.DateTime> LastTimeInsertChange { get; set; }
        public string FormatDateTimePos { get { return string.Format("{0:dd/MM/yyyy HH:mm}", DateTimePos); } }
        public string FormatFirstTimeEvent { get { return string.Format("{0:dd/MM/yyyy HH:mm}", FirstTimeEvent); } }
        public string FormatLastTimeEvent { get { return string.Format("{0:dd/MM/yyyy HH:mm}", LastTimeEvent); } }
        public string FormatLastTimeInsertAll { get { return (string.Format("{0:dd/MM/yyyy}", LastTimeInsertAll) == "31/12/1899" || string.Format("{0:dd/MM/yyyy}", LastTimeInsertAll) == "01/01/1900") ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", LastTimeInsertAll); } }
        public string FormatLastTimeInsertChange { get { return (string.Format("{0:dd/MM/yyyy}", LastTimeInsertChange) == "31/12/1899" || string.Format("{0:dd/MM/yyyy}", LastTimeInsertChange) == "01/01/1900") ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", LastTimeInsertChange); } }
        public bool? JobOnline { get; set; }
        public bool IsOnline
        {
            get
            {
                TimeSpan ts = DateTime.Now.Subtract(LastTimeEvent.Value);
                if (ts.TotalMinutes > IntervalJob)
                    return false;
                return true;
            }
        }
    }











}
