using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class MemberBalanceDto
    {
        public string MemberCard { get; set; }
        public int QuantityEarn { get; set; }
        public int QuantityRedeem { get; set; }
        public int QuantityRemain { get; set; }
        public bool IsGift { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
