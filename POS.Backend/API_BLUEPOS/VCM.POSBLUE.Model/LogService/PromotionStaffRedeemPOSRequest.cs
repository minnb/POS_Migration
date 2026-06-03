using System;

namespace VCM.POSBLUE.Model.LogService
{
    public class PromotionStaffRedeemPOSRequest
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string StaffCode { get; set; }
        public string StaffName { get; set; }
        public DateTime RedeemDate { get; set; }
        public float Amount { get; set; }
        public string OrderNo { get; set; }
    }

    public class PromotionStaffRedeemPartnerRequest
    {
        public string StaffCode { get; set; }
        public string StaffName { get; set; }
        public DateTime RedeemDate { get; set; }
        public float Amount { get; set; }
        public string RefCode { get; set; }

    }
}
