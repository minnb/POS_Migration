using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupCoupon
{
    public class CpnVchBOMIssueRuleModel
    {
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string UOM { get; set; }
        public int? DiscountType { get; set; }
        public double? DiscountValue { get; set; }
        public DateTime? StartingDate { get; set; }
        public DateTime? EndingDate { get; set; }
        public string CpnVchType { get; set; }
        public string Prefix { get; set; }
        public Nullable<int> LenCode { get; set; }
        public string IssueType { get; set; }
        public Nullable<int> CharOfNumber { get; set; }
        public Nullable<int> CharPosition { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<bool> IsCheckAPI { get; set; }
        public string SaleType { get; set; }
        public string StoreGroupCode { get; set; }
        public Nullable<bool> IsMultiUse { get; set; }
        public Nullable<bool> IsCheckItem { get; set; }
        public Nullable<int> LimitQtyUsed { get; set; }
        public byte Blocked { get; set; }
        public int LimitQty { get; set; }
        public Nullable<double> MaxAmount { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
        public int QtyCoupon { get; set; }
        public bool Status { get; set; }

    }
}
