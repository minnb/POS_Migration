using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class RefundLoyaltyPointsResponse
    {
        public RefundLoyaltyPointsMeta meta { get; set; }
        public RefundLoyaltyPointsData data { get; set; }
    }
    public class RefundLoyaltyPointsMeta
    {
        public string request_id { get; set; }
        public int code { get; set; }
    }
    public class RefundLoyaltyPointsData
    {
        CustomerIdentifierResponse customer_identifier { get; set; }
        HeaderResponse rs_header { get; set; }
        TransactionResponse transaction { get; set; }
        MemberBalanceResponse member_balance { get; set; }
        MemberProfileResponse member_profile { get; set; }
        MemberAccountResponse member_accounts { get; set; }
    }
}
