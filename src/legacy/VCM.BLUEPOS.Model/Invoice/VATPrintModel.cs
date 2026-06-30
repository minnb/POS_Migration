using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class VATPrintModel
    {
        public InvoicePrintModel dataPrint { get; set; }
        public List<InvoiceInfoVATModel> InvoiceInfoPrint { get; set; }
    }
}
