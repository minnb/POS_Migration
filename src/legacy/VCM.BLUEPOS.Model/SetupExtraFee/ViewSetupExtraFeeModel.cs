using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.SetupItem;

namespace VCM.BLUEPOS.Model.SetupExtraFee
{
    public class ViewSetupExtraFeeModel
    {
        public ViewExtraFeeHeaderModel InfoHeader { get; set; }
        public List<ViewExtraFeeLineModel> ListExtraFeeLineNganhHang { get; set; }
        public List<ViewExtraFeeLineModel> ListExtraFeeLineCombo { get; set; }
        public List<ViewEtraFeeOrderTypeModel> ListExtraFeeOrderTypeByCode { get; set; }
        public List<ViewEtraFeeOrderTypeModel> ListOrderType { get; set; }
        public List<ViewEtraFeeStoreModel> ListExtraFeeStore { get; set; }
        public List<POSGroupModel> ListPOSGroup { get; set; }
    }
    public class ViewExtraFeeHeaderModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string StoreGroup { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
    public class ViewExtraFeeLineModel
    {
        public string Code { get; set; }
        public string ItemType { get; set; }
        public string No { get; set; }
        public string ParentCode { get; set; }
        public string ParentName { get; set; }
        public string GroupName { get; set; }
        public double ExtraFeeValue { get; set; }
    }
    public class ViewEtraFeeOrderTypeModel
    {
        public string Code { get; set; }
        public string SalesOrderType { get; set; }
    }
    public class ViewEtraFeeStoreModel
    {
        public string Code { get; set; }
        public string StoreGroup { get; set; }
        public string StoreNo { get; set; }
    }
}
