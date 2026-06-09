using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.AkaChain
{
     public class CouponDetailAkaChain
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } 

        [JsonPropertyName("canUse")]
        public bool CanUse { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } 

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("Reason")]
        public string Reason { get; set; } 
    }
}
