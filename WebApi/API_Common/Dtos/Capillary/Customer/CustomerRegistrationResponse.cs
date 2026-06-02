using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Customer
{
    public class CustomerRegistrationResponse
    {
        public int CreatedId { get; set; }
        public List<object> Warnings { get; set; }
        public List<SideEffect> SideEffects { get; set; }
    }

    public class RefundTransationResponse
    {
        public int CreatedId { get; set; }
        public List<object> Warnings { get; set; }
        public List<object> SideEffects { get; set; }
        public List<object> Errors { get; set; }
    }
    public class SideEffect
    {
        public string EntityType { get; set; }
        public decimal RawAwardedPoints { get; set; }
        public long AwardedPoints { get; set; }
        public string Type { get; set; }
    }
    public class WarningsCAP
    {
        public bool Status { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
