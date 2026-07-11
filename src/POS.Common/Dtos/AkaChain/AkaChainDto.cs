using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace POS.Common.Dtos.AkaChain;

public class MemberProfileAkaChain
{
    public string? MemberId { get; set; }
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public string? Status { get; set; }
    public string? ReferralCode { get; set; }
    public long TotalCoin { get; set; }
    public long TotalPoint { get; set; }
    public string? CurrentTier { get; set; }
    public string? CurrentTierId { get; set; }
    public string? NextTierTarget { get; set; }
    public string? IsEligibleForSample { get; set; }
    public string? LoyaltyId { get; set; }
    public List<object> Coins { get; set; } = [];
    public List<DynamicAttributesAkaChain> DynamicAttributes { get; set; } = [];
}

public class DynamicAttributesAkaChain
{
    public string? AttributeId { get; set; }
    public string? AttributeKeyJson { get; set; }
    public string? AttributeValue { get; set; }
}

public class AccessTokenDataAkaChain
{
    public string? Access_token { get; set; }
    public string? Token_type { get; set; }
    public int Expires_in { get; set; }
    public string? Refresh_token { get; set; }
}

public class AkaChainErrorResponse
{
    [JsonProperty("error")]
    public ErrorDetailsAkaChain Error { get; set; } = new();
}

public class ErrorDetailsAkaChain
{
    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("details")]
    public string Details { get; set; } = string.Empty;

    [JsonProperty("data")]
    public Dictionary<string, object> Data { get; set; } = [];

    [JsonProperty("validationErrors")]
    public List<ValidationErrorAkaChain> ValidationErrors { get; set; } = [];
}

public class ValidationErrorAkaChain
{
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("members")]
    public List<string> Members { get; set; } = [];
}

public class AddTransactionAkaChainRequest
{
    public MemberKeysAkaChain MemberKeys { get; set; } = new();
    public int UsePoint { get; set; }
    public bool IsSimulation { get; set; }
    public object? SimulationOfferId { get; set; }
    public object? CustomEntityDataId { get; set; }
    public string ActivityCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public List<CouponDetailAkaChain> CouponCode { get; set; } = [];
    public ActivityDataAkaChain ActivityData { get; set; } = new();
    public object? TouchPointCode { get; set; }
    public bool UseBasePromotionSchemes { get; set; }
}

public class MemberKeysAkaChain
{
    public string Phone { get; set; } = string.Empty;
}

public class ActivityDataAkaChain
{
    public string? BusinessTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalCalcAmount { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public List<CartItemAkaChain> CartItems { get; set; } = [];
    public string StoreCode { get; set; } = string.Empty;
    public string? OriginalOrderCode { get; set; }
    public bool IsReturn { get; set; }
}

public class CartItemAkaChain
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class AddTransactionAkaChainResponse
{
    [JsonProperty("memberId")]
    public string? MemberId { get; set; }

    [JsonProperty("memberRank")]
    public string MemberRank { get; set; } = string.Empty;

    [JsonProperty("memberBalance")]
    public decimal MemberBalance { get; set; }

    [JsonProperty("activityEntityValueId")]
    public string? ActivityEntityValueId { get; set; }

    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;

    [JsonProperty("usedPoint")]
    public decimal UsedPoint { get; set; }

    [JsonProperty("totalDiscount")]
    public decimal TotalDiscount { get; set; }

    [JsonProperty("returnOldValue")]
    public bool ReturnOldValue { get; set; }

    [JsonProperty("rewardBalance")]
    public List<RewardBalanceAkaChain> RewardBalance { get; set; } = [];

    [JsonProperty("couponCode")]
    public List<CouponDetailAkaChain> CouponCode { get; set; } = [];

    [JsonProperty("offerData")]
    public List<OfferDetailAkaChain> OfferData { get; set; } = [];

    [JsonProperty("tempOfferData")]
    public List<OfferDetailAkaChain> TempOfferData { get; set; } = [];

    [JsonProperty("effects")]
    public object? Effects { get; set; }
}

public class RewardBalanceAkaChain
{
    public string? CurrencyId { get; set; }
    public string? CurrencyName { get; set; }
    public decimal Value { get; set; }
}

public class OfferDetailAkaChain
{
    [JsonProperty("offerId")]
    public Guid OfferId { get; set; }

    [JsonProperty("offerName")]
    public string OfferName { get; set; } = string.Empty;

    [JsonProperty("ruleData")]
    public List<RuleDetailAkaChain> RuleData { get; set; } = [];
}

public class RuleDetailAkaChain
{
    [JsonProperty("ruleId")]
    public Guid RuleId { get; set; }

    [JsonProperty("ruleName")]
    public string RuleName { get; set; } = string.Empty;

    [JsonProperty("loyaltyCurrency")]
    public List<LoyaltyCurrencyAkaChain> LoyaltyCurrency { get; set; } = [];

    [JsonProperty("reward360")]
    public List<object> Reward360 { get; set; } = [];

    [JsonProperty("updateMemberAttribute")]
    public List<object> UpdateMemberAttribute { get; set; } = [];
}

public class LoyaltyCurrencyAkaChain
{
    [JsonProperty("currencyId")]
    public Guid CurrencyId { get; set; }

    [JsonProperty("currencyTagId")]
    public Guid CurrencyTagId { get; set; }

    [JsonProperty("currencyName")]
    public string CurrencyName { get; set; } = string.Empty;

    [JsonProperty("currencyTagName")]
    public string CurrencyTagName { get; set; } = string.Empty;

    [JsonProperty("value")]
    public decimal Value { get; set; }
}

public class CouponDetailAkaChain
{
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("canUse")]
    public bool CanUse { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    public string? Reason { get; set; }
}

public class RedeemCouponAkaChainRequest
{
    public MemberKeysAkaChain MemberKeys { get; set; } = new();
    public decimal? UsePoint { get; set; }
    public List<UseCouponCodeAkaChain> CouponCode { get; set; } = [];
    public string ActivityCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public ActivityDataAkaChain ActivityData { get; set; } = new();
}

public class UseCouponCodeAkaChain
{
    public string Code { get; set; } = string.Empty;
    public string? Status { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ReturnState
{
    FullReturn,
    PartialReturn
}

public class InputReturnDataRequest
{
    [JsonProperty("memberKeys")]
    public ReturnMemberKeysAkaChain MemberKeys { get; set; } = new();
    public string ActivityCode { get; set; } = string.Empty;
    public string? State { get; set; }
    public ActivityDataAkaChain ActivityData { get; set; } = new();
}

public class ReturnMemberKeysAkaChain
{
    public string PartnerLoyaltyId { get; set; } = string.Empty;
}

public class InputReturnDataResponse
{
}

public class InputMemberDataAkaChainRequest
{
    public string EnrollmentFormCode { get; set; }= string.Empty;
    public MemberDataInputAkaChain MemberData { get; set; } = new();
}
public class MemberDataInputAkaChain
{
    public string Address { get; set; } = string.Empty;
    public string DOB { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TierGroup { get; set; } = string.Empty;
}
public class InputMemberDataAkaChainResponse
{
    public string MemberId{ get; set; } = string.Empty;
    public string LoyaltyId { get; set; } = string.Empty;
    public string TierId { get; set; } = string.Empty;
}


