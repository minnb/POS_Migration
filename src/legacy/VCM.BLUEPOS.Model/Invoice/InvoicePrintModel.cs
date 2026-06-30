using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class InvoicePrintModel
    {
        public string SiteName { get; set; }
        public string InvoicePattern { get; set; }
        public string InvoiceNo { get; set; }
        public string SerialNo { get; set; }
        public string FullName { get; set; }
        public string Company { get; set; }
        public string Address { get; set; }
        public string MST { get; set; }
        public string TeminalID { get; set; }
        public string StaffCode { get; set; }
        public string OrderNo { get; set; }
        public string SaltEncrypt { get; set; }
        public string OrderDate { get; set; }
        public string LinkSearchInvoice { get; set; }
    }

    public class PrintVATModel
    {
        public List<InvoicePrintModel> ListPrint { get; set; }
        public string LinkSearchInvoice { get; set; }
    }
}
