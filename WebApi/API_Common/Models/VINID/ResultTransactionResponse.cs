using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class ResultTransactionResponse
    {
        public int Code { get; set; }
        public string TransactionID { get; set; }
        public string Message { get; set; }
        public string TransactionStatus { get; set; }
        public string TransactionWalletNumber { get; set; }

    }
}
