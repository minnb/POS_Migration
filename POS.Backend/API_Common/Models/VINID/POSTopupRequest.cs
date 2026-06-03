using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class POSTopupRequest
    {
        public string BarcodeOrder { get; set; }//Don hang barcode 13 ky tu _888
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string CardNumber { get; set; }
        public string TopupNo { get; set; }
        public List<TopupType> TopupData { get; set; }

    }
    public class TopupType
    {
        public Int64? Amount { get; set; }//số tiền / số điểm cần refund
        public string Type { get; set; }//1. POINT - điểm 2. WALLET - ví 3. BANK_CARD - napas
    }


    public class TopupVINIDRequest
    {
        public string topup_no { get; set; }
        public List<TopupTypeVINIDRequest> topup_data { get; set; }
    }
    public class TopupTypeVINIDRequest
    {
        public Int64? amount { get; set; }//số tiền / số điểm cần refund
        public string type { get; set; }//1. POINT - điểm 2. WALLET - ví 3. BANK_CARD - napas
    }
}
