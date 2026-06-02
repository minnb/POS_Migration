using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
   public class LogSaleKiosModel
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string RequestPOS { get; set; }
        public string RequestAPI { get; set; }
        public string ResponsePOS { get; set; }
        public string ResponseAPI { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}
