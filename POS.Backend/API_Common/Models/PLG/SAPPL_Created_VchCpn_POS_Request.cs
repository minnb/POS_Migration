using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class SAPPL_Created_VchCpn_Serial_Request
    {
        public string Type { get; set; }
        public string ArticleNo { get; set; }
        public string SerialNumber { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }

    }
    public class SAPPL_Created_VchCpn_POS_Request
    {
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string UserPOS { get; set; }
        public List<SAPPL_Created_VchCpn_Serial_Request> ListSerial { get; set; }
    }

    public class SAPPL_CreatedPOS_Response
    {
        public bool StatusVchCpn { get; set; }
        public string SerialNumber { get; set; }
        public string MessageVchCpn { get; set; }
    }
}
