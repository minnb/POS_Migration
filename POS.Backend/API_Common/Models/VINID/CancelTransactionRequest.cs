using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class CancelTransactionRequest
    {
        public string channel_code { get; set; }
        public string transaction_id { get; set; }
        public string signature { get; set; }
    }

    public class CancelTransactionResponse
    {
        public bool Status { get; set; }
    }
}
