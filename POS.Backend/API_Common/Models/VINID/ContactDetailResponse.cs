using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class ContactDetailResponse
    {
        public string mobile_number { get; set; }
        public string email_address { get; set; }
        public string home_phone { get; set; }
        public string full_address { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string address4 { get; set; }
        public string country { get; set; }
        public string ward { get; set; }
        public string district { get; set; }
        public string city { get; set; }
        public string sms_service { get; set; }
    }
}
