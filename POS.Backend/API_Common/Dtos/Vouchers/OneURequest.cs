using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Vouchers
{
    public class TokenOneURequest
    {
        public string Client_id { get; set; }
        public string Client_secret { get; set; }
        public string Audience { get; set; }
    }
    public class EstimateRequest
    {
        public string Usecase { get; set; }
        public string Invoice_no { get; set; }
        public List<VoucherDataItem> Apply_items { get; set; }
        public CheckoutDataOneU Checkout_data { get; set; }
    }
    public class CheckoutDataOneU
    {
        public decimal Total_amount { get; set; }
        public int Id { get; set; }
        public MerchantOunU Merchant { get; set; }
        public MerchantOunU Store { get; set; }
        public List<MoneySourcesOneU> Money_sources { get; set; }
        public OrderDetailsOneU Order_details { get; set; }
    }
    public class OrderDetailsOneU 
    {
        public string Code { get; set; }
    }
    public class MoneySourcesOneU 
    {
        public string Payment_method_code { get; set; }
    }
    public class MerchantOunU 
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
    public class VoucherDataItem 
    { 
        public string Type { get; set; }
        public string Serial { get; set; }
        public string Merchant_code { get; set; }
    }
}
