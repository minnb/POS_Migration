using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.Store;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class ViewIssueInvoiceModel
    {
        public List<StoreModel> ListStore { get; set; }
        public InfoInvoiceModel InfoInvoice { get; set; }
    }
}
