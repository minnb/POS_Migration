using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Transaction
{
    public class TransactionDataCapillary
    {
        public string Id { get;set;}
        public string Number { get; set; }
        public string Type { get; set; }
        public string Created_date { get; set; }
        public string Store { get; set; }
    }
    public class TransactionsCapillary
    {
        public List<TransactionDataCapillary> Transaction { get; set; }
    }
    public class CouponsCapillary
    {
        public List<DataCouponsCapillary> Coupon { get; set; }
    }
    public class DataCouponsCapillary
    {
        public string Id { get; set; }
        public string Series_id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public DateTime Created_date { get; set; }
        public DateTime Valid_till { get; set; }
        public string Redeemed { get; set; }
        public string Same_user_multiple_redeem { get; set; }
    }
}
