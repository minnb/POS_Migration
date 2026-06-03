using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Capillary.Customer;

namespace TCX.API.Common.Dtos.Capillary.Point
{
    public class PointRedeemCapResponse
    {
        public PointRedeemDataCapResponse Response { get; set; }
    }
    public class PointRedeemDataCapResponse
    {
        public StatusCodeCapillary Status { get; set; }
        public PointsDataCapResponses Responses { get; set; }
    }
    public class PointsDataCapResponses
    {
        public PointsDataCap Points { get; set; }
    }
    public class PointsDataCap
    {
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string External_id { get; set; }
        public string User_id { get; set; }
        public string Redemption_id { get; set; }
        public string Points_redeemed { get; set; }
        public string Redemption_purpose { get; set; }
        public string Redeemed_value { get; set; }
        public string Redeemed_local_value { get; set; }
        public string Balance { get; set; }
        public SideEffectRedeemCap Side_effects { get; set; }
        public ItemStatusCapillary Item_status { get; set; }

    }

    public class SideEffectRedeemCap
    {
        public List<SideEffectRedeemDataCap> Effect { get; set; }
    }
    public class SideEffectRedeemDataCap
    {
        public long Id { get; set; }
        public string Case_value { get; set; }
        public decimal Num_points { get; set; }
        //public int Currency_value { get; set; }
        public string Validation_code { get; set; }
        public string Points_redemption_summary_id { get; set; }
        public string Redeemed_on_bill_number { get; set; }
        public int Redeemed_on_bill_id { get; set; }
        public string Type { get; set; }
    }
}
