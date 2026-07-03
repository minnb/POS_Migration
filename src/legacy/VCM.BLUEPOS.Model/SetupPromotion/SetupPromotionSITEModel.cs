using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class SetupPromotionSITEModel
    {
        public string Key { get; set; }
        public string FileNameRead { get; set; }
        public Nullable<System.DateTime> CREATEDDATE { get; set; }
        public string Remark { get; set; }
        public string BBYNR { get; set; }
        public string SITEGROUPCODE { get; set; }
        public string SITECODE { get; set; }
    }
}
