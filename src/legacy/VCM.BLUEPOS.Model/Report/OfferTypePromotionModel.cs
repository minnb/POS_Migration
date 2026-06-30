using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class OfferTypePromotionModel
    {
        public int ID { get; set; }
        public string OfferType1 { get; set; }
        public string OfferName { get; set; }
        public Nullable<bool> IsTotalBill { get; set; }
        public Nullable<bool> IsSetupBuy { get; set; }
        public Nullable<bool> IsSetupGet { get; set; }
        public Nullable<bool> IsVoucher { get; set; }
        public Nullable<bool> Enabled { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }


}
