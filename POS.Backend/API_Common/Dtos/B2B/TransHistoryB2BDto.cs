using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos
{
    public class TransHistoryB2BDto
    {
        public  Guid ID { get; set; }
        public string OrderNo { get; set; }
        public string ProcessCode { get; set; }
        public string ProcessDescription { get; set; }
        public string ActionName { get; set; }
        public string OriginalLineJson { get; set; }
        public string OriginalHeaderJson { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateUser { get; set; }
    }
}
