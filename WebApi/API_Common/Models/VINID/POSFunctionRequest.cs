using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class POSFunctionRequest
    {
        public string numberCard { get; set; }
        public string posID { get; set; }
        public string storeNo { get; set; }
      
    }

    public class POSFunctionSaleRequest
    {
        public string QRCode { get; set; }
        public string CardNumber { get; set; }
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string InvoiceNo { get; set; }
        public int SpendPoints { get; set; }
        public decimal BillAmount { get; set; }
        public string OrderNo { get; set; }
        public bool IsOffline { get; set; }
        public decimal OrderAmount { get; set; }
        public string VirtualCard { get; set; }
        public List<POSAmountToEarn> ListAmountEarn { get; set; }


    }
    public class POSFunctionRefundRequest
    {
        public string QRCode { get; set; }
        public string CardNumber { get; set; }
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string InvoiceNo { get; set; }     
        public string OrderNo { get; set; }
        public string OrigOrderNo { get; set; }
        public int SpendPoints { get; set; }
        public decimal RefundAmount { get; set; }
        public bool IsOffline { get; set; }
        public decimal OrderAmount { get; set; }
        public bool IsScanAndGo { get; set; }
        public List<POSAmountToEarn> ListAmountEarn { get; set; }
    }
    public class POSAmountToEarn
    {
        public string LoyaltyMerchantId { get; set; }//Mã đối tác quản lý chương trình Loyalty PLH/VCM/DWN
        public double Amount { get; set; }//Giá trị tính điểm thưởng theo chương trình Loyalty tương ứng

    }

}
