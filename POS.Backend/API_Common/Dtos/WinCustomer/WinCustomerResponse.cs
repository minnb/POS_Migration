using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.WinCustomer
{
    public class WinCustomerResponse
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public string Message { get; set; }
        public List<object> TechMsg { get; set; }
        public WinCustomerDataResponse Data { get; set; }
    }
    public class WinCustomerDataResponse
    {
        public string TraceId { get; set; }
    }

}