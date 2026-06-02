using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class PLRevertCXPOSRequest
    {
        public string clubCode { get; set; }
        public string phoneNo { get; set; }
        public string cardNo { get; set; }
        public string invoiceNo { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
    }
    public class PLRevertCXRequest
    {
        public string clubCode { get; set; }//CVL//PLH
        public string merchantId { get; set; }//Mã đối tác quản lý cửa hàng thực hiện huỷ GD. VCM cho BluePOS và PLH cho OdooPOS
        public string phoneNo { get; set; }//
        public string cardNo { get; set; }
        public string invoiceNo { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public long transactionTime { get; set; }
    }
}
