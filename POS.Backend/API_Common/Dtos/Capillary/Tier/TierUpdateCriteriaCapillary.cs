using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Tier
{
    public class TierUpdateCriteriaCapillary
    {
        public string Purchase_type { get; set; }
        public string Description { get; set; }
        public string Min_purchase_value_required { get; set; }
    }
}
