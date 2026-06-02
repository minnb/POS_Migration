using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
    public class InsuranceModel
    {
        public string Businessday { get; set; }
        public string ReceiptNo { get; set; }
        public string POSNo { get; set; }
        public decimal C_Total { get; set; }
    }
}
