using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class HistoryConfirmPOSExcel
    {
        public string StoreNo { get; set; }
        public string TerminalID { get; set; }
        public string DocumentType { get; set; }
        public string Status { get; set; }
        public DateTime? LastOrderTime { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public double? AmountTotal { get; set; }
        public double? CashMoney { get; set; }
        public int? CountCustomer { get; set; }
        public int? CountOrderNo { get; set; }
    }


}
