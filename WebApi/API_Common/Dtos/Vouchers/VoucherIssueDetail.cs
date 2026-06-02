using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Dtos.VoucherSAP
{
    public class VoucherIssueDetail
    {
        public string Status { get; set; }
        public string PhoneNumber { get; set; }
        public string VoucherNumber { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal Value { get; set; }
        public string FromDate { get; set; }
        public string ExpiryDate { get; set; }
        public string BonusBuy { get; set; }
        public string ArticleType { get; set; }
        public string ActicleNo { get; set; }
        public DateTime CrtDate { get; set; }
        public Guid RefId { get; set; }
        public string RequestId { get; set; }
        public Guid Id { get; set; }
        public string OrderNo { get; set; }
        public string PosUsed { get; set; }
        public Nullable<DateTime> UsedTime { get; set; }
    }
}
