using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class CreateDataResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }


    public class CreateDataResponse<T>
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public Exception Exception { get; set; }
    }

    public class UpdateDataResponse<T>
    {
        public bool Status { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}
