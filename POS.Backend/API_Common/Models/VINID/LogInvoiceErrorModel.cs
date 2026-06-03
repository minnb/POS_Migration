using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class LogInvoiceErrorModel
    {
        public string RequestPOS { get; set; }
        public string RequestXML { get; set; }
        public SalesResponse ResponseSaleError { get; set; }
    }
}
