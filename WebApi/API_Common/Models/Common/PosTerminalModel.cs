using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
   public class PosTerminalModel
    {
        public string IPAddress { get; set; }
        public string StoreNo { get; set; }
        public string TerminalPOS { get; set; }
        public string TerminalNetworkID { get; set; }
        public string StyleProfile { get; set; }
        public string DefaultSalesType { get; set; }
        public string SalesTypeFilter { get; set; }
        public string Pkey { get; set; }
        public string BillNoseri { get; set; }
        public string Placement { get; set; }
        public int StatementMethod { get; set; }
        public byte? TerminalStatement { get; set; }
        public int TerminalConnection { get; set; }
        public string PrintReceiptLogo { get; set; }
        public string CustomerDisplayText1 { get; set; }
        public string CustomerDisplayText2 { get; set; }
        public int PrintReceiptBCType { get; set; }
        public string InterfaceProfile { get; set; }
        public string DualDisHost { get; set; }
    }
}
