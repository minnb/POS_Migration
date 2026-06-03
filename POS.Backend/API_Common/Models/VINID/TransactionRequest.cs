using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class TransactionRequest
    {
      
        public string code_value { get; set; }// QR-BARCODE VALUE WHEN SCAN
        public string channel_code { get; set; }
        public string merchant_id { get; set; }
        public string terminal_id { get; set; }
        public string description { get; set; }    
        public string transaction_ref_number { get; set; }
        public string order_created_at { get; set; }
        public string bill_amount { get; set; }
        public string amount { get; set; }
        public string invoice_no { get; set; }        
        public string currency { get; set; }//VID, VND
        public string signature { get; set; }
    }
}
