using System;
using System.Collections.Generic;


namespace TCX.API.Common.Models
{
    public class CustomerEnquiryModel
    {
        public string id { get; set; }
        public string cardNo { get; set; }
        public string cardStatus { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string title { get; set; }
        public string fullName { get; set; }
        public string csn { get; set; }
        public DateTime? dateOfBirth { get; set; }
        public bool? birthdayGiftInd { get; set; }
        public string gender { get; set; }
        public string phoneNo { get; set; }
        public string barcode { get; set; }
        public string email { get; set; }
        public string nationalID { get; set; }
        public string provinceCode { get; set; }
        public string provinceName { get; set; }
        public string districtCode { get; set; }
        public string districtName { get; set; }
        public string wardCode { get; set; }
        public string wardName { get; set; }
        public string address { get; set; }
        public string customerTier { get; set; }
        public string customerTierDesc { get; set; }
        public bool? isBlue { get; set; }
        public bool? isRedeem { get; set; }
        public bool isGift { get; set; }
        public double? currencyRate { get; set; }
        public string paging { get; set; }
        public string errors { get; set; }
        public string dob { get; set; }
        public List<MemberBalance> memberBalance { get; set; }
        public List<AvailablePromotion> availablePromos { get; set; }
    }

    public class MemberBalance
    {
        public string poolId { get; set; }
        public string subPoolId { get; set; }
        public double? totalBalance { get; set; }
        public double? availableBalance { get; set; }
        public double? earliestExpiry { get; set; }
        public long? earliestExpiryDate { get; set; }
        public double? currencyRate { get; set; }
    }

    public class AvailablePromotion  //Danh sách các mã CTKM khách hang có thể sử dụng kèm số lượng tương ứng
    {
        public string winCode { get; set; }//W001,W002,...
        public int quantity { get; set; }
        public string discountType { get; set; } //BILL or ITEM
        public string merchantId { get; set; }//ALL or PLH
    }

}
