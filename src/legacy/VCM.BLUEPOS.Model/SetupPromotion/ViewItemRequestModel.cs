using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class ViewItemRequestModel
    {
        public string OfferNo { get; set; }
        public string LineNo { get; set; }
        public string OfferType { get; set; }
        public string Condition { get; set; }
        public string TabOffer { get; set; }
        public bool IsTotalBill { get; set; }
        public bool IsSetupGet { get; set; }
        public decimal TotalBill { get; set; }

    }
}
