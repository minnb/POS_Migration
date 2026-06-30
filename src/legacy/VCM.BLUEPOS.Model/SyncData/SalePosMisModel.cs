using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SyncData
{
    public class SalePosMisModel
    {
        public string OrderNo { get; set; }
        public string CustomerName { get; set; }
        public string CashierID { get; set; }
        public double? Amount { get; set; }
    }

    public class MissSalePosModel
    {
        public long? TotalOrderPOS { get; set; }
        public long? TotalOrderCentral{ get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string OrderDate { get; set; }
    }


}
