using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;

namespace TCX.WebApiCore.AppServices.FMV.Dtos
{
    public class MemberProfile
    {
        public string MemberId { get; set; }
        public string Phone { get; set; }
        public string FullName { get; set; }
        public string Status { get; set; }
        public string ReferralCode { get; set; }
        public long TotalCoin { get; set; }
        public long TotalPoint { get; set; }
        public string CurrentTier { get; set; }
        public string CurrentTierId { get; set; }
        public string NextTierTarget { get; set; }
        public string IsEligibleForSample { get; set; }
        public string LoyaltyId { get; set; }
        public List<object> Coins { get; set; } = new List<object>();
        public List<DynamicAttribute> DynamicAttributes { get; set; } = new List<DynamicAttribute>();
    }

    public class DynamicAttributes
    {
        public string AttributeId { get; set; }
        public string AttributeKeyJson { get; set; }
        public string AttributeValue { get; set; }
    }
}