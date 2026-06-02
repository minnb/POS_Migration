using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class CouponValidTimeData
    {
        public string PosNo { get; set; }
        public string MaterialNo { get; set; }
        public string CouponCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
    public class Gift_CouponValidTime
    {
        public string PosNo { get; set; }
        public string MaterialNo { get; set; }
        public string CouponCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string MemberCard { get; set; }
        public string OrderNo { get; set; }
        public string GiftCode { get; set; }
        public bool IsUsed { get; set; }
        public string OrderNoUsed { get; set; }
        public bool IsSync { get; set; }
        public string Message { get; set; }
        public DateTime CrtDate { get; set; }
    }
}
