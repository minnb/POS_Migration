using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.SAP
{
    public class LogSAPModel
    {
        public long ID { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string VoucherNumber { get; set; }
        public string RequestPOS { get; set; }
        public string RequestSAP { get; set; }
        public string ResponseSAP { get; set; }
        public string ActionType { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}
