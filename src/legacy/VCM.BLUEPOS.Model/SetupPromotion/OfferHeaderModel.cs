using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class OfferHeaderModel
    {
        public string No { get; set; }
        public string Type { get; set; }
        public string SalesType { get; set; }
        public string Description { get; set; }

        public int Status { get; set; }
        public bool isTotalBill { get; set; }
        public double AmountTotalBill { get; set; }
        public string OfferType { get; set; }      
        public System.DateTime LastDateModified { get; set; }       
        public System.DateTime StartingDate { get; set; }
        public System.DateTime EndingDate { get; set; }

        public string BuyLinkCat { get; set; }
        public string CheckMinValue { get; set; }
        public string TotalMinValue { get; set; }


        public string GetLinkCat { get; set; }       
        public string CheckDiscount { get; set; }
        public string DiscountType { get; set; }
        public string DiscountValue { get; set; }


        public int ConditionBuy { get; set; }
        public byte MemberOnly { get; set; }
        public int ConditionGet { get; set; }
        public string NoSeries { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public byte Mon { get; set; }
        public byte Tue { get; set; }
        public byte Wed { get; set; }
        public byte Thu { get; set; }
        public byte Fri { get; set; }
        public byte Sat { get; set; }
        public byte Sun { get; set; }
        public int NumOfDays { get; set; }
        public string DayOfWeek { get; set; }       
        public string LocalSiteGroup { get; set; }
        public Nullable<double> LimitQty { get; set; }
        public Nullable<System.DateTime> VoucherFromDate { get; set; }
        public Nullable<System.DateTime> VoucherToDate { get; set; }
        public string VoucherFromDateStr { get; set; }
        public string VoucherToDateStr { get; set; }

        public Nullable<int> VoucherValidDay { get; set; }
        public Nullable<int> VoucherLimitNumber { get; set; }
        public Nullable<int> PriorityBBY { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
        public bool IsVoucherHeader { get; set; }
        public bool IsApprove { get; set; }
        public string MemberCode { get; set; }
        public bool IsOfferSelected { get; set; }
        public Nullable<int> AllowUseAfterDay { get; set; }
        public string AllowUseAfterTime { get; set; }
    }
}
