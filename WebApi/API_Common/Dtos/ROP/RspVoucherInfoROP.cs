using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Dtos.ROP
{
    public class RspVoucherInfoROP
    {
        public string SAPCode { get; set; }
        public string NameCode { get; set; }
        public string SerialNumber { get; set; }
        public string VoucherCode { get; set; }
        public DateTime ValidFrom { get; set; } = DateTime.Now;
        public DateTime ValidTo { get; set; } = DateTime.Now;
        public decimal VoucherValue { get; set; }
        public decimal VoucherPercent { get; set; }
        public string BonusBuy { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public string StorageSite { get; set; }
        public string VoucherTypeName { get; set; }
        public string CompanyCode { get; set; }
        public string SAPVoucherType { get; set; }
        public string SAPArticleType { get; set; }

    }
    public class RspVoucherInfoNullROP
    {
        public string SAPCode { get; set; }
        public string NameCode { get; set; }
        public string SerialNumber { get; set; }
        public string VoucherCode { get; set; }
        public DateTime? ValidFrom { get; set; } = DateTime.Now;
        public DateTime? ValidTo { get; set; } = DateTime.Now;
        public decimal VoucherValue { get; set; }
        public decimal VoucherPercent { get; set; }
        public string BonusBuy { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public string StorageSite { get; set; }
        public string VoucherTypeName { get; set; }
        public string CompanyCode { get; set; }
    }
}
