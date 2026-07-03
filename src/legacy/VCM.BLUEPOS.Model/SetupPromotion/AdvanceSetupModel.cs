using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class AdvanceSetupModel
    {
        public List<DayModel> ListDayMonth { get; set; }
        public bool isTotalBill { get; set; }
        public bool isSetupBuy { get; set; }
        public bool isSetupGet { get; set; }
        public bool isVoucher { get; set; }
        public OfferHeaderModel GetOfferHeader { get; set; }
        //public string BonusBuy { get; set; }
    }

    public class DayModel
    {
        public int Day { get; set; }
        public string DayName { get; set; }
    }
    public class AdvenceSetupRequest
    {
        public bool isTotalBill { get; set; }
        public bool isSetupBuy { get; set; }
        public bool isSetupGet { get; set; }
        public bool isVoucher { get; set; }        
        public string BonusBuy { get; set; }
        public string SalesType { get; set; }
        public string OfferType { get; set; }
        public string Description { get; set; }
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }
        public string Status { get; set; }
        public bool isVoucherHeader { get; set; }
        public bool isApprove { get; set; }

        // Advanced settings (từ sessionStorage["OfferAdvance"])
        public bool HasAdvanceData { get; set; }
        public double LimitQty { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public byte Mon { get; set; }
        public byte Tue { get; set; }
        public byte Wed { get; set; }
        public byte Thu { get; set; }
        public byte Fri { get; set; }
        public byte Sat { get; set; }
        public byte Sun { get; set; }
        public byte MemberOnly { get; set; }
        public int PriorityBBY { get; set; }
        public string VoucherFromDateStr { get; set; }
        public string VoucherToDateStr { get; set; }
        public int VoucherValidDay { get; set; }
        public int VoucherLimitNumber { get; set; }
        public string MemberCode { get; set; }
        public int NumOfDays { get; set; }
        public int AllowUseAfterDay { get; set; }
        public string AllowUseAfterTime { get; set; }

        // Buy / Get rows
        public string ConditionBuy { get; set; }
        public string ConditionGet { get; set; }
        public List<OfferBuySetupModel> BuyRows { get; set; }
        public List<OfferGetSetupModel> GetRows { get; set; }
        public List<string> SiteRows { get; set; }
    }
}
