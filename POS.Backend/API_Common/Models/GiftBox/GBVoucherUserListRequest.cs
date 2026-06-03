using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.GiftBox
{
   public class GBVoucherUserListRequest
    {
        public string authKey { get; set; }
        public string brandCode { get; set; }
        public List<PinNoList> pinNoList { get; set; }
        public string referenceNumber { get; set; }
        public string storeCode { get; set; }
       
    }
    public class PinNoList
    {
        public string pinNo { get; set; }
        public int usePrice { get; set; }
    }
}
