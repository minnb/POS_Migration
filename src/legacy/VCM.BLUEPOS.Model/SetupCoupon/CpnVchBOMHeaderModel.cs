using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupCoupon
{
    public class CpnVchBOMHeaderModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public string CouponCode { get; set; }
        public int DiscountType { get; set; }
        public Nullable<double> DiscountValue { get; set; }
        public Nullable<double> MaxAmount { get; set; }
        public string ArticleType { get; set; }
        public decimal ValueOfVoucher { get; set; }
        public System.DateTime StartingDate { get; set; }       
       
        public System.DateTime EndingDate { get; set; }
      
        public byte Blocked { get; set; }
        public int LimitQty { get; set; }
        public System.DateTime LastDateModified { get; set; }
        public Nullable<long> Counter { get; set; }
        public Nullable<bool> IsCheckItem { get; set; }
        public string Pkey { get; set; }
        public Nullable<bool> IsMultiUse { get; set; }
        public Nullable<int> LimitQtyUsed { get; set; }
        public string CpnVchType { get; set; }
        public Nullable<bool> IsCheckAPI { get; set; }
        public string SaleType { get; set; }
        public string StoreGroupCode { get; set; }
        public bool Status { get; set; }

    }

    public class CpnVchBOMHeaderRequestModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public string CouponCode { get; set; }
        
        public int DiscountType { get; set; }
        public float DiscountValue { get; set; }
        public float MaxAmount { get; set; }
        public string ArticleType { get; set; }
        public System.DateTime StartingDate { get; set; }
        public string StartingDateStr { get; set; }
        public System.DateTime EndingDate { get; set; }
        public string EndingDateStr { get; set; }
        public byte Blocked { get; set; }
        public int LimitQty { get; set; }
        public System.DateTime LastDateModified { get; set; }
        public Nullable<long> Counter { get; set; }
        public Nullable<bool> IsCheckItem { get; set; }
        public string Pkey { get; set; }
        public Nullable<bool> IsMultiUse { get; set; }
        public Nullable<int> LimitQtyUsed { get; set; }
        public string CpnVchType { get; set; }
        public Nullable<bool> IsCheckAPI { get; set; }
        public string SaleType { get; set; }
        public string StoreGroupCode { get; set; }
        public string JsonItem { get; set; }
    }
}
