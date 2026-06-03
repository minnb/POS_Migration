using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.SAP
{
    public class CreateVoucherModel
    {
        [Required]
        public string VoucherNumber { get; set; }
        public decimal Value { get; set; }
        [Required]
        public string From_Date { get; set; }
        [Required]
        public string Expiry_Date { get; set; }
        [Required]
        public string SiteCode { get; set; }
        public string BonusBuy { get; set; }
        public string Article_No { get; set; }
        [Required]
        public string POSTerminal { get; set; } = string.Empty;
        public string OrderNo { get; set; }
    }
}
