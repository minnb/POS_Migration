using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class MemberProfileResponse
    {
        public string full_name { get; set; }
        public string date_of_birth { get; set; }
        public string gender { get; set; }
        public string registration_date { get; set; }
        public string client_id_1 { get; set; }
        public string id_type { get; set; }
        public string id_type_1 { get; set; }
        public string csn { get; set; }
        public string member_status { get; set; }
        public string email_preference { get; set; }
        public string migrate_flag { get; set; }
        public CustomerIdentifierResponse customer_identifier { get; set; }
        public List<ContactDetailResponse> contact_details { get; set; }
        public string message_type { get; set; }
    }
}
