using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.WinX
{
    public class ResponseDynamicVouchersWinX
    {
        public int Status { get; set; }
        public ResponseDataDynamicVoucherWinX Data { get; set; }
        public string Message { get; set; }
    }
    public class ResponseDataDynamicVoucherWinX
    {
        public List<DataDynamicVoucherWinX> Data { get; set; }
        public List<ResponseErrorDynamicVoucherWinX> Errors { get; set; }
    }
    public class ResponseErrorDynamicVoucherWinX
    {
        public string Dynamic_code { get; set; }
        public string Error { get; set; }
    }
    public class DataDynamicVoucherWinX
    {
        public string Dynamic_code { get; set; }
        public string Capillary_voucher_code { get; set; }
        public DateTime Valid_from { get; set; }
        public DateTime Valid_until { get; set; }
        public string Uuser_phone_number { get; set; }
        public string Capillary_user_id { get; set; }
        public string Series_id { get; set; }
    }
    public class RequestDynamicVouchersWinX
    {
        public List<string> Dynamic_codes { get; set; }
    }
}
