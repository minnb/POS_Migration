using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Giftee
{
    public class StatusReferenceResponse
    {
        public string code { get; set; }
        public int available_begin { get; set; }
        public int available_end { get; set; }
        public string status { get; set; }//issued, exchanged, expired, disabled
        public int issued_at { get; set; }
        public int? exchanged_at { get; set; }
        public string exchanged_shop_code { get; set; }
        public int? disabled_at { get; set; }
        public List<StatusHistoryRepsonse> histories { get; set; }
    }
    public class StatusHistoryRepsonse
    {
        public string terminal_code { get; set; }
        public string exchanged_shop_code { get; set; }
        public string request_date { get; set; }
        public string cancel_code { get; set; }
        public string operation_kind { get; set; }
        public int? actioned_at { get; set; }
        public int? disabled_at { get; set; }
    }
}
