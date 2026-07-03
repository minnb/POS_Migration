using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class StyleProfileModel
    {
        public string StyleProfile { get; set; } // VM,VMP
        public string ProfileName
        {
            get
            {
                var name = StyleProfile == "" ? "Tất cả" : StyleProfile == "VM" ? "Vinmart" : StyleProfile == "VMP" ? "Vinmart+" : "";
                return name;
            }
        }
    }


}
