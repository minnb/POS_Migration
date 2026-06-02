using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.WinX
{
    public class PosPostTransactionsRequest
    {
        public string Bill_id { get; set; }
    }

    public class PosPostTransactionsResponse
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
