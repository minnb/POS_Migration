using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class MemoryCacheConfig
    {
        public string Code { get; set; }      
        public string Desc { get; set; }
        public string MemoryCacheType { get; set; }
        public bool Blocked { get; set; }
        public string ListStore {  get; set; }
    }
}
