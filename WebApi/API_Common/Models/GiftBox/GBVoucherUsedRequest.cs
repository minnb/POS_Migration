using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.GiftBox
{
    public class GBVoucherUsedRequest
    {
        public string authKey { get; set; }
        public string brandCode { get; set; }
        public string posTrid { get; set; }
        public string pinNo { get; set; }
        public List<PinNo> pinNoList { get; set; }
        public string referenceNumber { get; set; }
        public string storeCode { get; set; }
        public int usePrice { get; set; }
        public int useQuantity { get; set; }
    }
    public class GBVoucherUsedListRequest
    {
        public string authKey { get; set; }
        public string brandCode { get; set; }
        public List<PinNo> pinNoList { get; set; }
        public string referenceNumber { get; set; }
        public string storeCode { get; set; }
        
    }
    public class PinNo
    {
        public string pinNo { get; set; }
        public int usePrice { get; set; }

    }
}
