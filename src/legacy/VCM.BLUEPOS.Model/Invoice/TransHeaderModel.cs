using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class TransHeaderModel
    {
        public string ListOrderSelect { get; set; }
        public string OrderNo { get; set; }
        public string StoreNo { get; set; }
        public string Partner { get; set; }
        public string CustomerName { get; set; }
        public DateTime? OrderDate { get; set; }
        public double? PaymentAtPOSAmount { get; set; }
        public string POSTerminalNo { get; set; }
        public string CashierID { get; set; }
        public string PaymentFormat { get { return PaymentAtPOSAmount == 0 ? "0" : string.Format("{0:###,###}", PaymentAtPOSAmount); } }
        public string OrderDateFormat { get { return String.Format("{0:dd-MM-yyyy HH:mm:ss}", OrderDate); } }
        public int? TotalRow { get; set; }
        public string TanencyNo { get; set; }
        public int DeliveringMethod { get; set; }
    }
}
