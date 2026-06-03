using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class SpendPointsResponse
    {
        public SpendMeta meta { get; set; }
        public SpendData data { get; set; }
    }
    public class SpendMeta
    {
        public string request_id { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }
    public class SpendData
    {
        CustomerIdentifierResponse customer_identifier { get; set; }
        HeaderResponse rs_header { get; set; }
        TransactionResponse transaction { get; set; }
        List<RedeemPoolResponse> redeem_pool_list { get; set; }
        Spend_Member_balance member_balance { get; set; }
    }
    public class Spend_Member_balance
    {
        public PoolResponse pool { get; set; }
        public List<AwardsResponse> awards { get; set; }      
    }


}
