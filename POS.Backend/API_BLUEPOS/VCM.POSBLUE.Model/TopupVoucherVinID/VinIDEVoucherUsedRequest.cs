using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.TopupVoucherVinID
{
    public class VinIDEVoucherUsedRequest
    {
        public string merchant_code { get; set; }
        public string store_code { get; set; }
        public string pos_code { get; set; }
        public string serial_number { get; set; }
        public string redeem_ref_id { get; set; }
        public string merchant_voucher_code { get; set; }
        public string merchant_reference_id { get; set; }
        public int merchant_staff_id { get; set; }
        public object extra_data { get; set; }
    }

    public class VinIDEVoucherUsedPosRequest
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }       
        public string UserPOS { get; set; }
        public string OrderNo { get; set; }
        public List<string> ListSerialNumber { get; set; }
    }

    public class EVoucherUsedPosRequest
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string UserPOS { get; set; }
        public string OrderNo { get; set; }
        public string SerialNumber { get; set; }
    }
}
