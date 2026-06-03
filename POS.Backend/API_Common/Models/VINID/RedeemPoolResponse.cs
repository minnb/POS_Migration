using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class RedeemPoolResponse
    {
        public string pool_id { get; set; }
        public string pool_units_to_redeem { get; set; }
        public string max_redeem { get; set; }
    }
}
