using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.CXVoucher
{
    public class UpdateVoucherStatusResponse
    {
        public string errorCode { get; set; }
        public string errorMessage { get; set; }
        public ResponseDataUpdateVoucherStatusData data { get; set; }
    }
    public class ResponseDataUpdateVoucherStatusData
    {
        public List<ResponseDataUpdateVoucherStatusCodeDetail> voucherErrorDetails { get; set; }
    }
    public class ResponseDataUpdateVoucherStatusCodeDetail
    {
        public string voucherType { get; set; }
        public string voucherSerial { get; set; }
        public string message { get; set; }
    }
}
