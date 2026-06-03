using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.WinCustomer;
using VCM.POSBLUE.Model.Dtos.ROP;

namespace TCX.API.Common.Shared
{
    public static class WinCustomerHelper
    {
        public static WinCustomerResponse GetResponseWincustomer(string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                return JsonConvert.DeserializeObject<WinCustomerResponse>(response);
            }
            else
            {
                return null;
            }
        }
    }
}
