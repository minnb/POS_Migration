using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Enums
{
    public enum EStatusResponse
    {
        [Description("Success")]
        Success = 1,        
        [Description("Fail")]
        Fail = 0,
        [Description("Error")]
        Error = -1,
        [Description("503 Service Unavailable")]
        ServiceUnavailable = 6688503,
    }
}
