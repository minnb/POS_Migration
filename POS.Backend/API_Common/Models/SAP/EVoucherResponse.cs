using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.SAP
{
    public class EVoucherResponse
    {
        public string CSN { get; set; }
        public string SerialNo { get; set; }
        public string ArticleNo { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Return { get; set; }
        public string Message { get; set; }
    }
}
