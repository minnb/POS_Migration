using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Const
{
    public static class HttpStatusConst
    {
        public static List<int> HttpSatusServerError = new List<int> { 500, 501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511, 530 };
    }
    public class ConstStatus
    {
        public const string N = "N";
        public const string A = "A";
        public const string R = "R";
        public const string E = "E";
        public const string D = "D";
        public const string L = "L";
        public const string I = "I";

        //public ConstStatus();
    }
}
