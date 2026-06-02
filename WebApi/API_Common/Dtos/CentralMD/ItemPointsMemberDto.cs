using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.CentralMD
{
    public class ItemPointsMemberDto
    {
        public string PointsCode { get; set; }
        public string ItemNo { get; set; }
        public string Barcode { get; set; }
        public string ItemName { get; set; }
        public string Uom { get; set; }
        public int ShelfLife { get; set; }
        public int DaysOfUsed { get; set; }
        public bool Blocked { get; set; }
    }
}
