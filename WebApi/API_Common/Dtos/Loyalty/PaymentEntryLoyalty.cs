using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class PaymentEntryLoyalty
    {
        public int LineNo { get; set; }

        public string TenderType { get; set; }

        public decimal AmountTendered { get; set; }

        public string CardType { get; set; }
    }
}
