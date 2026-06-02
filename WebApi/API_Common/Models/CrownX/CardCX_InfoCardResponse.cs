using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.CrownX
{
    public class CardCX_InfoCardResponse
    {
        public InfoCardData data { get; set; }        
    }
    public class CardCX_InfoCardPOSResponse
    {
        public string clubCode { get; set; }
        public CardCX_BillInfo billInfo { get; set; }
        public bool otpInd { get; set; }//POS cần thực hiện call Get OTP API để xác nhận khách hàng (true) hoặc không (false)(Là true nếu KH được xác nhận = số điện thoại, còn lại là false)
        public bool registerInd { get; set; }//Bật tính năng "Đăng ký thành viên" (true) hoặc không (false)(Là true nếu thông tin nhập vào có định dạng SDT và số điện thoại chưa được gán với thành viên PLH nào trên hệ thống, còn lại là false)
        public bool issueCardInd { get; set; }//Bật tính năng "Làm thẻ" (true) hoặc không (false)(Là true nếu khách hàng đã là thành viên PLH nhưng chưa từng làm thẻ, còn lại là false)
        public bool changeCardInd { get; set; }//Bật tính năng "Đổi thẻ" (true) hoặc (false). Việc tính năng đổi thẻ là miễn phí hay có phí phụ thuộc vào giá trị trường "fee" (Là true nếu khách hàng đã là thành viên PLH và đã từng làm thẻ, còn lại là false)
        public double fee { get; set; }//Không xét đến nếu changeCardInd = false
                                       //Nếu fee = 0 là tính năng đổi thẻ miễn phí, nếu fee > 0 là tính năng đổi thẻ có phí và giá trị của fee là mức phí khách hàng cần trả khi đổi thẻ
        public string customerName { get; set; }
        public string customerPhone { get; set; }
        public string accumulatedAmount { get; set; }//Điểm tích luỹ xét hạng của khách hàng
        public string qualifyDate { get; set; }//Này xét hạng để kết thúc chu kỳ hiện tại
        public List<InfoCardHistory> cardInfo { get; set; }
    }
    public class InfoCardData
    {
        public string clubCode { get; set; }
        public bool otpInd { get; set; }//POS cần thực hiện call Get OTP API để xác nhận khách hàng (true) hoặc không (false)(Là true nếu KH được xác nhận = số điện thoại, còn lại là false)
        public bool registerInd { get; set; }//Bật tính năng "Đăng ký thành viên" (true) hoặc không (false)(Là true nếu thông tin nhập vào có định dạng SDT và số điện thoại chưa được gán với thành viên PLH nào trên hệ thống, còn lại là false)
        public bool issueCardInd { get; set; }//Bật tính năng "Làm thẻ" (true) hoặc không (false)(Là true nếu khách hàng đã là thành viên PLH nhưng chưa từng làm thẻ, còn lại là false)
        public bool changeCardInd { get; set; }//Bật tính năng "Đổi thẻ" (true) hoặc (false). Việc tính năng đổi thẻ là miễn phí hay có phí phụ thuộc vào giá trị trường "fee" (Là true nếu khách hàng đã là thành viên PLH và đã từng làm thẻ, còn lại là false)
        public double fee { get; set; }//Không xét đến nếu changeCardInd = false
                                       //Nếu fee = 0 là tính năng đổi thẻ miễn phí, nếu fee > 0 là tính năng đổi thẻ có phí và giá trị của fee là mức phí khách hàng cần trả khi đổi thẻ

        public string note { get; set; }
        public string customerName { get; set; }
        public string customerPhone { get; set; }
        public string accumulatedAmount { get; set; }//Điểm tích luỹ xét hạng của khách hàng
        public string qualifyDate { get; set; }//Này xét hạng để kết thúc chu kỳ hiện tại
        public List<InfoCardHistory> cardInfo { get; set; }
        public CardCX_BillInfo billInfo { get; set; }
    }
    public class InfoCardHistory
    {
        public string cardId { get; set; }
        public string issuedDatetime { get; set; }
        public string cardStatus { get; set; }
        public string updatedDate { get; set; }
    }
}
