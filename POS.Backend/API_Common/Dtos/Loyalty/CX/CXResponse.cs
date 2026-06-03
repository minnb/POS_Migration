using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.CX
{
    public class BadRequestCX
    {
        public string Message { get; set; }
    }
    public class CXResponse
    {
        public string Message { get; set; }
        public string DeveloperMessage { get; set; }
        public object Data { get; set; }
    }

    public class CXResponseData
    {
        public string MessageId { get; set; }
        public string Status { get; set; }
    }

    public class OtpResponse
    {
        public string Message { get; set; }
        public string DeveloperMessage { get; set; }
        public CXResponseData Data { get; set; }
    }

}
