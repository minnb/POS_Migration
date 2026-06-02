using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.WinScore
{
    public class UpdateStatusWinscoreDto
    {
        public string PhoneNumber { get; set; }
        public int Status { get; set; }
        public string PosNo { get; set; }
    }
}
