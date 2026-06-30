using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class GetBBModel
    {
        public string OfferNo { get; set; }
        public string OfferType { get; set; }
        public string Condition { get; set; }
        public bool IsTotalBill { get; set; }
        public bool IsSetupBuy { get; set; }
        public bool IsSetupGet { get; set; }
        public string BuyLinkCat { get; set; }
        public string CheckMinValue { get; set; }
        public string TotalMinValue { get; set; }

        public OfferHeaderModel GetOfferHearder { get; set; }
        public List<OfferBuyModel> GetOfferBuy { get; set; }
        public List<OfferGetModel> GetOfferGet { get; set; }
    }
}
