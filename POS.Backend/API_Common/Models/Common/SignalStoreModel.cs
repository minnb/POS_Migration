using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
   public class SignalStoreModel
    {
        public int ID { get; set; }
        public string StoreNO { get; set; }
        public string POSTerminalID { get; set; }
        public Nullable<System.DateTime> BusinessDate { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}
