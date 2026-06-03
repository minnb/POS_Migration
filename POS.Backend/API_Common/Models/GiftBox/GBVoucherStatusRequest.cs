using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.GiftBox
{
    public class GBVoucherStatusRequest
    {
        public string authKey { get; set; }
        public string brandCode { get; set; }
        public string pinNo { get; set; }
        public string storeCode { get; set; }
    }
}
