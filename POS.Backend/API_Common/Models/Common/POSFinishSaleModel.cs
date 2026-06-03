using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
    public class POSFinishSaleModel
    {
        public string StaffCode { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public DateTime? BussinessDate { get; set; }
    }
}
