namespace VCM.POSBLUE.Model.LogService
{
    public class PromotionStaffResponse
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public int TotalItems { get; set; }
        public string TechMsg { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }

    }

    public class ForceCheckResponse
    {
        public string StaffCode { get; set; }
        public string ApplyFrom { get; set; }
        public string ValidBefore { get; set; }
        public float InitQuota { get; set; }
        public float RemainQuota { get; set; }
    }
}
