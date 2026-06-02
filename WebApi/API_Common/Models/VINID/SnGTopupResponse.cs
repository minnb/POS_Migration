using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class SnGTopupResponse
    {
        public SnGTopupMeta meta { get; set; }
    }
    public class SnGTopupMeta
    {
        public int code { get; set; }
        public string message { get; set; }
    }
}
