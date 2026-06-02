using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class VoucherCardLogModel
    {
        public long ID { get; set; }
        public string Partner { get; set; }
        public string SeriNo { get; set; }
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public Nullable<double> Value { get; set; }
        public Nullable<bool> Status { get; set; }
        public string FunctionName { get; set; }
        public string PosRequest { get; set; }
        public string PosResponse { get; set; }
        public string PartnerRequest { get; set; }
        public string PartnerResponse { get; set; }
        public Nullable<int> TimeResponse { get; set; }
        public Nullable<int> StatusCode { get; set; }
        public string MsgError { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string UserName { get; set; }
    }
}
