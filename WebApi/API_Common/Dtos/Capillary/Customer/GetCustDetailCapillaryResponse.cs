using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Capillary.Tier;
using TCX.API.Common.Dtos.Capillary.Transaction;

namespace TCX.API.Common.Dtos.Capillary.Customer
{
    public class GetCustDetailCapillaryResponse
    {
        public CapillaryResponse Response { get; set; }    
    }

    public class CapillaryResponse
    {
        public StatusCodeCapillary Status { get; set; }
        public CustomerCapillary Customers { get; set; }
    }

    public class CustomerCapillary
    {
        public IList<DataCustomerCapillaryResponse> Customer { get; set; }
    }
    public class DataCustomerCapillaryResponse
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string External_id { get; set; }
        public string Lifetime_points { get; set; }
        public string Lifetime_purchases { get; set; }
        public string Loyalty_points { get; set; }
        public string Current_slab { get; set; }
        public string Registered_on { get; set; }
        public string Updated_on { get; set; }
        public string Type { get; set; }
        public string Source { get; set; }
        public List<IdentifiersCapillary> Identifiers { get; set; }
        public string Gender { get; set; }
        public string Registered_by { get; set; }
        public RegisteredBy Registered_store { get; set; }
        public RegisteredBy Registered_till { get; set; }
        public object User_groups2 { get; set; }
        public string Next_slab { get; set; }
        public string Next_slab_serial_number { get; set; }
        public string Next_slab_description { get; set; }
        public string Trackers { get; set; }
        public object Slab_history { get; set; }
        public string Current_nps_status { get; set; }
        public string User_id { get; set; }
        public Custom_fields Custom_fields { get; set; }
        public Extended_fields Extended_fields { get; set; }
        public TransactionDataCapillary Transactions { get; set; }
        public CouponsCapillary Coupons { get; set; }
        public TierUpdateCriteriaCapillary Tier_update_criteria { get; set; }
        public PointsSummaries Points_summaries { get; set; }
    }

    public class PointsSummaries
    {
        public List<PointsSummary> Points_summary { get; set; }
    }
    public class PointsSummary
    {
        public string ProgramId { get; set; }
        public string Redeemed { get; set; }
        public string Returned { get; set; }
        public string Adjusted { get; set; }
        public string LifetimePoints { get; set; }
        public string LoyaltyPoints { get; set; }
        public string CumulativePurchases { get; set; }
        public string NextSlab { get; set; }
        public string NextSlabSerialNumber { get; set; }
        public string NextSlabDescription { get; set; }
        public string SlabSNo { get; set; }
        public string SlabExpiryDate { get; set; }
        public string TotalPoints { get; set; }
        public string Delayed_points { get; set; }
        public string Delayed_returned_points { get; set; }
        public string Total_available_points { get; set; }
        public string Total_returned_points { get; set; }
        public string Program_title { get; set; }
        public string Program_description { get; set; }
        public string Program_points_to_currency_ratio { get; set; }
        public object Linked_partner_programs { get; set; }
        //public List<object> Gap_to_upgrade { get; set; }
        public ItemStatusCapillary Item_status { get; set; }
    }
}
