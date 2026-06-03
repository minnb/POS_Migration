using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Dtos.ROP
{
    public class InsertVoucherROP
    {
        public string SiteCode { get; set; }
        public string POSTerminal { get; set; }
        public string ArticleType { get; set; } // 3 loại: CP/VC/BNT
        public IList<InsertVoucherDataROP> Vouchers { get; set; }
    }

    public class InsertVoucherDataROP
    {
        public string VoucherCode { get; set; }
        public decimal VoucherValue { get; set; }
        public int VoucherPercent { get; set; }
        public string OrderNumber { get; set; }
        public string BonusBuy { get; set; }
        public string SAPCode { get; set; }
        public int StatusId { get; set; }
        public string ValidFrom { get; set; }
        public string ValidTo { get; set; }
    }
}
