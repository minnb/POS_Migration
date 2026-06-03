using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class CX_LogAPIModel
    {
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string CardNumber { get; set; }
        public string InvoiceNo { get; set; }
        public string ActionType { get; set; }
        public string PlainText { get; set; }
        public string Signature { get; set; }
        public string RequestPOS { get; set; }
        public string RequestXML { get; set; }
        public string ResponseXML { get; set; }
        public long ResponseTotal { get; set; }
        public string Source { get; set; }
        public Nullable<System.DateTime> DateTime { get; set; }
    }
}
