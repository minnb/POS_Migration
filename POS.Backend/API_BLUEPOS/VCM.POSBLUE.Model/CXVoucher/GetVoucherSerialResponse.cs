using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.CXVoucher
{
    public class GetVoucherSerialResponse
    {
        public string errorCode { get; set; }
        public string errorMessage { get; set; }
        public ResponseDataVoucher data { get; set; }
    }
    public class ResponseDataVoucher
    {
        public string phoneNo { get; set; }
        public string otp { get; set; }
        public List<DetailVoucher> voucherDetails { get; set; }
    }
    public class DetailVoucher
    {
        public string voucherType { get; set; }
        public string voucherSerial { get; set; }
    }
}
