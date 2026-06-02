using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class ExtratItemBillModel
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
