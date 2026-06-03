using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.WinCode
{
    public class WinCodeStoreDto
    {
        public Guid ID { get; set; }
        public string ProgramCode { get; set; }
        public string StoreNo { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string Pkey { get; set; }
        public long Counter { get; set; }

    }
}
