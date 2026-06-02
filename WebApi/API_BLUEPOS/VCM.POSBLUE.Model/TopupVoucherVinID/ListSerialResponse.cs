using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.TopupVoucherVinID
{
    public class ListSerialResponse
    {
        public string SerialNumber { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        
    }
    public class ListSerialShowPOSResponse
    {
        public string SerialNumber { get; set; }
        public string Message { get; set; }
    }
    public class SerialPOSResponse
    {
        public List<ListSerialResponse> usedList { get; set; }
        public List<RevokeResponse> revokeList { get; set; }

    }
}
