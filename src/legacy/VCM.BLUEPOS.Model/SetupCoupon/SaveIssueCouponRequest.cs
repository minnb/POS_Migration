using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupCoupon
{
    public class SaveIssueCouponRequest
    {
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string ArticleType { get; set; }
        public string SaleType { get; set; }
        public string StoreGroupCode { get; set; }
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime EndingDate { get; set; }
        public string Prefix { get; set; }
        public int LenCode { get; set; }
        public string IssueType { get; set; }
        public int CharOfNumber { get; set; }
        public int CharPosition { get; set; }
        public int Quantity { get; set; }
        public List<RandomCode> ListCode { get; set; }
        public int QuantityCodeInDB { get; set; }
        public Nullable<bool> IsCheckItem { get; set; }
        public string JsonItem { get; set; }

    }
    public class RandomCode
    {
        public string Code { get; set; }
    }
}
