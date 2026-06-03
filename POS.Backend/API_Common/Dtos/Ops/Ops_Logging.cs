using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.POSBLUE.Model.VINID;

namespace TCX.API.Common.Dtos.Ops
{
    public class Ops_Logging:Ops_Header
    {
        public Ops_Logging_Data Data { get; set; }
    }
   public class Ops_Logging_Data
    {
        public string Server_code { get; set; }
        public string Group_name { get; set; }
        public string App_module { get; set; }
        public string Error_code { get; set; }
        public string Error_type { get; set; }
        public string Severity { get; set; }
        public int Http_status { get; set; }
        public string Endpoint { get; set; }
        public string Error_message { get; set; }
        public string Request_id { get; set; }
        public object Context { get; set; }
    }
}
