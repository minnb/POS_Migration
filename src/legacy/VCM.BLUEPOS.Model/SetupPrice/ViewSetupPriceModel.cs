using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.SetupPromotion;

namespace VCM.BLUEPOS.Model.SetupPrice
{
    public class ViewSetupPriceModel
    {
        public List<StorePriceGroupModel> listStorePriceGroup { get; set; }
        public List<SalesTypeModel> ListSalesType { get; set; }

        public List<PriceGroupModel> listPriceGroup { get; set; }
    }
}
