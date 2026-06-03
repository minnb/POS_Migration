using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class RefundResponse
    {
        public RefundMeta meta { get; set; }
        public RefundData data { get; set; }
    }
    public class RefundMeta
    {
        public string request_id { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }
    public class RefundData
    {
        public HeaderResponse rs_header { get; set; }
        public TransactionResponse transaction { get; set; }
        public List<Refund_Redeem_Pool_List> redeem_pool_list { get; set; }
        public Refund_Member_balance member_balance { get; set; }
        public List<Refund_Member_Accounts> member_accounts { get; set; }
    }
    
   
    public class Refund_Redeem_Pool_List
    {
    }
    public class Refund_Member_balance
    {
        public PoolResponse pool { get; set; }
        public List<AwardsResponse> awards { get; set; }
    }   
  
    public class Refund_Member_profile
    {
        public string full_name { get; set; }
        public string date_of_birth { get; set; }
        public string gender { get; set; }
        public string registration_date { get; set; }
        public string id_type { get; set; }
        public string member_status { get; set; }
        public string email_preference { get; set; }
        public CustomerIdentifierResponse customer_identifier { get; set; }
        public List<Refund_Contact_Details> contact_details { get; set; }
        public string message_type { get; set; }
    }
   
    public class Refund_Contact_Details
    {
        public string mobile_number { get; set; }
        public string email_address { get; set; }
        public string full_address { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string sms_service { get; set; }
    }
    public class Refund_Member_Accounts
    {
        public string account_no { get; set; }
        public string account_type { get; set; }
        public string account_status { get; set; }
        public string block_code { get; set; }
        public string registration_date { get; set; }
        public string activation_date { get; set; }
        public string expiry_date { get; set; }       
        public string account_update { get; set; }
    }
}
