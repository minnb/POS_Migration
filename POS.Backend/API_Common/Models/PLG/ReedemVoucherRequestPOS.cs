using System.Collections.Generic;

namespace TCX.API.Common.Models
{
    public class ReedemVoucherRequestPOS
    {
        public string partner { get; set; }
        public List<SeriNo> listSeriNo { get; set; }
        public string orderNo { get; set; }
        public string storeNo { get; set; }
        public string posID { get; set; }
        public string staffCode { get; set; }
        public double totalBill { get; set; }
    }
    public class SeriNo
    {
        public string seriNo { get; set; }
        public bool isVoucher { get; set; }
        public double value { get; set; }
    }
}
