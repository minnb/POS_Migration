using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupExtraFee
{
   public class SaveExtraFeeRequest
    {
        public SaveExtraFeeHeaderRequest ExtraFeeHeader { get; set; }
        public List<SaveExtraFeeLineRequest> ExtraFeeNganhHang { get; set; }
        public List<SaveExtraFeeLineRequest> ExtraFeeCombo { get; set; }
    }

    public class SaveExtraFeeHeaderRequest
    {
        public string ExtraFeeNo { get; set; }
        public string ExtraFeeName { get; set; }
        public string StoreGroup { get; set; }
        public List<string> ArraySalesType { get; set; }
        public string FromDateStr { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string ToDateStr { get; set; }
        public string ItemNo { get; set; }
        public string CreatedUser { get; set; }
    }

    public class SaveExtraFeeLineRequest
    {
        public string No { get; set; }
        public string ItemType { get; set; }
        public double ExtraFeeValue { get; set; }
        
    }
}
