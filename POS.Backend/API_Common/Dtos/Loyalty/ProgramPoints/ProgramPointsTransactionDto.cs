using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos.Loyalty.ProgramPoints
{
    public class ProgramPointsTransactionDto
    {
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string MemberCard { get; set; }
        [Required]
        public string OrderNo { get; set; }
        [Required]
        public int TransactionType { get; set; }
        public string ReturnedOrderNo { get; set; }
        public List<ProgramPointsTransactionItem> Items { get; set; }
    }
    public class ProgramPointsTransactionItem
    {
        [Required]
        public int LineNo { get; set; }
        [Required]
        public int ProgramId { get; set; }
        [Required]
        public string ItemNo { get; set; }
        [Required]
        public string Uom { get; set; }
        public decimal UnitPrice { get; set; }
        [Required]
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal TotalPoint { get; set; }
    }
    public class ProgramPointsTransactionItemResponse
    {
        public int LineNo { get; set; }
        public int ProgramId { get; set; }
        public string ProgramType { get; set; }
        public string ItemNo { get; set; }
        public string Uom { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal TotalPoint { get; set; }
    }
    public class ProgramPointsTransactionSuccess
    {
        public string ProgramName { get; set; }
        public string ClubCode { get; set; }
        public decimal EarnPoint { get; set; }
        public decimal RedeemPoint { get; set; }
        public decimal AvailablePoints { get; set; }
        public List<ProgramPointsTransactionItemResponse> Items { get; set; }
    }
}
