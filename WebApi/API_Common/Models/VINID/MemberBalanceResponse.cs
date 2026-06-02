using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class MemberBalanceResponse
    {
        PoolResponse pool { get; set; }
        List<AwardsResponse> awards { get; set; }
    }
}
