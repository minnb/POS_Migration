using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary
{
    public class StatusCodeCapillary
    {
        public string Success { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public string Total { get; set; }
        public string Success_count { get; set; }

    }
}
