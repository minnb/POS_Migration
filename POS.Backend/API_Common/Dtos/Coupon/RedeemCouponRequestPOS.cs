using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Coupon
{
    public class RedeemCouponRequestPOS
    {
        public List<SerialCouponModel> ListSeriNo { get; set; }
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string OrderNo { get; set; }
        public string UserPOS { get; set; }

    }
    public class SerialCouponModel
    {
        public string SeriNo { get; set; }
        public long QuantityRedeem { get; set; }
        public string Status { get; set; }
    }
}
