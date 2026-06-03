using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary
{
    public class ItemStatusCapillary
    {
        public string Success { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
    }
    public class ErrorCapillary
    {
        public bool Status { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
    }
}
