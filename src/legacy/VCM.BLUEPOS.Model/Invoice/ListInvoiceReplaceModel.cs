using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class ListInvoiceReplaceModel
    {
        public string InvoicePattern { get; set; }
        public string SerialNo { get; set; }
        public string InvoiceNo { get; set; }
        public string Key { get; set; }
        public string Fkey { get; set; }
        public DateTime? ArisingDate { get; set; }
        public int? IsRun { get; set; }
        public string Status { get; set; }
    }
}
