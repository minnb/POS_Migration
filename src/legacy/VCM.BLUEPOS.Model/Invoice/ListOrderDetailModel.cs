using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class ListOrderDetailModel
    {
        public Int64? Num { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }

        public double? Quantity { get; set; }
        public double? ProdPrice { get; set; }
        public double? NetAmount { get; set; }
        public double? VATRate { get; set; }
        public double? VATAmount { get; set; }
        public double? Amount { get; set; }

    }
}
