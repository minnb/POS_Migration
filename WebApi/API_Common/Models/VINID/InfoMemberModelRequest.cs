using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class InfoMemberModelRequest
    {
        public string NumberCard { get; set; }
        public string QRCode { get; set; }
        public string PosID { get; set; }
        public string StoreNo { get; set; }
    }
}
