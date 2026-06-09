using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.AkaChain
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReturnState
    {
        FullReturn,
        PartialReturn
    }
    public class InputReturnDataRequest
    {
        [JsonPropertyName("memberKeys")]
        public ReturnMemberKeysAkaChain MemberKeys { get; set; } = new ReturnMemberKeysAkaChain();

        [JsonPropertyName("ActivityCode")]
        public string ActivityCode { get; set; } = string.Empty;

        [JsonPropertyName("State")]
        public string State { get; set; }

        [JsonPropertyName("ActivityData")]
        public ActivityDataAkaChain ActivityData { get; set; } = new ActivityDataAkaChain();
    }
    public class ReturnMemberKeysAkaChain
    {
        [JsonPropertyName("PartnerLoyaltyId")]
        public string PartnerLoyaltyId { get; set; } = string.Empty;
    }
}
