using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class MemberAccountResponse
    {
        public string account_no { get; set; }
        public string account_type { get; set; }
        public string account_status { get; set; }
        public string block_code { get; set; }
        public string registration_date { get; set; }
        public string activation_date { get; set; }
        public string expiry_date { get; set; }
        public string csn { get; set; }
        public string status { get; set; }
        public string card_nick_name { get; set; }
        public string account_update { get; set; }
    }
}
