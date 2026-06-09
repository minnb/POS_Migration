using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Web;
using TCX.API.Common.Dtos.AkaChain;

namespace TCX.API.Common.Dtos
{
    public class AddTransactionAkaChainRequest
    {
        [JsonPropertyName("MemberKeys")]
        public MemberKeysAkaChain MemberKeys { get; set; } = new MemberKeysAkaChain();

        [JsonPropertyName("UsePoint")]
        public int UsePoint { get; set; }

        [JsonPropertyName("IsSimulation")]
        public bool IsSimulation { get; set; }

        [JsonPropertyName("SimulationOfferId")]
        public object SimulationOfferId { get; set; } 

        [JsonPropertyName("CustomEntityDataId")]
        public object CustomEntityDataId { get; set; }

        [JsonPropertyName("ActivityCode")]
        public string ActivityCode { get; set; } = string.Empty;

        [JsonPropertyName("State")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("CouponCode")]
        public List<CouponDetailAkaChain> CouponCode { get; set; } = new List<CouponDetailAkaChain>();

        [JsonPropertyName("ActivityData")]
        public ActivityDataAkaChain ActivityData { get; set; } = new ActivityDataAkaChain();

        [JsonPropertyName("TouchPointCode")]
        public object TouchPointCode { get; set; }

        [JsonPropertyName("UseBasePromotionSchemes")]
        public bool UseBasePromotionSchemes { get; set; }
    }

    public class MemberKeysAkaChain
    {
        [JsonPropertyName("Phone")]
        public string Phone { get; set; } = string.Empty;
    }

    public class ActivityDataAkaChain
    {
        [JsonPropertyName("BusinessTime")]
        public string BusinessTime { get; set; }

        [JsonPropertyName("Description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("TotalCalcAmount")]
        public decimal TotalCalcAmount { get; set; } // Dùng decimal cho số tiền để đảm bảo độ chính xác tài chính

        [JsonPropertyName("OrderCode")]
        public string OrderCode { get; set; } = string.Empty;

        [JsonPropertyName("CartItems")]
        public List<CartItemAkaChain> CartItems { get; set; } = new List<CartItemAkaChain>();

        [JsonPropertyName("StoreCode")]
        public string StoreCode { get; set; } = string.Empty;

        [JsonPropertyName("OriginalOrderCode")]
        public string OriginalOrderCode { get; set; }
        public bool IsReturn { get; set; }
    }

    public class CartItemAkaChain
    {
        [JsonPropertyName("ProductCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("ProductCategory")]
        public string ProductCategory { get; set; } = string.Empty;

        [JsonPropertyName("Quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("UnitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("ProductName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("TotalAmount")]
        public decimal TotalAmount { get; set; }
    }
}