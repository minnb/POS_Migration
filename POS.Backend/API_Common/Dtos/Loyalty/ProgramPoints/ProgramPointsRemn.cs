using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.ProgramPoints
{
    public class ProgramPointsRemn
    {
        public string ClubCode { get; set; }
        public string MemberCard { get; set; }
        public decimal PreviousPoint { get; set; }
        public decimal CommitPoints { get; set; }
        public decimal AvailablePoints { get; set; }
    }
}
