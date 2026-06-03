using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.CentralMD
{
    public class LoyaltyRateDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Code { get; set; }
        public decimal Rate { get; set; }
        public bool Blocked { get; set; }
        public string Pkey { get; set; }
        public string CardType { get; set; }
    }
}
