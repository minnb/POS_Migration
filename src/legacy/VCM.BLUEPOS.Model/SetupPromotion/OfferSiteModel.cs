using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class OfferSiteModel
    {
        public string OfferNo { get; set; }
        public string PriceGroupCode { get; set; }
        public string GroupName { get; set; }
        public string StoreNo { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
    }
}
