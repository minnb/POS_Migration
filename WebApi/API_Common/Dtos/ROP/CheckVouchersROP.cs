using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Dtos.ROP
{
    public class CheckVouchersROP
    {
        public string[] VoucherNumbers { get; set; }
        public string Site { get; set; }
        public string PosTerminal { get; set; }
        public string OrderNumber { get; set; }
    } 
}
