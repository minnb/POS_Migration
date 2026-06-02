using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.WinX
{
    public class WinxResponse
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }

    }
}
