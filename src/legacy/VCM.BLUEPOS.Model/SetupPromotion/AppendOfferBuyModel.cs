using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
   public class AppendOfferBuyModel
    {
        public string OfferNo { get; set; }
        public string OfferType { get; set; }
        public string ConditonBuy { get; set; }
        public bool IsTottalBill { get; set; }
        public bool IsSetupGet { get; set; }
        public OfferBuyModel GetOfferBuy { get; set; }
    }
}
