using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class POS_RefunModelRequest
    {
        public string QRCode { get; set; }// QR-BARCODE VALUE WHEN SCAN     
        public string CardNumber { get; set; }//So thẻ
        public string MerchantId { get; set; }//Mã cửa hàng
        public string TerminalId { get; set; }//Mã POS
        public string InvoiceNo { get; set; }//So Invoice
        public string OrderNo { get; set; }//sodh POS
        public string OrigOrderNo { get; set; }//luôn luôn truyền là đơn hàng bán
        public int SpendPoints { get; set; }//Tich điểm

        // public string BillAmount { get; set; }//
        public decimal RefundAmount { get; set; }//Tổng trả
        public decimal OrderAmount { get; set; }//Tổng hóa đơn
        public bool IsOffline { get; set; }

    }
}
