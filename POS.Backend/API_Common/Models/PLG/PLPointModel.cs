using System.Collections.Generic;

namespace TCX.API.Common.Models
{
    public class PLPointModel
    {
        public int? PointEarn { get; set; }
        public int? PointRedeem { get; set; }
        public int? CurrentRate { get; set; }
        public int RedeemStampsPoints { get; set; }
        public int RefundStampsPoints { get; set; }
        public List<PLPointEarnCampaint> ExtraEarnByCampaign { get; set; }
    }
    public class PLPointEarnCampaint
    {
        public string LoyaltyMerchantId { get; set; }
        public double Amount { get; set; }
        public double EarnedPoints { get; set; }
    }
}
