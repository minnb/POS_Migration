using System.Collections.Generic;

namespace TCX.API.Common.Models
{
    public class PLCheckVoucherRequest
    {
        public string partner { get; set; }
        public string storeNo { get; set; }
        public string posID { get; set; }
        public bool isVoucher { get; set; }
        public List<SeriNoRequest> listSeriNo { get; set; }

        public bool isWeb = false;
    }
    public class SeriNoRequest
    {
        public string seriNo { get; set; }
        public string articleSAP { get; set; }
        public bool isEmployee { get; set; }
    }
}
