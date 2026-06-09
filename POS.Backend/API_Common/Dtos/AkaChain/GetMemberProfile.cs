using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;

namespace TCX.API.Common.Dtos
{
    public class MemberProfileAkaChain
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
        public List<DynamicAttributesAkaChain> DynamicAttributes { get; set; } = new List<DynamicAttributesAkaChain>();
    }

    public class DynamicAttributesAkaChain
    {
        public string AttributeId { get; set; }
        public string AttributeKeyJson { get; set; }
        public string AttributeValue { get; set; }
    }
}