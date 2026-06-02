using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.CrownX
{
    public class CardCX_InfoCardPOSModel
    {
        public string clubCode { get; set; }//Chương trình loyalty, default = PLH
        public string codeValue { get; set; }//Là phoneNo hoặc barcode được nhập vào máy POS
        public string cardId { get; set; }//Là cardId trong trường hợp KH muốn đổi thẻ miễn phí
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public CardCX_BillInfo billInfo { get; set; }
    }

    public class CardCX_InfoCardModel
    {
        public string clubCode { get; set; }
        public string codeValue { get; set; }
        public string cardId { get; set; }//
        public CardCX_BillInfo billInfo { get; set; }
    }
    public class CardCX_BillInfo
    {
        public string invoiceNo { get; set; }
        public double? grossAmount { get; set; }
        public long? transactionDatetime { get; set; }
        public string merchantId { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
    }
}
