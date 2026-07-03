using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class ItemByComboLineModel
    {
        public string No { get; set; }
        public string Description { get; set; }
        public string UOM { get; set; }
        public string GroupCode { get; set; }
    }

    public class PriceSalesModel
    {
        public string ItemNo { get; set; }
        public string SalesCode { get; set; }
        public double? UnitPrice { get; set; }
        public string UnitOfMeasure { get; set; }
        public string SalesType { get; set; }
    }
}
