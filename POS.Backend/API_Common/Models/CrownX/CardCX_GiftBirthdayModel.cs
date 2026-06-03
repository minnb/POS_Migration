using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.CrownX
{
    public class CardCX_GiftBirthdayModel
    {
        public string clubCode { get; set; }
        public string codeValue { get; set; }        
        public long txnDatetime { get; set; }
        public string invoiceNo { get; set; }
        public string giftType { get; set; }//CAKE-Bánh sinh nhật + "VOU20K" - loại voucher
        public string storeId { get; set; }
        public string storeName { get; set; }

    }
    public class CardCX_GiftBirthdayPOSModel
    {
        public string clubCode { get; set; }
        public string codeValue { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public string orderNo { get; set; }
        public string giftType { get; set; } // hiện tại POS chưa có, để rỗng
    }
}
