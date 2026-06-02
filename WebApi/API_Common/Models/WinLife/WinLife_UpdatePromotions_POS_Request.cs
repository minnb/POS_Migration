using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.WinLife
{
    public class WinLife_UpdatePromotions_POS_Request
    {
        public string csn { get; set; }//Ma CSN
        [Required]
        public string action { get; set; }//Ghi nhận [A] hoặc xoá [I] thông tin ghi nhận áp dụng CTKM
        [Required]
        public string storeCode { get; set; }
        [Required]
        public string posCode { get; set; }
        public string phoneNumber { get; set; }//Mã xác định thông tin khách hàng, hiện tại là SĐT
        [Required]
        public string orderNo { get; set; }
        public List<AppliedCodeRequest> appliedCodes { get; set; }
    }
    public class WinLife_UpdatePromotions_CX_Request
    {
        public string csn { get; set; }
        // public string winCode { get; set; }//Ma CTKM
        public string action { get; set; }//Ghi nhận [A] hoặc xoá [I] thông tin ghi nhận áp dụng CTKM
        public string merchantId { get; set; }
        public string storeNo { get; set; }
        public string posNo { get; set; }
        // public string codeValue { get; set; }//Mã xác định thông tin khách hàng, hiện tại là SĐT
        public string billNo { get; set; }
        public List<AppliedCodeRequest> appliedCodes { get; set; }
    }
    public class AppliedCodeRequest
    {
        public string winCode { get; set; }
        public int quantity { get; set; }
    }
}
