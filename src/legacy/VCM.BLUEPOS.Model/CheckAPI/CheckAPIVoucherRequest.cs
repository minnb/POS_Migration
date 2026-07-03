using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.CheckAPI
{
    public class CheckAPIVoucherRequest
    {
        public string partner { get; set; }
        public string storeNo { get; set; }
        public string posID { get; set; }
        public bool isVoucher { get; set; }
        public bool isWeb { get; set; }
        public List<SeriNoRequest> listSeriNo { get; set; }
    }
    public class SeriNoRequest
    {
        public string seriNo { get; set; }
        public string articleSAP { get; set; }
        public bool isEmployee { get; set; }
    }
}
