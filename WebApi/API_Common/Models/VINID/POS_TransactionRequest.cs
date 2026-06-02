using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class POS_TransactionRequest
    {
        public string QRCode { get; set; }// QR-BARCODE VALUE WHEN SCAN
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
       // public string Description { get; set; }
       // public string TransactionRefNumber { get; set; }
        public decimal BillAmount { get; set; }
        public decimal Amount { get; set; }
        public string InvoiceNo { get; set; }
       // public string Currency { get; set; }//VID, VND
       
    }
}
