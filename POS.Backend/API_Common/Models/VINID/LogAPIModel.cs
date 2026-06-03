using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class LogAPIModel
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
        public Nullable<bool> StatusVINID { get; set; }
        public Nullable<bool> CallOffline { get; set; }
        public Nullable<System.DateTime> DateTime { get; set; }
     
    }
  
}
