using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class SpendPointsRequest
    {
        public string transaction_ref_number { get; set; }//transaction no of PnL
        public string description { get; set; }
        public string code_value { get; set; }// QR-BARCODE VALUE WHEN SCAN
        public string date { get; set; }//<Date>20181019</Date>
        public string time { get; set; }//<Time>094101</Time>
        public string time_zone { get; set; }//<TimeZone>GMT+07:00</TimeZone>
        public string merchant_id { get; set; }
        public string terminal_id { get; set; }
        public string invoice_no { get; set; }
        public string channel_code { get; set; }
        public string spend_points { get; set; }
        public string signature { get; set; }
    }
}
