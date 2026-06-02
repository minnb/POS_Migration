using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Point
{
    public class PointTopupCapillaryRequest
    {
        public RootTopupCapillaryRequest Root { get; set; }
    }
    public class PointTopupPOSRequest
    {
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string OrderNo { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public long Points { get; set; }
        public string Comments { get; set; }
    }
    public class RootTopupCapillaryRequest
    {
        public List<RequestTopupCapillaryRequest> Request { get; set; }
    }
    public class RequestTopupCapillaryRequest
    {
        public CustomerTopupCapillary Customer { get; set; }
        public string Reason { get; set; }
        public string Type { get; set; }
        public string Base_type { get; set; }
        public string Points { get; set; }
        public string Comments { get; set; }

    }
    public class CustomerTopupCapillary 
    { 
        public string Mobile { get; set; }
    }
    public class ItemsTopUpCapillary
    {
        public string ItemNo { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineAmountIncVAT { get; set; }
        public int Points { get; set; }
    }
}
