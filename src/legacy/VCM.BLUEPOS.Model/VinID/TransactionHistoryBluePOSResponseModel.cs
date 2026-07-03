using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.VinID
{
    public class TransactionHistoryBluePOSResponseModel
    {
        public int ID { get; set; }
        public string StoreID { get; set; }
        public string TerminalId { get; set; }
        public string DateTimeStr { get; set; }
        public string CardNumber { get; set; }
        public string QRCode { get; set; }
        public string InvoiceNo { get; set; }
        public string OrderNo { get; set; }
        public string OrigOrderNo { get; set; }
        public string OrigInvoiceNo { get; set; }
        public string TransactionName { get; set; }
        public double AmountOnOrderPOS { get; set; }
        public double GrossTransactionAmountOE { get; set; }
        public double RefundAmount { get; set; }
        public double SpendPoints { get; set; }
        public double AwardPointOE { get; set; }
        public double AwardAmountOE { get; set; }
        public double RedeemAmountOE { get; set; }
        public int Total { get; set; }

    }

    public class TransactionLoyaltyModel
    {
        public long ID { get; set; }
        public string CardNumber { get; set; }
        public string QRCode { get; set; }
        public string InvoiceNo { get; set; }
        public string AccessToken { get; set; }
        public string TerminalId { get; set; }
        public string StoreID { get; set; }
        public string ChannelCode { get; set; }
        public string DateTime { get; set; }
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

        public string FormatRefundAmount
        {
            get
            {
                return string.Format("{0:###,###}", RefundAmount);
            }
        }
        public string FormatAmountOnOrderPOS
        {
            get
            {
                return string.Format("{0:###,###}", AmountOnOrderPOS);
            }
        }

        public string FormatGrossTransactionAmountOE
        {
            get
            {
                return string.Format("{0:###,###}", GrossTransactionAmountOE);
            }
        }
        public string FormatRedeemPointOE
        {
            get
            {
                return string.Format("{0:###,###}", RedeemPointOE);
            }
        }

        public string FormatAwardAmountOE
        {
            get
            {
                return string.Format("{0:###,###}", AwardAmountOE);
            }
        }

        public string FormatRedeemAmountOE
        {
            get
            {
                return string.Format("{0:###,###}", RedeemAmountOE);
            }
        }

    }


    public class ExportTransactionLoyaltyModel
    {
        public string StoreID { get; set; }
        public string TerminalId { get; set; }
        public string DateTimeStr { get; set; }
        public string CardNumber { get; set; }
        public string QRCode { get; set; }
        public string InvoiceNo { get; set; }
        public string OrderNo { get; set; }
        public string OrigOrderNo { get; set; }
        public string OrigInvoiceNo { get; set; }
        public string TransactionName { get; set; }
        public Nullable<double> AmountOnOrderPOS { get; set; }
        public Nullable<double> GrossTransactionAmountOE { get; set; }
        public Nullable<double> RefundAmount { get; set; }
        public Nullable<double> AwardPointOE { get; set; }          // Diem tich
        public Nullable<double> SpendPoints { get; set; }           // Diem tieu
        public Nullable<double> AwardAmountOE { get; set; }         // Tien tich diem
        public Nullable<double> RedeemAmountOE { get; set; }        // Tien tieu diem

    }

}
