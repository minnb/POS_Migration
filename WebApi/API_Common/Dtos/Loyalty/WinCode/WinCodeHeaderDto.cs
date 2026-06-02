using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.WinCode
{
    public class WinCodeHeaderDto
    {
        public Guid ID { get; set; }
        public string ProgramCode { get; set; }
        public string WinCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Quantity { get; set; }
        public bool Status { get; set; }
        public string DiscountType { get; set; }
        public string ApplyType { get; set; }
        public string Pkey { get; set; }
        public long Counter { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
