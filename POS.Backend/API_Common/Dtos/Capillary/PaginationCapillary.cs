using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary
{
    public class PaginationCapillary
    {
        public int limit { get; set; }
        public int offset { get; set; }
        public int total { get; set; }
    }
}
