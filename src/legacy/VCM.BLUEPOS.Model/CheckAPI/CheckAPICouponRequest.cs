using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.CheckAPI
{
    public class CheckAPICouponRequest
    {
        public string storeNo { get; set; }
        public string posID { get; set; }
        public string userPOS { get; set; }
        public bool isWeb { get; set; }
        public List<SeriNoCouponRequest> listSeriNo { get; set; }
    }
    public class SeriNoCouponRequest
    {
        public string seriNo { get; set; }
        public int quantity { get; set; }
    }
}
