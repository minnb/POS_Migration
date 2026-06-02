using System;

namespace TCX.API.Common.Dtos
{
    public class OfferStaffSetupDto
    {
        public string ClubCode { get; set; }
        public DateTime ApplyFrom { get; set; }
        public DateTime ValidBefore { get; set; }
        public bool IsMonth { get; set; }
        public bool Blocked { get; set; }
        public decimal InitQuota { get; set; }
        public string Description { get; set; }
    }
}
