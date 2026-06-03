using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Enums
{
    public enum OpsActionEnum
    {
        upsert,
        insert
    }
    public enum OpsStatusEnum
    {
        running,
        idle,
        error,
        unknown,
        warning,
        critical
    }
}
