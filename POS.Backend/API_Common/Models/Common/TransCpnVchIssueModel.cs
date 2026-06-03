using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
   public class TransCpnVchIssueModel
    {
        public string ArticleNo { get; set; }
        public string VoucherType { get; set; }
        public int? MaxQtyUse { get; set; }
        public int? QtyUse { get; set; }
    }
}
