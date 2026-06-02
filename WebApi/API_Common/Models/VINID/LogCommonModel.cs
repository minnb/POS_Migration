using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class LogCommonModel
    {
        public long ID { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string OrderNo { get; set; }
        public string CardNumber { get; set; }
        public string ActionType { get; set; }
        public string JSONModel { get; set; }
        public string MessageError { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}
