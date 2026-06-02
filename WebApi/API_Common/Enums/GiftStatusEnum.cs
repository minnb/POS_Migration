using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Enums
{
    public enum GiftStatusEnum
    {
        CHECK = -1,
        CREATE = 0,
        AVAILABLE = 1,
        USED = 2,
        EXPIRED = 3,
        ERROR = 99
    }
}
