using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
    public class POSEOD_APIModel
    {
        public string POSTerminal { get; set; }
        public string StoreNo { get; set; }
        public DateTime BussinessDate { get; set; }
        public int TotalSale { get; set; }
    }
}
