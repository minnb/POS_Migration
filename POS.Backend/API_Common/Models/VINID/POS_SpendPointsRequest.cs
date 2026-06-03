using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class POS_SpendPointsRequest
    {
        public string TransactionRefNumber { get; set; }//transaction no of PnL
        public string Description { get; set; }
        public string CodeValue { get; set; }// QR-BARCODE VALUE WHEN SCAN       
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string InvoiceNo { get; set; }
        public string SpendPoints { get; set; }
    }
}
