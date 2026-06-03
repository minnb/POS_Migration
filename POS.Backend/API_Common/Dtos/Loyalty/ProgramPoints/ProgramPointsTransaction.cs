using System;

namespace TCX.API.Common.Dtos.Loyalty.ProgramPoints
{
    public class ProgramPointsTransaction
    {
        public string StoreNo { get; set; }
        public string PosNo { get; set; }
        public string Type { get; set; }
        public int ProgramId { get; set; }
        public string ClubCode { get; set; }
        public string MemberCard { get; set; }
        public string OrderNo { get; set; }
        public string ReturnedOrderNo { get; set; }
        public int TransactionType { get; set; }
        public decimal TotalPoint { get; set; }
        public DateTime CrtDate { get; set; }
    }
}
