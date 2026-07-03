using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.SetupPromotion;

namespace VCM.BLUEPOS.Model.SetupPrice
{
    public class ViewSetupItemPriceModel
    {
        public string TypeSetup { get; set; }
        public string SalesType { get; set; }
        public string SalesTypeName { get; set; }
        public string saleGroupCode { get; set; }
        public string saleGroupName { get; set; }
        public string ChannelCode { get; set; }
        public string RegionCode { get; set; }
        public string Store { get; set; }
        public List<ItemModel> ListItem { get; set; }

        public string  QtyRow { get; set; }
        
        public bool IsImport { get; set; }
        public List<LoadSalesPriceModel> ListItemFromExcel { get; set; }
        public List<ImportResultSalePriceModel> ListItemResultFromExcel { get; set; }
    }
}
