using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.CentralMD
{
    public class SysWebApiConfig
    {
        public string Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Prefix { get; set; }
        public string ConnectionString { get; set; }
        public string Description { get; set; }
        public string ProcedureName { get; set; }
        public bool Blocked { get; set; }
    }
    public class StoreSetConfig : SysWebApiConfig
    {
        public string StoreNo { get; set; } = string.Empty;
    }
}
