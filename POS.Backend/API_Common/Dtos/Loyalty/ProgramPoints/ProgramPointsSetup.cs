using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.ProgramPoints
{
    public class ProgramPointsSetup
    {
        public int ProgramId { get; set; }
        public string CumulativeType { get; set; }
        public string ClubCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Description { get; set; }
        public bool Blocked { get; set; }
        public DateTime CrtDate { get; set; }
        public DateTime ChgDate { get; set; }
    }
}
