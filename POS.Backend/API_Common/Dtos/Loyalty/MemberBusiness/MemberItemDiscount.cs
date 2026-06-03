using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.MemberBusiness
{
    public class MemberItemDiscount
    {
        public string CardLevel { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Uom { get; set; }
        public string Month { get; set; }
        public decimal MaxValue { get; set; }
        public bool Blocked { get; set; }
        public DateTime CrtDate { get; set; }
    }
}
