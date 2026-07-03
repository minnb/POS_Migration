using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class DetailInvoiceWinlifeModel
    {
        public List<ListOrderDetailModel> ListDetailInvoiceLine { get; set; }
        public InvoiceByViewHeaderModel InvoiceHeader { get; set; }
    }
}
