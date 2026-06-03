using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
   public class KiosCheckOrderResponse
    {
        public string OrderNo { get; set; }
        public string PosNo { get; set; }
        public int StepProcess { get; set; }
        public string AppCode { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public string CSN { get; set; }
        public string MemberCardNo { get; set; }
    }
}
