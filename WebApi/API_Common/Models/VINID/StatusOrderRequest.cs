using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class StatusOrderRequest
    {
        public string ScanAndGoOrderNo { get; set; }
        public string OrderNo { get; set; }
        public int? Status { get; set; }
        public double? CollectedAmount { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string CardNumber { get; set; }
        public double? SpendPoints { get; set; }

    }
}
