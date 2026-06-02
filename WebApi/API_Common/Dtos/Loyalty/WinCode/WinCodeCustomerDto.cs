using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.WinCode
{
    public class WinCodeCustomerDto
    {
        public Guid ID { get; set; }
        public string WinCode { get; set; }
        public string MemberCard { get; set; }
        public string Csn { get; set; }
        public int QuantityRecieptedSum { get; set; }
        public int QuantityReciepted { get; set; }
        public string OrderNo { get; set; }
        public string StoreNo { get; set; }
        public string PosNo { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime TransDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

    }
}
