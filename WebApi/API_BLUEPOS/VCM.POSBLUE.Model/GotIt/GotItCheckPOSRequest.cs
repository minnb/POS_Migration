using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.GotIt
{
   public class GotItCheckPOSRequest
    {
        public string serialNo { get; set; }
        public string partner { get; set; }
        public string storeNo { get; set; }
        public string posID { get; set; }
    }
}
