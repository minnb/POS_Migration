using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.TopupVoucherVinID
{
    public class VinIDEVoucherRefundRequest
    {
        public List<string> serials { get; set; }
    }
    public class VinIDEVoucherRefundPOSRequest
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public List<string> ListserialNumber { get; set; }
    }
}
