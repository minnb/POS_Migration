using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupCoupon
{
    public class SetupAdvancedCouponRequest
    {
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string ArticleType { get; set; }
        public string UOM { get; set; }
        public string IssueType { get; set; }
        public string CpnVchType { get; set; }
        public int DiscountType { get; set; }
        public double DiscountValue { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime EndingDate { get; set; }
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public double MaxValue { get; set; }
        public bool Blocked { get; set; }
        public bool IsMultiUsed { get; set; }
        public bool IsCheckAPI { get; set; }
        public int LimitQty { get; set; }
        public int LimitQtyUsed { get; set; }
        public string SaleType { get; set; }
        public string StoreGroupCode { get; set; }
    }
}
