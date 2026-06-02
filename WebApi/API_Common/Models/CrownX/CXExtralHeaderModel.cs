using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class CXExtralHeaderModel
    {
        public long ID { get; set; }
        public string StoreNo { get; set; }
        public string POSCode { get; set; }
        public string OrderNo { get; set; }
        public string TransactionType { get; set; }
        public Nullable<double> TransactionTime { get; set; }
        public string CustomerName { get; set; }
        public string CardNumber { get; set; }
        public string PhoneNUmber { get; set; }
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
        public Nullable<double> Rate { get; set; }
        public string ActionAPI { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedUser { get; set; }
    }

    public class CXExtralItemBillModel
    {
        public string ReceiptNumber { get; set; }
        public Nullable<double> RecordNo { get; set; }
        public string Barcode { get; set; }
        public string Article { get; set; }
        public string ArticleName { get; set; }
        public string UOM { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> RefundQuantity { get; set; }
        public Nullable<double> SalePrice { get; set; }
        public Nullable<double> Amount { get; set; }
        public Nullable<double> DiscountAmount { get; set; }
        public Nullable<double> LineAmount { get; set; }
        public Nullable<double> ExtraQuantityEarn { get; set; }
        public Nullable<double> ExtraAmountEarn { get; set; }
        public Nullable<double> ExtraQuantityRefund { get; set; }
        public Nullable<double> ExtraAmountRefund { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedUser { get; set; }
    }
}
