using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupCoupon
{
    public class StoreGroupCodeModel
    {
        public string StoreNo { get; set; }
        public string GroupCode { get; set; }
        public string Pkey { get; set; }
        public Nullable<long> Counter { get; set; }
        public Nullable<bool> Status { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}
