using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.CrownX
{
    public class CXUpdateVoucherPOSRequest
    {
        public string storeNo { get; set; }
        public string posID { get; set; }       
        public List<CXVoucherListPOSRequest> voucherList { get; set; }
    }
    public class CXUpdateVoucherRequest
    {
        public string merchantId { get; set; }
        public string channelId { get; set; }
        public List<CXVoucherListRequest> voucherList { get; set; }
    }
    public class CXVoucherListRequest
    {
        public string voucherSerial { get; set; }
        public string status { get; set; }
        public string updatedDate { get; set; }
    }
    public class CXVoucherListPOSRequest
    {
        public string voucherSerial { get; set; }       
    }
}
