using System;
using System.Collections.Generic;
using TCX.API.Common.Dtos.Capillary;
using TCX.API.Common.Dtos.Loyalty.MemberBusiness;
using TCX.API.Common.Dtos.Loyalty.ProgramPoints;

namespace VCM.POSBLUE.Model.VINID
{
    public class InfoMemberModel_PLH: InfoMemberModel
    {
        public string CustomerTier { get; set; }
        public bool IsGift { get; set; }
        public int StampsPoint { get; set; }
    }

    public class InfoMemberModel
    {
        public string CardNumber { get; set; }
        public string VirtualCard { get; set; }
        public string CMND { get; set; }
        public string MemberName { get; set; }
        public string Title { get; set; }
        public string PhoneNumber { get; set; }
        public string CardLevel { get; set; }
        public long? MemberPoint { get; set; }
        public long? TotalPoint { get; set; }
        public long? RedemptionValue { get; set; } //Redemption Value VND
        public int? CurrentRate { get; set; }
        public string MemberCSN { get; set; }
        public string OtherInfo { get; set; }
        public string QRCode { get; set; }
        public bool? ExtraPoint { get; set; }
        public bool IsRedeem { get; set; }
        public bool? IsOfflineVinID { get; set; }
        public bool IsShowMessage { get; set; }
        public string Status { get; set; }
        public string System { get; set; }
        public string ClubCode { get; set; }
        public string DateOfBirth { get; set; }
        public string Dob { get; set; }
        public bool? BirthdayGiftInd { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string ExternalId {get;set; }
        public string OtherStatus { get; set; }
        public string MemberType { get; set; }
        public string Source { get; set; }
        public List<ExtendedField> ExtendedFields { get; set; }
        public List<BLUEAvailablePromotion> AvailablePromotion { get; set; }
        public MemberBusinessData MemberBusiness { get; set; }
        public List<ProgramPointData> PointsSummaries { get; set; }
    }

    public class BLUEAvailablePromotion  //Danh sách các mã CTKM khách hang có thể sử dụng kèm số lượng tương ứng
    {
        public string WinCode { get; set; }//W001,W002,...
        public int? Quantity { get; set; }
        public string DiscountType { get; set; } //BILL or ITEM
        public string MerchantId { get; set; }//ALL or PLH
        public decimal InitQuota { get; set; } = 0;
        public decimal RemainQuota { get; set; } = 0;
        public string Serial { get; set; }
        public string PromotionCode { get; set; }
    }

    public class ProgramPointData
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; }
        public string CumulativeType { get; set; }
        public string ClubCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal PreviousPoint { get; set; }
        public decimal CommitPoints { get; set; }
        public decimal AvailablePoints { get; set; }
        public string ProgramType { get; set; }
        public List<ProgramPointsItemDto> Items { get; set; }
    }

    public class ExtendedField
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
