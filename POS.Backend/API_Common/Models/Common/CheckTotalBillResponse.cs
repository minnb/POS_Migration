using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
    public class CheckTotalBillResponse
    {
        public bool Status { get; set; }
        public string Description { get; set; }
        public int TotalBillPOS { get; set; }
        public int TotalBillCentral { get; set; }
    }
}
