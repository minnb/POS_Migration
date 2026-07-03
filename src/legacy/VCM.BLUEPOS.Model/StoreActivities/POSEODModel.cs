using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class POSEODModel
    {
        public int ID { get; set; }
        public string POSTerminalNo { get; set; }
        public string StoreNo { get; set; }
        public Nullable<System.DateTime> BussinessDate { get; set; }
        public Nullable<int> TotalSale { get; set; }
        public Nullable<double> TotalAmount { get; set; }
        public string MaxBillNumber { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }


}
