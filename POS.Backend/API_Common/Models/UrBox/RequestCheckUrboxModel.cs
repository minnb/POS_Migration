using System;
using System.Collections.Generic;

namespace TCX.API.Common.Models
{
    public class CheckUrboxRequestV2_SKU 
    {
        public int amount { get; set; }
        public string bill_at { get; set; }
        public string bill_id { get; set; }
        public int brand_id { get; set; }
        public string code { get; set; }//UB317974981
        public int has_order_detail { get; set; }
        public List<UrboxProducts> products { get; set; }
        public string store_id { get; set; }
        public string terminal_id { get; set; }
    }
    public class PayCodeToUrboxRequestProducts
    {
        public int amount { get; set; }
        public string bill_at { get; set; }
        public string bill_id { get; set; }
        public int brand_id { get; set; }
        public string code { get; set; }//UB317974981
        public int has_order_detail { get; set; }
        public List<UrboxProducts> products { get; set; }
        public string store_id { get; set; }
        public string terminal_id { get; set; }
    }

    public class CheckUrboxRequest
    {
        public int amount { get; set; }
        public string code { get; set; }//UB317974981
        public List<UrboxProducts> products { get; set; }
        public string store_id { get; set; }
        public string terminal_id { get; set; }
    }
    public class CheckUrboxRequestV2
    {
        public int amount { get; set; }
        public int brand_id { get; set; }
        public string code { get; set; }//UB317974981
        public string store_id { get; set; }
        public string terminal_id { get; set; }
    }
    public class PayCodeToUrboxRequest
    {
        public int amount { get; set; }
        public string bill_id { get; set; }
        public string code { get; set; }//UB317974981
        public string store_id { get; set; }
        public string terminal_id { get; set; }
    }
    public class PayCodeToUrboxRequestV2
    {
        public int amount { get; set; }
        public string bill_id { get; set; }
        public int brand_id { get; set; }
        public string code { get; set; }//UB317974981
        public string store_id { get; set; }
        public string terminal_id { get; set; }
    }
    public class RequestCheckUrboxModel
    {
        public int amount { get; set; }
        public string code { get; set; }//UB317974981
        //public string staff_id { get; set; }
        public string store_id { get; set; }
        public int brand_id { get; set; }
        public string terminal_id { get; set; }
    }
    public class RequestCheckUrboxPhucLong
    {
        //{"amount":0,"code":"UB571201564","staff_id":"","store_id":"phuclong1","terminal_id":"phuclong1"}
        public int amount { get; set; }
        public string code { get; set; }//UB317974981
        public string staff_id { get; set; }
        public string store_id { get; set; }
        public string terminal_id { get; set; }
    }
}
