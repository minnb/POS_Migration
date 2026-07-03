using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class PosTerminalMissOrderModel
    {
        public string StoreNo { get; set; }
        public string PosTerminalID { get; set; }
        public string IPAddress { get; set; }
        public int TotalSale { get; set; }
        public List<TransHeaderPOSModel> ListOrderCentral { get; set; }
    }



}
