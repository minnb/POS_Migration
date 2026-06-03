using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Point
{
    public class PointRedeemableCapResponse
    {
        public PointRedeemableDataCapResponse Response { get; set; }
    }
    public class PointRedeemableDataCapResponse
    {
        public StatusCodeCapillary Status { get; set; }
        public PointsRedeemableCap Points { get; set; }
    }

    public class PointsRedeemableCap
    {
        public DataRedeemableCap Redeemable { get; set; }
    }
    public class DataRedeemableCap
    {
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string External_id { get; set; }
        public int Points { get; set; }
        public string Is_redeemable { get; set; }
        public int Points_redeem_value { get; set; }
        public int Points_redeem_local_value { get; set; }
        public string Input_type { get; set; }
        public string Points_currency_ratio { get; set; }
        public ItemStatusCapillary Item_status { get; set; }
    }
}
