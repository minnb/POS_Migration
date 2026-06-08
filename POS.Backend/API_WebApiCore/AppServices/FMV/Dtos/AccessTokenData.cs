using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TCX.WebApiCore.AppServices.FMV.Dtos
{
    public class AccessTokenData
    {
        public string Access_token { get; set; }
        public string Token_type { get; set; }
        public string Expires_in { get; set; }
        public string Refresh_token { get; set; }
    }
}