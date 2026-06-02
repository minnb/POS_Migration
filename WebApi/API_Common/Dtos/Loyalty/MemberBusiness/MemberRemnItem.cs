using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.MemberBusiness
{
    public class MemberRemnItem
    {
        public string Month { get; set; }
        public string CardLevel { get; set; }
        public string MemberCard { get; set; }
        public string ItemNo { get; set; }
        public string Uom { get; set; }
        public decimal MaxValue { get; set; }
        public decimal UsedValue { get; set; }
        public decimal RemnValue { get; set; }
        public string OrderNo { get; set; }
        public long Id { get; set; }
        public string Type { get; set; }
        public DateTime CrtDate { get; set; }
    }

    public class MemberRemnItemGroupBy
    {
        public string Month { get; set; }
        public string CardLevel { get; set; }
        public string MemberCard { get; set; }
        public string ItemNo { get; set; }
        public string Uom { get; set; }
        public decimal UsedValue { get; set; }
    }
}
