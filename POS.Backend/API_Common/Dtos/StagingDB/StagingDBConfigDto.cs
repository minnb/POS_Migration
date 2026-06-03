using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.StagingDB
{
    public class StagingDBConfigDto
    {
        public string Action { get; set; }
        public string Execution { get; set; }
        public string Description { get; set; }
        public bool Blocked { get; set; }
    }
}
