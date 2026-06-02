using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
   public class POSDocumentNoModel
    {
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string LastNumber { get; set; }
        public DateTime? LastDateTime { get; set; }
        public string DocumentType { get; set; }

    }
}
