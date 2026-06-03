using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class LogTransactionLoyaltyModel
    {
        public string CardNumber { get; set; }
        public string QRCode { get; set; }
        public string VirtualCard { get; set; }
        public string InvoiceNo { get; set; }
        public string ScanAndGoOrderNo { get; set; }
        public string ScanAndGoReferenceNumber { get; set; }
        public string AccessToken { get; set; }
        public string TerminalId { get; set; }
        public string StoreID { get; set; }
        public string ChannelCode { get; set; }
        public Nullable<System.DateTime> DateTime { get; set; }
        public string TransactionType { get; set; }
        public string TransactionName { get; set; }
        public Nullable<double> AmountOnOrderPOS { get; set; }
        public Nullable<double> BillAmount { get; set; }
        public Nullable<double> SpendPoints { get; set; }
        public Nullable<double> RefundAmount { get; set; }
        public string Operator { get; set; }
        public string OrderNo { get; set; }
        public string OrigOrderNo { get; set; }
        public string OrigInvoiceNo { get; set; }
        public Nullable<double> OrigBillAmount { get; set; }
        public string Source { get; set; }
        public Nullable<double> NettAmtOE { get; set; }
        public Nullable<double> GrossTransactionAmountOE { get; set; }
        public Nullable<double> AwardPointOE { get; set; }
        public Nullable<double> AwardAmountOE { get; set; }
        public Nullable<double> RedeemPointOE { get; set; }
        public Nullable<double> RedeemAmountOE { get; set; }
        public string PointPoolIDToRedeem { get; set; }
        public Nullable<bool> IsOffline { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string ResponseCode { get; set; }
    }
}
