using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Capillary.Customer;

namespace TCX.API.Common.Dtos.Capillary.Point
{
    public class PointTopUpCapillaryResponse
    {
        public PointTopUpDataCapResponse Response { get; set; }
    }
    public class PointTopUpDataCapResponse
    {
        public StatusCodeCapillary Status { get; set; }
        public TopUpDataRootCapResponse Requests { get; set; }
    }
    public class TopUpDataRootCapResponse
    {
        public List<TopUpDataCapResponse> Request { get; set; }
    }
    public class TopUpDataCapResponse
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public string Requested_on { get; set; }
        public string Type { get; set; }
        public string Base_type { get; set; }
        public string Reason { get; set; }
        public string Comments { get; set; }
        public DataCustomerCapillaryResponse Customer { get; set; }
        public ItemStatusCapillary Item_status  { get; set; }
    }
}
