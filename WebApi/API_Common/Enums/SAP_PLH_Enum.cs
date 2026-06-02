using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Enums
{
    public static class SAP_PLH_StatusConst
    {
        public static List<string> PLH_VoucherStatusConst = new List<string> { "EXPI", "REDE", "CANC", "SOLD" };
    }
    
    public enum PLH_VoucherStatusEnum
    {
        EXPI,
        REDE,
        CANC,
        SOLD
    }

}
