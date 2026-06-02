using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.ProgramPoints
{
    public class ProgramPointsSetupStore
    {
        public int ProgramId { get; set; }
        public string StoreNo { get; set; }
        public int LimitedQty { get; set; }
        public bool Blocked { get; set; }
    }
}
