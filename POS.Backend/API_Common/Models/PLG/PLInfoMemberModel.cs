using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class PLInfoMemberModel
    {
        public string CardNumber { get; set; }
        public string CMND { get; set; }
        public string Gender { get; set; }
        public string MemberName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool? BirthdayGiftInd { get; set; }
        public string CardLevel { get; set; }
        public string CustomerTier { get; set; }
        public int? MemberPoint { get; set; }
        public int? TotalPoint { get; set; }
        public int? CurrentRate { get; set; }
        public string MemberCSN { get; set; }
        public string QRCode { get; set; }
        public bool IsRedeem { get; set; }
        public string Status { get; set; }
        public string ClubCode { get; set; }
        public bool IsGift { get; set; }
        public int StampsPoint { get; set; }
        public List<PLHAvailablePromotion> AvailablePromotion { get; set; }
    }
    public class MemberClub
    {
        public string CSN { get; set; }
        public string MerchantId { get; set; }
        public string ClubCode { get; set; }
    }
    public class PLHAvailablePromotion  //Danh sách các mã CTKM khách hang có thể sử dụng kèm số lượng tương ứng
    {
        public string WinCode { get; set; }//W001,W002,...
        public int? Quantity { get; set; }
        public string DiscountType { get; set; } //BILL or ITEM
        public string MerchantId { get; set; }//ALL or PLH
    }
}
