using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.ProgramPoints
{
    public class ProgramPointsSetupItem
    {
        public int ProgramId { get; set; }
        public string Type { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Uom { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal LimitedQty { get; set; }
        public bool Blocked { get; set; }
        
    }
}
