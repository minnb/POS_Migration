
namespace TCX.API.Common.Dtos.Loyalty.ProgramPoints
{
    public class ProgramPointsRemnItem
    {
        public string ClubCode { get; set; }
        public string StoreNo { get; set; }
        public string ItemNo { get; set; }
        public string Uom { get; set; }
        public decimal LimitedQty { get; set; }
        public decimal UsedQty { get; set; }
        public int Rate { get; set; }
    }
}
