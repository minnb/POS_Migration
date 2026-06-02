using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class InfoMemberRequest
    {
        public string code_value { get; set; }//transaction no of PnL
        public string date { get; set; }       
        public string time { get; set; }//<Time>094101</Time>
        public string time_zone { get; set; }//<TimeZone>GMT+07:00</TimeZone>
        public string merchant_id { get; set; }
        public string terminal_id { get; set; }       
    }
}
