using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class ExtraRefundPOSResponse
    {
        public Nullable<double> RefundAmount { get; set; }
        public Nullable<double> RefundPointDefault { get; set; }
        public Nullable<double> ExtraRefundByItems { get; set; }
        public Nullable<double> ExtraRefundByCampaign { get; set; }
        public string CompanyCode { get; set; }
        public string EmployCode { get; set; }
        public List<ExtraRefundtemPOSResponse> ListItem { get; set; }


    }
    public class ExtraRefundtemPOSResponse
    {
        public Nullable<double> RecordNo { get; set; }
        public string Barcode { get; set; }
        public string Article { get; set; }
        public string ArticleName { get; set; }
        public string UOM { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> SalePrice { get; set; }
        public Nullable<double> Amount { get; set; }
        public Nullable<double> DiscountAmount { get; set; }
        public Nullable<double> LineAmount { get; set; }
        //public Nullable<double> ExtraQuantityRefund { get; set; }
        //public Nullable<double> ExtraAmountRefund { get; set; }
        public float? ExtraQuantityEarn { get; set; }
        public float? ExtraAmountEarn { get; set; }
    }
}
