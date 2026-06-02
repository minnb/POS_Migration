using System;
using System.Collections.Generic;

namespace TCX.API.Common.Models
{
    public class PLH_CheckCouponModel
    {
        public string storeNo { get; set; }
        public string posID { get; set; }
        public string userPOS { get; set; }

        public bool isWeb = false;
        public List<SeriNoCouponRequest> listSeriNo { get; set; }
    }
    public class SeriNoCouponRequest
    {
        public string seriNo { get; set; }
        public int quantity { get; set; }
        public bool? isEmployee { get; set; }
    }

    public class PLH_CheckCouponResponse
    {
        public string ItemNo { get; set; }
        public string SeriNo { get; set; }
        public string StatusCoupon { get; set; }
        public string DescCoupon { get; set; }
        public Nullable<int> Quantity { get; set; }
        public bool IsEmployee { get; set; }
    }
}
