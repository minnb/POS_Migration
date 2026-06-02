using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class SalesResponse
    {
        public SalesMeta meta { get; set; }
        public SalesData data { get; set; }
    }

    public class SalesMeta
    {
        public string request_id { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }
    public class SalesData
    {
        public CustomerIdentifierResponse customer_identifier { get; set; }
        public HeaderResponse rs_header { get; set; }
        public TransactionResponse transaction { get; set; }
        public List<RedeemPoolResponse> redeem_pool_list { get; set; }
        public Sales_Member_balance member_balance { get; set; }
    } 
   
    public class Sales_Member_balance
    {
        public PoolResponse pool { get; set; }
        public List<AwardsResponse> awards { get; set; }
        public List<Sales_Redeems> redeems { get; set; }
    }      
    public class Sales_Redeems
    {
        public string redeem_scheme { get; set; }
        public string redeem_units { get; set; }
        public string pool_id { get; set; }

    }
}
