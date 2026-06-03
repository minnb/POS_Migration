using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.WinLife
{
    public class WinLife_HistoryModel
    {
        public string csn { get; set; }
        public List<WinCodeHistoryModel> histories { get; set; }
    }
    public class WinCodeHistoryModel
    {
        public string winCode { get; set; }
        public string winCodeName { get; set; }
        public int quantityOnHands { get; set; }
        public List<WinCodeTransactionModel> transactions { get; set; }


    }
    public class WinCodeTransactionModel
    {
        public string billNo { get; set; }
        public string merchantId { get; set; }
        public string storeNo { get; set; }
        public string posNo { get; set; }
        public int quantity { get; set; }
        public string transactionDatetime { get; set; }

    }
}
