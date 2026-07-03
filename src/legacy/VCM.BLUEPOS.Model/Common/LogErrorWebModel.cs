using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Common
{
    public class LogErrorWebModel
    {
        public long ID { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string OrderNo { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string Msg { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }


}
