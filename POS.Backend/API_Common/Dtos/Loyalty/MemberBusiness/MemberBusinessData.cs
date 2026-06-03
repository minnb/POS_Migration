using System.Collections.Generic;

namespace TCX.API.Common.Dtos.Loyalty.MemberBusiness
{
    public class CheckMemberBusinessData
    {
        public bool IsChecked { get; set; }
        public string Description { get; set; }
        public MemberBusinessData MemberBusinessData { get; set; }
    }
    public class MemberBusinessData
    {
        public string Month { get; set; }
        public string CardLevel { get; set; }
        public string MemberCard { get; set; }
        public string MemberStore { get; set; }
        public bool IsDiscount { get; set; }
        public string Key { get; set; }
        public ExistsProcessing ExistsProcessing { get; set; }
        public List<MemberBusinessItemQuota> Items { get; set; }
    }
    public class MemberBusinessItemQuota
    {
        public string ItemNo { get; set; }
        public string Uom { get; set; }
        public decimal MaxValue { get; set; }
        public decimal UsedValue { get; set; }
        public decimal RemnValue { get; set; }
    }
    public class ExistsProcessing
    {
        public string PosNo { get; set; }
        public string KeyExists { get; set; }
    }
}
