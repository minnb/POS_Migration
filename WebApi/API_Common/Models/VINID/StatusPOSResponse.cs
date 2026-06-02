using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class StatusPOSResponse
    {
        public string ReceiptNumber { get; set; }
        public string TransactionType { get; set; }
        public string StoreCode { get; set; }
        public string PosCode { get; set; }

        public string CashierCode { get; set; }
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }

        public string CustomerName { get; set; }
        public string VinidCardNumber { get; set; }
        public string VinidCSN { get; set; }
        public double? TotalBillAmount { get; set; }
        public string ReferenceNumber { get; set; }
        public int? ExtraEarnByItems { get; set; }
        public int? ExtraEarnByCampaign { get; set; }

        public int? VinIdEarn { get; set; }
        public bool? IsEarnSuccess { get; set; }
        public bool? OverQuota { get; set; }
        public List<StatusBillPOS> BillLines { get; set; }
    }
    public class StatusBillPOS
    {
        public float? RecordNo { get; set; }
        public string Barcode { get; set; }
        public string Article { get; set; }
        public string ArticleName { get; set; }
        public string UOM { get; set; }
        public float? Quantity { get; set; }
        public float? SalePrice { get; set; }
        public float? Amount { get; set; }
        public float? DiscountAmount { get; set; }
        public float? LineAmount { get; set; }
        public float? ExtraQuantityEarn { get; set; }
        public float? ExtraAmountEarn { get; set; }
        public float? VCMUnitPrice { get; set; }
    }
}
