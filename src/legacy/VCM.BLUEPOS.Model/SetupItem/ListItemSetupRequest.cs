using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.SetupPromotion;

namespace VCM.BLUEPOS.Model.SetupItem
{
    public class ListItemSetupRequest
    {
        public string ItemNoHeader { get; set; }
        public List<SalesTypeModel> ListSalesType { get; set; }
    }
}
