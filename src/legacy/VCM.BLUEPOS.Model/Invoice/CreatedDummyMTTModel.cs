using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class CreatedDummyMTTModel
    {
        public string MessageStr { get; set; }
    }
    public class ListInvoiceJsonModel
    {
        public List<ParamJsonInvoiceDummyMTT> ListInvoiceJSon { get; set; }

    }
    public class ParamJsonInvoiceDummyMTT
    {
        public int value { get; set; }
        public string BranchNo { get; set; }
        public string SerialNo { get; set; }
        public string ArisingDate { get; set; }
        public string BillTaxCode { get; set; }
    }
}
