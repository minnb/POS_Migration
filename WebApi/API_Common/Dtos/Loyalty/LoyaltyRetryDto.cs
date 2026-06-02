using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class LoyaltyRetryDto
    {
        public string OrderNo { get; set; }
        public string PhoneNumber { get; set; }
        public string Status { get; set; }

    }
}
