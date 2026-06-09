using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Web;
using TCX.API.Common.Dtos.AkaChain;

namespace TCX.API.Common.Dtos
{
    public class AddTransactionAkaChainResponse
    {
        [JsonPropertyName("memberId")]
        public string MemberId { get; set; }

        [JsonPropertyName("memberRank")]
        public string MemberRank { get; set; } = string.Empty;

        [JsonPropertyName("memberBalance")]
        public decimal MemberBalance { get; set; }

        [JsonPropertyName("activityEntityValueId")]
        public string ActivityEntityValueId { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("usedPoint")]
        public decimal UsedPoint { get; set; }

        [JsonPropertyName("totalDiscount")]
        public decimal TotalDiscount { get; set; }

        [JsonPropertyName("returnOldValue")]
        public bool ReturnOldValue { get; set; }

        [JsonPropertyName("rewardBalance")]
        public List<RewardBalanceAkaChain> RewardBalance { get; set; } = new List<RewardBalanceAkaChain>();

        [JsonPropertyName("couponCode")]
        public List<CouponDetailAkaChain> CouponCode { get; set; } = new List<CouponDetailAkaChain>();

        // Sử dụng chung class OfferDetail cho cả offerData và tempOfferData
        [JsonPropertyName("offerData")]
        public List<OfferDetailAkaChain> OfferData { get; set; } = new List<OfferDetailAkaChain>();

        [JsonPropertyName("tempOfferData")]
        public List<OfferDetailAkaChain> TempOfferData { get; set; } = new List<OfferDetailAkaChain>();

        [JsonPropertyName("effects")]
        public object Effects { get; set; }
    }
    public class RewardBalanceAkaChain
    {
        public string CurrencyId { get; set; } //Mã loại điểm

        public string CurrencyName { get; set; } //"Tên thưởng (Ví dụ: điểm đổi quà (coin), điểm xếp hạng (point)) -> Điểm đổi quà tích được thì lấy coin"

        public decimal Value { get; set; } //Số điểm thưởng cho loại điểm đó

    }


    // 2. Class cấu trúc của một Ưu đãi (Offer)
    public class OfferDetailAkaChain
    {
        [JsonPropertyName("offerId")]
        public Guid OfferId { get; set; }

        [JsonPropertyName("offerName")]
        public string OfferName { get; set; } = string.Empty;

        [JsonPropertyName("ruleData")]
        public List<RuleDetailAkaChain> RuleData { get; set; } = new List<RuleDetailAkaChain>();
    }

    // 3. Class cấu trúc của một Quy tắc (Rule) bên trong Ưu đãi
    public class RuleDetailAkaChain
    {
        [JsonPropertyName("ruleId")]
        public Guid RuleId { get; set; }

        [JsonPropertyName("ruleName")]
        public string RuleName { get; set; } = string.Empty;

        [JsonPropertyName("loyaltyCurrency")]
        public List<LoyaltyCurrencyAkaChain> LoyaltyCurrency { get; set; } = new List<LoyaltyCurrencyAkaChain>();

        [JsonPropertyName("reward360")]
        public List<object> Reward360 { get; set; } = new List<object>();

        [JsonPropertyName("updateMemberAttribute")]
        public List<object> UpdateMemberAttribute { get; set; } = new List<object>();
    }

    // 4. Class cấu trúc của Tiền tệ/Điểm thưởng (Currency) bên trong Quy tắc
    public class LoyaltyCurrencyAkaChain
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