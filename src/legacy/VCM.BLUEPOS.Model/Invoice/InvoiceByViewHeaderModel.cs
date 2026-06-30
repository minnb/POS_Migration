using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class InvoiceByViewHeaderModel
    {
        public double? VinIDRedeem { get; set; }
        public int? vatNon { get; set; }
        public double? NetNon { get; set; }
        public int? Vat0 { get; set; }
        public double? Net0 { get; set; }
        public double? Vat5 { get; set; }
        public double? Net5 { get; set; }

        public double? Vat8 { get; set; }
        public double? Net8 { get; set; }

        public double? Vat10 { get; set; }
        public double? Net10 { get; set; }
        public double? TotalNon { get; set; }
        public double? Total0 { get; set; }
        public double? Total5 { get; set; }
        public double? Total8 { get; set; }
        public double? Total10 { get; set; }
    }
}
