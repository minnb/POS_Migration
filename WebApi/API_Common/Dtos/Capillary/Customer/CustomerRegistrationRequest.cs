using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Customer
{
    public class CustomerRegistrationRequest
    {
        public LoyaltyInfoCapillary LoyaltyInfo { get; set; }
        public List<ProfilesCapillary> Profiles { get; set; }
        public ExtendedFieldsCapillary ExtendedFields { get; set; }
    }

    public class ProfilesCapillary
    {
        public string FirstName { get; set; }
        public List<IdentifiersCapillary> Identifiers { get; set; }
        public string Source { get; set; } = "INSTORE";
        public RegisterFieldsCapillary Fields { get; set; }
    }

    public class ExtendedFieldsCapillary
    {
        public string Gender { get; set; }
        public string Acquisition_channel { get; set; }
        public string Dob_date { get; set; } //yyyy-MM-dd
        public string Sub_area { get; set; }
        public string Area { get; set; }
        public string State { get; set; }
        public string City { get; set; }

    }
    public class RegisterFieldsCapillary
    {
        public string Staff_id { get; set;}
        public string Masan_referral_code { get; set; }
        public string Masan_customer_id { get; set; }
        public string Customer_address { get; set; }
    }
}
