using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class TransactionResponse
    {
        public string txn_type { get; set; }
        public string gross_transaction_amount { get; set; }
        public string nett_amt { get; set; }
        public string refund_amount { get; set; }
    }
}
