using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.CrownX
{
    public class CardCX_AssignCardPOSModel
    {
        public string clubCode { get; set; }//Chương trình loyalty, default = PLH
        public string codeValue { get; set; }//Là phoneNo hoặc barcode được nhập vào máy POS
        public string cardId { get; set; }//Mã thẻ được gán cho khách hàng, là null trong trường hợp actionType = 'RG'
                                          //và bắt buộc trong những trường hợp còn lại
        public string otp { get; set; }
        public string actionType { get; set; }//Đăng ký thành viên" [RG]
                                              //"Đổi thẻ có phí" [CC]
                                              //"Làm thẻ" [IC]
                                              //"Đổi thẻ miễn phí" [FC]

        public string merchantId { get; set; }//Mã đối tác nơi gán thẻ, default = PLH với Odoo POS
        public string storeCode { get; set; }
        public string posCode { get; set; }       
        public CardCX_BillInfo billInfo { get; set; }
    }

    public class CardCX_AssignCardRequestCXModel
    {
        public string clubCode { get; set; }//Chương trình loyalty, default = PLH
        public string codeValue { get; set; }//Là phoneNo hoặc barcode được nhập vào máy POS
        public string cardId { get; set; }//Mã thẻ được gán cho khách hàng, là null trong trường hợp actionType = 'RG'
                                          //và bắt buộc trong những trường hợp còn lại
        public string otp { get; set; }
        public string actionType { get; set; }//Đăng ký thành viên" [RG]
                                              //"Đổi thẻ có phí" [CC]
                                              //"Làm thẻ" [IC]
                                              //"Đổi thẻ miễn phí" [FC]

        public string merchantId { get; set; }//Mã đối tác nơi gán thẻ, default = PLH với Odoo POS
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public long transactionTime { get; set; }//Thời gian thực hiện gán thẻ (UTC)
        public string referId { get; set; }//Mã duy nhất cho mỗi request, để track log
        public CardCX_BillInfo billInfo { get; set; }
    }
}
