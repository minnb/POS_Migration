using Newtonsoft.Json;
using System.Collections.Generic;

namespace TCX.API.Common.Models
{
    public class ResponseCheckUrboxModel
    {
        public int done { get; set; }
        public int type { get; set; }
        public int status { get; set; }
        public UrboxResponseData data { get; set; }

    }
    public class ResponseMessageUrbox
    {
        public string message { get; set; }
        public bool status { get; set; }

    }
    public class CheckUrboxResponse
    {
        public int done { get; set; }
        public int type { get; set; }
        public int status { get; set; }
        public UrboxResponseData data { get; set; }
    }

    public class CheckUrboxErrorResponse
    {
        [JsonProperty("0")]
        public CheckUrboxResponse Zero { get; set; }
    }

    public class UrboxResponseData
    {
        public string msg { get; set; }
        public string code { get; set; }
        public int amount { get; set; }
        public int brand_id { get; set; }
        public int supplier_id { get; set; }
        public int gift_detail_id { get; set; }
        public string gift_title { get; set; }
        public List<string> sku_apply { get; set; } 
    }
    public class PayErrorUrboxResponse
    {
        public List<CheckUrboxResponse> Original { get; set; }
    }

    public class UrboxProducts
    {
        public string product_code { get; set; }
        public int quantity { get; set; }
        public int total_price { get; set; }
    }
}
