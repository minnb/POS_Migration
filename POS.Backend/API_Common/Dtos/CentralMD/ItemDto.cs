using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.CentralMD
{
    public class ItemDto
    {
        public string ItemNo { get; set; }
        public string Uom { get; set; }
        public string DivisionCode { get; set; }
        public string TaxGroupCode { get; set; }
        public decimal VATPercent { get; set; }
        public bool IsVAT { get; set; }

    }
}
