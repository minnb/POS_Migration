using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
   public class SetupBuyModel
    {
        public SetupPromotionHEADERModel Header { get; set; }
        public List<OfferBuyModel> ListOfferBuy { get; set; }
    }
}
