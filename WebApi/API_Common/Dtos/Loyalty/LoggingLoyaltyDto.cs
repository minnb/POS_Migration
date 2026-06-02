using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class LoggingLoyaltyDto
    {
        public string AppCode { get; set; }
        public string OrderNo { get; set; }
        public string MemberCardNo { get; set; }
        public string ActionType { get; set; }
        public long LoyaltyPoints { get; set; }
        public string Transaction { get; set; }
        public string Status { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public DateTime CrtDate { get; set; }
        public string OrigOrderNo { get; set; }
        public string Items { get; set; }
        public string CustName { get; set; }
        public int TransactionType { get; set; }
    }
}
