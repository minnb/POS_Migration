using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class POS_InfoMemberRequest
    {
        public string CodeValue { get; set; }       
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
    }
}
