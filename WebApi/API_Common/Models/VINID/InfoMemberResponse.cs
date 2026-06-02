using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class InfoMemberResponse
    {
        public Meta meta { get; set; }
        public Data data { get; set; }
    }
    public class Meta
    {
        public string request_id { get; set; }
        public string message { get; set; }
        public int code { get; set; }
    }
    public class Data
    {
        public User_Profile user_profile { get; set; }
        public Oe_Info oe_info { get; set; }
        public DeviceInfo device_info { get; set; }
    }
    public class User_Profile
    {
        public string phone_number { get; set; }
        public string email { get; set; }
        public string full_name { get; set; }
        public string full_name_without_accents { get; set; }
        public string dob { get; set; }
        public int gender { get; set; }
        public string occupation { get; set; }
        public List<Address> address { get; set; }
        public string nationality_1 { get; set; }
    }
    public class Address
    {
        public string city { get; set; }
        public string district { get; set; }
        public string commune { get; set; }
        public string address { get; set; }
        public int type { get; set; }
    }
    public class Oe_Info
    {
        public CustomerIdentifierResponse customer_identifier { get; set; }
        public HeaderResponse rs_header { get; set; }
        public member_balance member_balance { get; set; }
        public member_bucket_balance member_bucket_balance { get; set; }
        public member_profile member_profile { get; set; }
       
        public List<MemberAccountResponse> member_accounts { get; set; }
        public string original_invoice_no { get; set; }
        public string remark { get; set; }
        public string refund_amount { get; set; }
        public string action { get; set; }
        public bool? has_extra_point { get; set; }
    }
    //public class customer_identifier
    //{
    //    public string idType { get; set; }
    //    public string id { get; set; }
    //}    
    public class member_balance
    {
        public Pool pool { get; set; }
    }
    public class Pool
    {
        public string pool_id { get; set; }
        public string pool_description { get; set; }
        public string pool_type { get; set; }
        public string total_pool_units { get; set; }
        public string redeemable_pool_units { get; set; }
        public string earliest_expiry_pool_units { get; set; }
        public string earliest_expiry_date { get; set; }
        public base_currency base_currency { get; set; }
    }
    public class base_currency
    {
        public string unit_value { get; set; }
        public string total_pool_value { get; set; }
        public string redeemable_pool_value { get; set; }
        public string earliest_expirying_pool_value { get; set; }
    }
    public class member_bucket_balance
    {
    }
    public class member_profile
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
        public List<attributes> attributes { get; set; }
        public string message_type { get; set; }
    }
    //public class contact_details
    //{
    //    public string mobile_number { get; set; }
    //    public string email_address { get; set; }
    //    public string home_phone { get; set; }
    //    public string full_address { get; set; }
    //    public string address2 { get; set; }
    //    public string address3 { get; set; }
    //    public string address4 { get; set; }
    //    public string country { get; set; }
    //    public string ward { get; set; }
    //    public string district { get; set; }
    //    public string city { get; set; }
    //    public string sms_service { get; set; }
    //}
    public class attributes
    {
        public string attribute_id { get; set; }
        public string attribute_name { get; set; }
        public string attribute_value { get; set; }
        public string attribute_group_id { get; set; }
    }

    public class DeviceInfo
    {
        public string device_id { get; set; }
        public string app_version { get; set; }
    }

    //public class member_accounts
    //{
    //    public string account_no { get; set; }
    //    public string account_type { get; set; }
    //    public string account_status { get; set; }
    //    public string block_code { get; set; }
    //    public string registration_date { get; set; }
    //    public string activation_date { get; set; }
    //    public string expiry_date { get; set; }
    //    public string csn { get; set; }
    //    public string status { get; set; }
    //    public string card_nick_name { get; set; }
    //    public string account_update { get; set; }
    //}
}
