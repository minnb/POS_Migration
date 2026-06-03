using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class HeaderResponse
    {
        public string date { get; set; }
        public string time { get; set; }
        public string time_zone { get; set; }
        public string message_type { get; set; }
        public string version_no { get; set; }
        public string app_verion { get; set; }
        public string access_token { get; set; }
        public string channel_id { get; set; }
        public string merchant_id { get; set; }
        public string terminal_id { get; set; }
        public string last_host_ref_no { get; set; }
        public string host_ref_no { get; set; }
        public string response_date { get; set; }
        public string response_time { get; set; }
        public string response_code { get; set; }
        public string error_code { get; set; }
        public string error_message { get; set; }
        public string invoice_no { get; set; }
        public string refund_deficit_units { get; set; }
        public string refund_deficit_amount { get; set; }
    }
}
