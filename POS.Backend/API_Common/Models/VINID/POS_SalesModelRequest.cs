using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class POS_SalesModelRequest
    {
        public string QRCode { get; set; }// QR-BARCODE VALUE WHEN SCAN
        public string CardNumber { get; set; }//So thẻ       
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string InvoiceNo { get; set; }
        public string SpendPoints { get; set; }
        public string BillAmount { get; set; }
        public string OrderNo { get; set; }
        public string IsOffline { get; set; }
        public string OrderAmount { get; set; }//Tổng hóa đơn
    }
}
