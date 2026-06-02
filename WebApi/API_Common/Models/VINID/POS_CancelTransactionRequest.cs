using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class POS_CancelTransactionRequest
    {
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string OrderNo { get; set; }
        public string TransactionId { get; set; }
    }
}
