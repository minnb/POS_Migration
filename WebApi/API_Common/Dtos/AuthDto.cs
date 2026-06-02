using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos
{
    public class AuthDto
    {
        public string AppCode { get; set; } = string.Empty;
        public string StoreNo { get; set; } = string.Empty;
        public string PosNo { get; set; } = string.Empty;
    }
}
