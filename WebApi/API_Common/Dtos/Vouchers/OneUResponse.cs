using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Vouchers
{
    public class TokenOneUResponse
    {
        public MetaOneResponse Meta { get; set; }
        public TokenDataOneUResponse Data { get; set; }
    }
    public class MetaOneResponse
    {
        public string Request_id { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public string Service_code { get; set; }
    }
    public class TokenDataOneUResponse
    {
        public string Access_token { get; set; }
        public string Token_type { get; set; }
        public int Expires_in { get; set; }
    }

    public class RedeemResponseOneU
    {
        public MetaOneResponse Meta { get; set; }
        public RedeemDataOneU Data { get; set; }
    }
    public class RedeemDataOneU
    {
       public string Transaction_id { get; set; }
       public decimal Total_discount { get; set; }
    }
    public class EstimateResponseOneU
    {
        public MetaOneResponse Meta { get; set; }
        public EstimateDataOneU Data { get; set; }
    }
    public class EstimateDataOneU
    {
        public decimal Total_discount { get; set; }
        public List<EstimateItemDetails> Item_details { get; set; }
    }
    public class EstimateItemDetails
    {
        public string Item_id { get; set; }
        public string Item_code { get; set; }
        public string Item_type { get; set; }
        public long Item_value { get; set; }
        public long Item_max_allow { get; set; }
        public List<string> Variant_code { get; set; }
        public string Payment_method { get; set; }
        public decimal Discount_amount { get; set; }
    }
}
