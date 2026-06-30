using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupExtraFee
{
    public class ExtraFeeStroreGroupModel
    {
        public string StoreGroup { get; set; }
        public string StroreGroupName { get; set; }
        public string ListStore { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<bool> Status { get; set; }
    }
}
