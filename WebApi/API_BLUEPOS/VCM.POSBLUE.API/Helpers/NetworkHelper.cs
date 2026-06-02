using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.API.Helpers
{
    public static class NetworkHelper
    {
        public static string GetHostName()
        {
            try
            {
                return System.Net.Dns.GetHostName() ?? "BLUEPOS";
            }
            catch
            {
                return "BLUEPOS";
            }
        }
    }
}
