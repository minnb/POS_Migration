using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class ExtraHeaderModel
    {
        public string StoreNo { get; set; }
        public string POSCode { get; set; }
        public string ReceiptNumber { get; set; }
        public string OrderNo { get; set; } = "";
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string TransactionType { get; set; }
        public string CashierCode { get; set; }
        public Nullable<double> BusinessTime { get; set; }
        public Nullable<double> TransactionTime { get; set; }
        public string CustomerName { get; set; }
        public string VinidCardNumber { get; set; }
        public string VinidCSN { get; set; }
        public Nullable<double> TotalBillAmount { get; set; }
        public string ReferenceNumber { get; set; }
        public Nullable<bool> OverQuota { get; set; }
        public Nullable<double> ExtraEarnByItems { get; set; }

        public Nullable<double> ExtraEarnByCampaign { get; set; }

        public Nullable<double> ExtraRefundByItems { get; set; }

        public Nullable<double> ExtraRefundByCampaign { get; set; }
        public string EmployeeCode { get; set; }
        public string CompanyCode { get; set; }
        public string OriginalReceiptNumber { get; set; }
        public string OriginalPosNumber { get; set; }
        public string OriginalReferenceNumber { get; set; }
        public Nullable<double> EarnByDefault { get; set; }
        public Nullable<double> RefundAmount { get; set; }
        public Nullable<double> RefundPointDefault { get; set; }
        public string ActionAPI { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedUser { get; set; }
    }

    public class LogExtralSaleModel
    {
        public ExtraHeaderModel saleHeader { get; set; }
        public List<ExtratItemBillModel> saleLine { get; set; }

    }
}
