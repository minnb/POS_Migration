using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class InitTransactionResponse
    {
        public TMeta meta { get; set; }
        public TData data { get; set; }
    }
    public class TMeta
    {
        public int code { get; set; }
        public string request_id { get; set; }
        public string message { get; set; }
        public List<Error> list_error { get; set; }
    }
    public class TData
    {
        public string transaction_id { get; set; }
        public string transaction_ref_number { get; set; }
        public string transactionStatus { get; set; }
        public string transaction_wallet_number { get; set; }
    }
    public class Error
    {
        public int code { get; set; }
        public string message { get; set; }
    }
}
