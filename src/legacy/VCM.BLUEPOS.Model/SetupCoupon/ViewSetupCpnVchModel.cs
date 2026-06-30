using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.SetupItem;
using VCM.BLUEPOS.Model.SetupPromotion;

namespace VCM.BLUEPOS.Model.SetupCoupon
{
   public class ViewSetupCpnVchModel
    {
        public string ItemNo { get; set; }
        public CpnVchBOMHeaderModel CpnVchHeader { get; set; }
        public List<UnitOfMeasureModel> ListUOM { get; set; }       
        public List<DiscountTypeModel> ListDiscountType { get; set; }
        public List<TypeCouponModel> ListType { get; set; }
        public List<SalesTypeModel> ListSalesType { get; set; }
        public List<StoreGroupCodeModel> ListStoreGroup { get; set; }
        public List<CpnItemModel> ListItemLine { get; set; }
        public List<ArticleTypeModel> ListArticleType { get; set; }
    }
    public class ArticleTypeModel
    {
        public string ArticleNo { get; set; }
        public string ArticleName { get; set; }
    }
}
