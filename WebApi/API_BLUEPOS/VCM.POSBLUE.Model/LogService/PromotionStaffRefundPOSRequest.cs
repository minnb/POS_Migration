using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.LogService
{
   public class PromotionStaffRefundPOSRequest
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string StaffCode { get; set; }
        public string StaffName { get; set; }
        public DateTime RefundDate { get; set; }
        public float Amount { get; set; }
        public string OrderNo { get; set; }
    }
}
