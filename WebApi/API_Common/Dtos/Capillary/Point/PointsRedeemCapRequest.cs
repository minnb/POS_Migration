using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Point
{
    public class PointsRedeemCapRequest
    {
        public RootPointRedeemCap Root { get; set; }
    }

    public class RootPointRedeemCap
    {
        public List<RedeemCap> Redeem { get; set; }
    }
    public class RedeemCap
    {
        public string Points_redeemed { get; set; }
        public string Transaction_number { get; set; }
        public CustRedeemCap Customer { get; set; }
    }
    public class CustRedeemCap
    {
        public string Mobile { get; set; }
    }

    public class PointRedeemPOSRequest
    {
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string OrderNo { get; set; }
        [Required]
        public string MemberCard { get; set; }
        public int PointRedeem { get; set; }
    }
}
