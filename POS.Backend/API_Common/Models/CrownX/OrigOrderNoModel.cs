using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class OrigOrderNoModel
    {
        public string InvoiceNo { get; set; }
        public double? BillAmount { get; set; }
        public double? SpendPoints { get; set; }
        public double? AwardPointCX { get; set; }
    }
}
