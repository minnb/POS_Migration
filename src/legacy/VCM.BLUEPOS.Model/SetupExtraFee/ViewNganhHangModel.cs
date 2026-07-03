using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.SetupItem;

namespace VCM.BLUEPOS.Model.SetupExtraFee
{
    public class ViewNganhHangModel
    {
        public List<POSGroupModel> ListNganhHang { get; set; }
        public List<JsonExtraFeeSelected> ListNganhHangSelected { get; set; }
    }

    public class ViewComboModel
    {       
        public List<JsonExtraFeeSelected> ListComboSelected { get; set; }
    }
    public class JsonExtraFeeSelected
    {
        public string No { get; set; }
        public string ExtraFeeValue { get; set; }
    }
}
