using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Coupons
{
    public class ReActivateCouponsRequest
    {
        public List<string> RedemptionIds { get; set; }
    }
    public class ReActivateCouponsResponse
    {
        public List<object> Warnings { get; set; }
        public List<ErrorCapillary> Errors { get; set; }
        public bool Success { get; set; }
    }
}
