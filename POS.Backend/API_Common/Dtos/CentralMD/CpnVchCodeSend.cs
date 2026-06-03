using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.CentralMD
{
    public class CpnVchCodeSendDto
    {
        public long ID { get; set; }
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public Nullable<System.DateTime> Date { get; set; }
        public string OrderNo { get; set; }
        public string ItemNo { get; set; }
        public string Code { get; set; }
        public string PhoneNumber { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}
