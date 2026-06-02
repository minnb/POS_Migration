using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.WinX
{
    public class ResolveWinxUserIdRequest
    {
        public List<string> Winx_user_ids { get; set; }

    }
    public class ResolveWinxUserIdCapillary
    {
        public int Status { get; set; }
        public ResolveWinxUserIdCapillaryData Data { get; set; }
        public string Message { get; set; }
    }
    public class ResolveWinxUserIdCapillaryData
    {
        public List<WinxUserIdCapillaryData> Data { get; set; }
        public List<ResponseErrorDynamicVoucherWinX> Errors { get; set; }
    }
    public class WinxUserIdCapillaryData
    {
        public string Winx_user_id { get; set; }
        public string Capillary_user_id { get; set; }
        public string Phone { get; set; }
        public string Valid_from { get; set; }
        public string Valid_until { get; set; }
    }
}
