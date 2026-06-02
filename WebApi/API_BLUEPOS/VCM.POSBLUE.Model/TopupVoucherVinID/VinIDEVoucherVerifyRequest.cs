using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.TopupVoucherVinID
{
    public class VinIDEVoucherVerifyRequest
    {
        public string store_code { get; set; }
        public string pos_code { get; set; }
        public string serial_number { get; set; }
        public string merchant_staff_id { get; set; }
    }

    public class VinIDEVoucherVerifyPOSRequest
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string SerialNumber { get; set; }
        public string UserPOS { get; set; }
    }
}
