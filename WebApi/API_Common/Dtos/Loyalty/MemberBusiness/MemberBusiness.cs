using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.MemberBusiness
{
    public class MemberBusiness
    {   
        public string MemberCard {  get; set; }
        public string MemberStore { get; set; }
        public string StoreNo { get; set; }
        public string DistcountType { get; set; }
        public decimal DistcountValue { get; set; }
        public string CardLevel { get; set; }
        public bool Blocked { get; set; }
        public string Description { get; set; }
        public DateTime CrtDate { get; set; }
    }
}
