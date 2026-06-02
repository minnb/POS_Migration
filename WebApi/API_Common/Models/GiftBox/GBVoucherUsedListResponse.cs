using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.GiftBox
{
    public class GBVoucherUsedListResponse
    {
        public string messageTrans { get; set; }
        public string referenceNumber { get; set; }
        public string resCodeTrans { get; set; }
        public List<PinNoListResponse> pinInfo { get; set; }
    }
    public class PinNoListResponse
    {
        public int cashBalance { get; set; }
        public string goodsId { get; set; }
        public string goodsName { get; set; }
        public string goodsType { get; set; }//Voucher product type (discount, cash discount, product voucher, voucher book)
        public int listPrice { get; set; }
        public string pinNo { get; set; }
        public string pinStatus { get; set; }//R: un used U: used part of voucher F: already used C:cancel
        public string posTrid { get; set; }
        public string supplyCode { get; set; }
        public int supplyPrice { get; set; }
    }
}
