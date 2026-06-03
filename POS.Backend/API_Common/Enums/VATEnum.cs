using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Enums
{
    public enum VATCheckEnum
    {
        ExistsTemp,
        ExistsEOD,
        NotExistsTrans,
        OverTime,
        OverDate,
        OrderReturned,
        NotExistsStore,
        OrderInPLH
    }
}
