using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class StoreMappingModel
    {
        public int ID { get; set; }
        public string OldTerminalID { get; set; }
        public string OldStoreID { get; set; }

        public string OldVinPayTerminalID { get; set; }
        public string OldVinPayStoreID { get; set; }
        public string NewTerminalID { get; set; }
        public string NewStoreID { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}
