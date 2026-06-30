using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.MonitorPOS
{
    public class SignalStoreModel
    {
        public int ID { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalID { get; set; }
        public string IpAddress { get; set; }
        public string ComputerName { get; set; }
        public Nullable<System.DateTime> BusinessDate { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedDateFormat { get { return String.Format("{0:dd-MM-yyyy HH:mm}", CreatedDate); } }
        public string BusinessDateFormat { get { return String.Format("{0:dd-MM-yyyy}", BusinessDate); } }
        public Nullable<System.DateTime> LastTimeEvent { get; set; }
        public bool? StatusBeginDate { get; set; }
        public int? IntervalJob { get; set; }
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
