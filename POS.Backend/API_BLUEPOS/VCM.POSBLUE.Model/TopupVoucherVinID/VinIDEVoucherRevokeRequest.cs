using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.TopupVoucherVinID
{
    public class VinIDEVoucherRevokeRequest
    {
        public string merchant_code { get; set; }        
        public string serial_number { get; set; }
    }

    public class VinIDEVoucherRevokePosRequest
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string SerialNumber { get; set; }
    }
}
