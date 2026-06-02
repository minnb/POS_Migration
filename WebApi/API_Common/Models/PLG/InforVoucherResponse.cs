using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class InforVoucherResponse
    {
        public string SeriNo { get; set; }
        public string ItemNo { get; set; }
        public string TypeVoucher { get; set; }
        public string StatusVoucher { get; set; }
        public string DescVoucher { get; set; }
        public Nullable<double> Value { get; set; }
        public bool IsEmployee { get; set; }

    }

    public class InforCouponResponse
    {
        public string ItemNo { get; set; }
        public string SeriNo { get; set; }
        public string StatusCoupon { get; set; }
        public string DescCoupon { get; set; }
        public Nullable<long> Quantity { get; set; }
        public bool IsEmployee { get; set; }

    }
}
