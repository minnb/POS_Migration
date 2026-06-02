using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.GotIt
{
   public class GotItVoucherUsedRequest
    {
        public string pin { get; set; }
        public string code { get; set; }
        public string bill_number { get; set; }
        public double total_bill { get; set; }
    }
    public class GotItVoucherUsedListRequest
    {
        public string pin { get; set; }
        public List<string> code { get; set; }
        public string bill_number { get; set; }
        public double total_bill { get; set; }
    }
}
