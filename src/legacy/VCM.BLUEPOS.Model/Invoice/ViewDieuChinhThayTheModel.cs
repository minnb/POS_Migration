using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class ViewDieuChinhThayTheModel
    {
        public List<ListInvoiceReplaceModel> ListInvoiceReplace { get; set; }
        public PrintAgainModel InfoInvoiceByKey { get; set; }
        public string LoaiDieuChinh { get; set; }
    }
}
