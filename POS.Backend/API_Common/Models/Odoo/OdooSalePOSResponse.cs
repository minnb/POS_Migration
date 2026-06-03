using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models.Odoo
{
    public class OdooSalePOSResponse
    {
        public string SeriNo { get; set; }
        public string Message { get; set; }
        public double? Amount { get; set; }
        public bool StatusVoucher { get; set; }
    }
}
