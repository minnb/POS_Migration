using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos
{
    public class RedeemCouponAkaChainRequest
    {
        [JsonPropertyName("MemberKeys")]
        public MemberKeysAkaChain MemberKeys { get; set; } = new MemberKeysAkaChain();

        [JsonPropertyName("UsePoint")]
        public decimal? UsePoint { get; set; }

        [JsonPropertyName("CouponCode")]
        public List<UseCouponCodeAkaChain> CouponCode { get; set; } = new List<UseCouponCodeAkaChain>();

        [JsonPropertyName("ActivityCode")]
        public string ActivityCode { get; set; } = string.Empty;

        [JsonPropertyName("State")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("ActivityData")]
        public ActivityDataAkaChain ActivityData { get; set; } = new ActivityDataAkaChain();
    }
    public class UseCouponCodeAkaChain
    {
        [JsonPropertyName("Code")]
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("Status")]
        public string Status { get; set; }
    }
}
