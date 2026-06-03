using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.GiftBox
{
    public class GBVoucherUsedResponse
    {
        public string cashBalance { get; set; }
        public string expiryDate { get; set; }
        public string goodsId { get; set; }
        public string goodsName { get; set; }
        public string goodsType { get; set; }//Voucher product type (discount, cash discount, product voucher, voucher book)
        public string listPrice { get; set; }
        public string message { get; set; }
        public string pinNo { get; set; }
        public string pinStatus { get; set; }//R: un used U: used part of voucher F: already used C:cancel
        public string posTrid { get; set; }
        public int remainQuantity { get; set; }
        public string resCode { get; set; }
        public string supplyCode { get; set; }
    }   
}
