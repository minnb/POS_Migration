using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Point
{
    public class PointReverseCapResponse
    {
        public long OrgId { get; set; }
        public IdentifiersCapillary Identifier { get; set; }
        public long CustomerId { get; set; }
        public string ReversalId { get; set; }
        public string RedemptionId { get; set; }
        public decimal PointsToBeReversed { get; set; }
        public decimal PointsReversed { get; set; }
        public string Balance { get; set; } = string.Empty;
        public DataPointsReversedDetails PointsReversedDetails { get; set; }
        //public List<object> Warnings { get; set; }
    }

    public class DataPointsReversedDetails
    {
        public decimal Available { get; set; }
        public decimal Expired { get; set; }
    }
}
