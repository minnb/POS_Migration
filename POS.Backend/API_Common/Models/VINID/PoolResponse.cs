using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class PoolResponse
    {
        public string pool_description { get; set; }
        public string pool_type { get; set; }
        public string total_pool_units { get; set; }
        public string redeemable_pool_units { get; set; }
        public string earliest_expiry_pool_units { get; set; }
        public string earliest_expiry_date { get; set; }
    }
}
