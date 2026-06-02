using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.CXVoucher
{
    public class UpdateVoucherStatusPosRequest
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public List<VoucherSerialPosRequest> ListSerial { get; set; }
    }
    public class VoucherSerialPosRequest
    {
        public string VoucherSerial { get; set; }
    }

    public class UpdateVoucherStatusCXRequest
    {  
        public VoucherSerialDataCXRequest data { get; set; }
    }
    public class VoucherSerialDataCXRequest
    {
        public List<VoucherSerialCXRequest> voucherDetails { get; set; }
    }
    public class VoucherSerialCXRequest
    {
        public string voucherStatus { get; set; }
        public string voucherSerial { get; set; }
    }
}
