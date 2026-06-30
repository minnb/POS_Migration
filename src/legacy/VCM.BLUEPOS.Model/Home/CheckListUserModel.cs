using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Home
{
    public class CheckListUserModel
    {
        public Nullable<int> CheckListID { get; set; }
        public Nullable<System.DateTime> DateTimeCheck { get; set; }
        public string UserCheck { get; set; }
    }
}
