using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SyncData
{
    public class SAPLogModel
    {
        public string TransType { get; set; }
        public string Message { get; set; }
        public string ProcessID { get; set; }
        public bool Status
        {
            get
            {
                if (Message.Substring(0, 2) != "OK")
                    return false;
                return true;
            }
        }
    }


}
