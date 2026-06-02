using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Capillary.Transaction;

namespace TCX.API.Common.Dtos.Capillary.Coupons
{
    public class CheckActiveCouponResponseCap
    {
        public DataCheckActiveCouponResponseCap Response { get; set; }
    }
    public class DataCheckActiveCouponResponseCap
    {
        public PaginationResponseCap Pagination { get; set; }
        public StatusCodeCouponCapillary Status { get; set; }
        public CustomersResponseInCoupon Customers { get; set; }
    }
    public class CustomersResponseInCoupon
    {
        public List<DataCustomersResponseInCoupon> Customer { get; set; }
    }
    public class DataCustomersResponseInCoupon
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string External_id { get; set; }
        public string Mobile { get; set; }
        public CouponsCapillary Coupons { get; set; }
        public ItemStatusCapillary Item_status { get; set; }

    }
    public class PaginationResponseCap
    {
        public string Limit { get; set; }
        public string Offset { get; set; }
        public int Total { get; set; }
    }
}
