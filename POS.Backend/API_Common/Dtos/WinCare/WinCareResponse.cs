using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.WinCare
{
    public class WinCareResponse
    {
        public int StatusCode {  get; set; }
        public object Data { get; set; }
        public string StatusName { get; set; }
        public string MessageTechnical { get; set; }
        public string ErrorMessage { get; set; }

    }
}
