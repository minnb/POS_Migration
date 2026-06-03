using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Customer
{
    public class CustomerUpdateCapillary
    {
        public LoyaltyInfoCapillary LoyaltyInfo { get; set; }
        public List<ProfilesUpdateCapillary> Profiles { get; set; }
        public ExtFieldsCustomerUpdateCapillary ExtendedFields { get; set; }
    }
    public class ProfilesUpdateCapillary
    {
        public string Source { get; set; } = "INSTORE";
        public string FirstName { get; set; }
        public RegisterFieldsCapillary Fields { get; set; }
    }
    public class ExtFieldsCustomerUpdateCapillary
    {
        public string Acquisition_channel { get; set; }
        public string Gender { get; set; }
        public string Dob_date { get; set; }
        public string Sub_area { get; set; }
        public string Area { get; set; }
        public string State { get; set; }
        public string City { get; set; }
    }
}
