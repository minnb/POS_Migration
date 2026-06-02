using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.GotIt
{
    public class GotItVoucherUsedResponse
    {
        public bool success { get; set; }
        public int state { get; set; }
        public string voucher_code { get; set; }
        public string bill_number { get; set; }
        public string message { get; set; }
        public string message_vi { get; set; }
        public string usedDate { get; set; }
        public string expiryDate { get; set; }
        public string cancelDate { get; set; }
        public string transactionId { get; set; }
        public GotItProductCheckResponse products { get; set; }
    }

    public class GotItVoucherUsedListResponse
    {
        public bool success { get; set; }
        public int state { get; set; }
        public string message { get; set; }
        public string message_vi { get; set; }       
        public string cancelDate { get; set; }
        public string transactionId { get; set; }
        public List<GotItProductCheckResponse> products { get; set; }
        public List<string> code { get; set; }
        public UsedStore used_store { get; set; }
    }
    public class GotItVoucherUsedCheckResponse
    {
        public bool success { get; set; }
        public string state { get; set; }
        public string message { get; set; }
        public string message_vi { get; set; }
        public string error { get; set; }
    }

    public class UsedStore
    {
        public string name_vi { get; set; }
        public string name_en { get; set; }

    }
}
