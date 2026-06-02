using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.CentralMD
{
    public class CpnVchBOMLineDto
    {
        public string ItemNo { get; set; }
        public int LineNo { get; set; }
        public string LineItemNo { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Barcode { get; set; }
        public string Pkey { get; set; }
    }
}
