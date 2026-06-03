using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Point
{
    public class RevertTopUpPointsCapRequest
    {
        public string PointsToBeAdjusted { get; set; }
        public int ProgramId { get; set; }
        public string PointCategoryTypes { get; set; }
        public string ReasonOfReturn { get; set; }

    }
    public class PointReverseCapRequest
    {
        public string RedemptionId { get; set; }
        public long PointsToBeReversed { get; set; }
        public IdentifiersCapillary Identifier { get; set; }

    }
    public class RevertTopUpPointsPOSRequest
    {
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string MemberCard { get; set; }
        [Required]
        public string OrderNo { get; set; }
        public long PointsToBeAdjusted { get; set; }
        public string ReasonOfReturn { get; set; }
    }
    public class RevertPointsRedeemRequest 
    {
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string MemberCard { get; set; }
        [Required]
        public string OrderNo { get; set; }
        [Required]
        public string RedeemId { get; set; }
        public long Points  { get; set; }
        [Required]
        public string Reason { get; set; }
    }
    public class PointReversePOSRequest
    {
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string MemberCard { get; set; }
        [Required]
        public string RedemptionId { get; set; }
        [Required]
        public int PointsToBeReversed { get; set; }
        public string OrderNo { get; set; }
    }
    public class RevertPointsRedeemResponse
    {
        public string CustomerId { get; set; }
        public string ReversalId { get; set; }
        public string RedemptionId { get; set; }
        public decimal PointsToBeReversed { get; set; }
        public decimal PointsReversed { get; set; }
    }
}
