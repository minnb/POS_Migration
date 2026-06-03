using System;

namespace TCX.API.Common.Dtos
{
    public class CheckOfferStaffRemn : OfferStaffRemnDto
    {
        public string PosNo { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
    public class OfferStaffRemnDto
    {
        public string ClubCode { get; set; }
        public string Month { get; set; }
        public string PhoneNumber { get; set; }
        public string StaffCode { get; set; }
        public decimal InitQuota { get; set; }
        public decimal RemainQuota { get; set; }
        public DateTime CrtDate { get; set; }
        public DateTime ChgDate { get; set; }
    }
}
