using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class TransHeaderPOSModel
    {
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string OrderNo { get; set; }
    }

    public class TransHeaderCentralModel
    {
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string OrderNo { get; set; }
    }


}
