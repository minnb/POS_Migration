using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary
{
    public class CardLevelDto
    {
        public string AppCode { get; set; }
        public string Level { get; set; }
        public string ToLevel { get; set; }
        public bool Blocked { get; set; }
        public string Description { get; set; }
    }
}
