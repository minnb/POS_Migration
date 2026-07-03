using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class PosTerminalModel
    {
        public string StoreNo { get; set; }
        public string PosTerminalID { get; set; }
        public string IPAddress { get; set; }
        public string Placement { get; set; }  // Loại POS
    }

    public class PosTerminalV2Model
    {
        public string No { get; set; }
        public string StoreNo { get; set; }
        public string Placement { get; set; }  // Loại POS
    }
}
