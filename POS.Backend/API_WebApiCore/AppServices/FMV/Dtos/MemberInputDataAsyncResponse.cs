using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Web;

namespace TCX.WebApiCore.AppServices.FMV.Dtos
{
    public class MemberInputDataAsyncResponse
    {
        [JsonPropertyName("memberId")]
        public Guid MemberId { get; set; }

        [JsonPropertyName("memberRank")]
        public string MemberRank { get; set; } = string.Empty;

        [JsonPropertyName("memberBalance")]
        public decimal MemberBalance { get; set; }

        [JsonPropertyName("activityEntityValueId")]
        public Guid ActivityEntityValueId { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("usedPoint")]
        public decimal UsedPoint { get; set; }

        [JsonPropertyName("totalDiscount")]
        public decimal TotalDiscount { get; set; }

        [JsonPropertyName("returnOldValue")]
        public bool ReturnOldValue { get; set; }

        [JsonPropertyName("rewardBalance")]
        public List<object> RewardBalance { get; set; } = new List<object>();

        [JsonPropertyName("couponCode")]
        public string CouponCode { get; set; }

        // Sử dụng chung class OfferDetail cho cả offerData và tempOfferData
        [JsonPropertyName("offerData")]
        public List<OfferDetail> OfferData { get; set; } = new List<OfferDetail>();

        [JsonPropertyName("tempOfferData")]
        public List<OfferDetail> TempOfferData { get; set; } = new List<OfferDetail>();

        [JsonPropertyName("effects")]
        public object Effects { get; set; }
    }

    // 2. Class cấu trúc của một Ưu đãi (Offer)
    public class OfferDetail
    {
        [JsonPropertyName("offerId")]
        public Guid OfferId { get; set; }

        [JsonPropertyName("offerName")]
        public string OfferName { get; set; } = string.Empty;

        [JsonPropertyName("ruleData")]
        public List<RuleDetail> RuleData { get; set; } = new List<RuleDetail>();
    }

    // 3. Class cấu trúc của một Quy tắc (Rule) bên trong Ưu đãi
    public class RuleDetail
    {
        [JsonPropertyName("ruleId")]
        public Guid RuleId { get; set; }

        [JsonPropertyName("ruleName")]
        public string RuleName { get; set; } = string.Empty;

        [JsonPropertyName("loyaltyCurrency")]
        public List<LoyaltyCurrency> LoyaltyCurrency { get; set; } = new List<LoyaltyCurrency>();

        [JsonPropertyName("reward360")]
        public List<object> Reward360 { get; set; } = new List<object>();

        [JsonPropertyName("updateMemberAttribute")]
        public List<object> UpdateMemberAttribute { get; set; } = new List<object>();
    }

    // 4. Class cấu trúc của Tiền tệ/Điểm thưởng (Currency) bên trong Quy tắc
    public class LoyaltyCurrency
    {
        [JsonPropertyName("currencyId")]
        public Guid CurrencyId { get; set; }

        [JsonPropertyName("currencyTagId")]
        public Guid CurrencyTagId { get; set; }

        [JsonPropertyName("currencyName")]
        public string CurrencyName { get; set; } = string.Empty;

        [JsonPropertyName("currencyTagName")]
        public string CurrencyTagName { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public decimal Value { get; set; }
    }
}