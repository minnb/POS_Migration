using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class DataVoucherPartnerResponse
    {
        public string Status { get; set; }
        public string Code { get; set; }
        public int VoucherAmount { get; set; }
        public int Amount { get; set; }
        public string Msg { get; set; }
        public string Remark { get; set; }
        public bool IsApplySku { get; set; }
    }
}
