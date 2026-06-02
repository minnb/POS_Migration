using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.ROP
{
    public class GenOspQRLogin
    {
        [Required]
        public string StoreNo {get; set;}
        [Required]
        public string PosNo { get; set; }
    }
    public class GenOspQRLoginROP
    {
        public string Site { get; set; }
        public string PosTerminal { get; set; }
    }
}
