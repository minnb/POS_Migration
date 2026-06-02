using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.WinX
{
    public class OrderDataWinXResponse
    {
        [JsonPropertyName("bill")]
        public BillWinX Bill { get; set; }
    }
    public class BillWinX
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("user")]
        public UserWinX User { get; set; }

        [JsonPropertyName("amounts")]
        public AmountWinX Amounts { get; set; }

        [JsonPropertyName("lineItems")]
        public List<LineItemWinX> LineItems { get; set; }
    }

    public class UserWinX
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }
    }

    public class AmountWinX
    {
        [JsonPropertyName("gross")]
        public decimal Gross { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("net")]
        public decimal Net { get; set; }
    }

    public class LineItemWinX
    {
        [JsonPropertyName("lineId")]
        public int LineId { get; set; }

        [JsonPropertyName("itemCode")]
        public string ItemCode { get; set; }

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
        [JsonPropertyName("uom")]
        public string UOM { get; set; }
        [JsonPropertyName("pricing")]
        public PricingWinX Pricing { get; set; }
    }

    public class PricingWinX
    {
        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("tax")]
        public decimal Tax { get; set; }
    }
}
