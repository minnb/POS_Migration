using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Odoo
{
   public class ResvCreateResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string ResNum { get; set; }
    }
    public class ResvCreatePOSResponse
    {
        public string StatusSAP { get; set; }
        public string Message { get; set; }
        public string ResNum { get; set; }
    }
}
