using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Models
{
    public class PLSaleCXRequestModel
    {
        [Required]
        public string cardNumber { get; set; }
        public string merchantId { get; set; }
        public string clubCode { get; set; }
        [Required]
        public string storeNo { get; set; }
        [Required]
        public string posID { get; set; }
        public string posTerminal { get; set; }
        [Required]
        public string orderNo { get; set; }
        public double awardAmount { get; set; }//Tổng tiền tích
        public double orderAmount { get; set; }//Tổng đơn hàng
        public int spendPoints { get; set; }//Điểm tiêu
        public int redeemStampsPoints { get; set; } //sử dụng điểm tích tem đổi LY
        public string referenceInvoice { get; set; }//Số hoá đơn tại POS để tham chiếu khi gửi 2 GD tích/tiêu điểm trên 1 GD bán hàng
    }

    public class PLSalePOSV2RequestModel
    {
        public string clubCode { get;set; }
        public string cardNumber { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public string orderNo { get; set; }
        public double orderAmount { get; set; }//Tổng đơn hàng
        public int spendPoints { get; set; }//Điểm tiêu
        public string referenceInvoice { get; set; }//Số hoá đơn tại POS để tham chiếu khi gửi 2 GD tích/tiêu điểm trên 1 GD bán hàng
        public List<amountToEarn> amountToEarn { get; set; }

    }

    public class PLSaleCXV2RequestModel
    {
        public string clubCode { get; set; }//Tên chương trình WIN/PLH
        public string merchantId { get; set; }//VCM/PLH/DWN
        public string cardNo { get; set; }
        public string phoneNo { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public string invoiceNo { get; set; }
        //public double awardAmount { get; set; }//Tổng tiền tích
        public double totalBillAmount { get; set; }//Tổng đơn hàng
        public int spendPoints { get; set; }//Điểm tiêu
        public long transactionTime { get; set; }
        // public string referenceInvoice { get; set; }//Số hoá đơn tại POS để tham chiếu khi gửi 2 GD tích/tiêu điểm trên 1 GD bán hàng
        public List<amountToEarn> amountToEarn { get; set; }////Mã đối tác quản lý chương trình Loyalty PLH/VCM/DWN

    }
    public class amountToEarn//Tich diem
    {
        public string loyaltyMerchantId { get; set; }////Mã đối tác quản lý chương trình Loyalty PLH/VCM/DWN
        public double amount { get; set; }//Giá trị tính điểm thưởng theo chương trình Loyalty tương ứng
    }

}

