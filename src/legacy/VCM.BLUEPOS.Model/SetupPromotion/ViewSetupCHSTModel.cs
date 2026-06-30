using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.Store;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class ViewSetupCHSTModel
    {
        public string OfferNo { get; set; }
        public List<StorePartnerModel> ListStore {get;set;}
    }
}
