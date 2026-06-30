using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.SetupItem;
using VCM.BLUEPOS.Model.SetupPromotion;

namespace VCM.BLUEPOS.Model.SetupCoupon
{
    public class ViewSetupCouponModel
    {
        public string ItemNo { get; set; }
        public int QuantityCode { get; set; }
        public List<UnitOfMeasureModel> ListUOM { get; set; }
        public CpnVchBOMIssueRuleModel CpnRule { get; set; }
        public List<IssueTypeCouponModel> ListIssueType { get; set; }
        public List<DiscountTypeModel> ListDiscountType { get; set; }
        public List<TypeCouponModel> ListType { get; set; }
        public List<SalesTypeModel> ListSalesType { get; set; }
        public List<StoreGroupCodeModel> ListStoreGroup { get; set; }
        public List<CpnItemModel> ListItemLine { get; set; }
    }

    public class IssueTypeCouponModel
    {
        public string IssueType { get; set; }
        public string IssueName { get; set; }
    }

    public class TypeCouponModel
    {
        public string TypeCode { get; set; }
        public string TypeName { get; set; }
    }
    public class DiscountTypeModel
    {
        public int DiscountType { get; set; }
        public string DiscountName { get; set; }
    }
}
