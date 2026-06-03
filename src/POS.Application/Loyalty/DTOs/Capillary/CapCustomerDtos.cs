namespace POS.Application.Loyalty.DTOs.Capillary;

// ── GET /api/customer ────────────────────────────────────────────────────────

public class CapGetCustomerResponse
{
    public CapCustomerResponseBody? Response { get; set; }
}

public class CapCustomerResponseBody
{
    public CapStatusCode? Status { get; set; }
    public CapCustomerList? Customers { get; set; }
}

public class CapCustomerList
{
    public IList<CapCustomerData> Customer { get; set; } = new List<CapCustomerData>();
}

public class CapCustomerData
{
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? External_id { get; set; }
    public string? Lifetime_points { get; set; }
    public string? Lifetime_purchases { get; set; }
    public string? Loyalty_points { get; set; }
    public string? Current_slab { get; set; }
    public string? Registered_on { get; set; }
    public string? Updated_on { get; set; }
    public string? Type { get; set; }
    public string? Source { get; set; }
    public List<CapIdentifier>? Identifiers { get; set; }
    public string? Gender { get; set; }
    public string? Registered_by { get; set; }
    public CapRegisteredStore? Registered_store { get; set; }
    public CapRegisteredStore? Registered_till { get; set; }
    public string? Next_slab { get; set; }
    public string? Next_slab_serial_number { get; set; }
    public string? Next_slab_description { get; set; }
    public string? User_id { get; set; }
    public CapCustomFields? Custom_fields { get; set; }
    public CapExtendedFields? Extended_fields { get; set; }
    public CapTransactionList? Transactions { get; set; }
    public CapCouponList? Coupons { get; set; }
    public CapTierUpdateCriteria? Tier_update_criteria { get; set; }
    public CapPointsSummaries? Points_summaries { get; set; }
}

public class CapPointsSummaries
{
    public List<CapPointsSummary> Points_summary { get; set; } = new();
}

public class CapPointsSummary
{
    public string? ProgramId { get; set; }
    public string? Redeemed { get; set; }
    public string? Returned { get; set; }
    public string? Adjusted { get; set; }
    public string? LifetimePoints { get; set; }
    public string? LoyaltyPoints { get; set; }
    public string? CumulativePurchases { get; set; }
    public string? NextSlab { get; set; }
    public string? NextSlabSerialNumber { get; set; }
    public string? NextSlabDescription { get; set; }
    public string? SlabSNo { get; set; }
    public string? SlabExpiryDate { get; set; }
    public string? TotalPoints { get; set; }
    public string? Delayed_points { get; set; }
    public string? Delayed_returned_points { get; set; }
    public string? Total_available_points { get; set; }
    public string? Total_returned_points { get; set; }
    public string? Program_title { get; set; }
    public string? Program_description { get; set; }
    public string? Program_points_to_currency_ratio { get; set; }
    public CapItemStatus? Item_status { get; set; }
}

public class CapTransactionList
{
    public List<CapTransactionData>? Transaction { get; set; }
}

public class CapTransactionData
{
    public string? Id { get; set; }
    public string? Number { get; set; }
    public string? Type { get; set; }
    public string? Created_date { get; set; }
    public string? Store { get; set; }
}

public class CapCouponList
{
    public List<CapCouponData>? Coupon { get; set; }
}

public class CapCouponData
{
    public string? Id { get; set; }
    public string? Series_id { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DateTime Created_date { get; set; }
    public DateTime Valid_till { get; set; }
    public string? Redeemed { get; set; }
    public string? Same_user_multiple_redeem { get; set; }
}

public class CapTierUpdateCriteria
{
    public string? Purchase_type { get; set; }
    public string? Description { get; set; }
    public string? Min_purchase_value_required { get; set; }
}

// ── POST /api/v2/customers (Registration) ───────────────────────────────────

public class CapCustomerRegistrationRequest
{
    public CapLoyaltyInfo? LoyaltyInfo { get; set; }
    public List<CapCustomerProfile>? Profiles { get; set; }
    public CapCustomerExtendedFields? ExtendedFields { get; set; }
}

public class CapLoyaltyInfo
{
    public string? LoyaltyType { get; set; }
}

public class CapCustomerProfile
{
    public string? FirstName { get; set; }
    public List<CapIdentifier>? Identifiers { get; set; }
    public string Source { get; set; } = "INSTORE";
    public CapRegisterFields? Fields { get; set; }
}

public class CapCustomerExtendedFields
{
    public string? Gender { get; set; }
    public string? Acquisition_channel { get; set; }
    public string? Dob_date { get; set; }
    public string? Sub_area { get; set; }
    public string? Area { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
}

public class CapRegisterFields
{
    public string? Staff_id { get; set; }
    public string? Masan_referral_code { get; set; }
    public string? Masan_customer_id { get; set; }
    public string? Customer_address { get; set; }
}

public class CapCustomerRegistrationResponse
{
    public int CreatedId { get; set; }
    public List<object>? Warnings { get; set; }
    public List<CapSideEffect>? SideEffects { get; set; }
}

public class CapSideEffect
{
    public string? EntityType { get; set; }
    public decimal RawAwardedPoints { get; set; }
    public long AwardedPoints { get; set; }
    public string? Type { get; set; }
}

// ── PUT /api/v2/customers (Update) ──────────────────────────────────────────

public class CapCustomerUpdateRequest
{
    public CapLoyaltyInfo? LoyaltyInfo { get; set; }
    public List<CapCustomerUpdateProfile>? Profiles { get; set; }
    public CapCustomerUpdateExtendedFields? ExtendedFields { get; set; }
}

public class CapCustomerUpdateProfile
{
    public string Source { get; set; } = "INSTORE";
    public string? FirstName { get; set; }
    public CapRegisterFields? Fields { get; set; }
}

public class CapCustomerUpdateExtendedFields
{
    public string? Acquisition_channel { get; set; }
    public string? Gender { get; set; }
    public string? Dob_date { get; set; }
    public string? Sub_area { get; set; }
    public string? Area { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
}

// ── GET points ledger ────────────────────────────────────────────────────────

public class CapLedgerResponse
{
    public CapLedgerCustomerDetail? CustomerDetails { get; set; }
    public CapLedgerDetails? LedgerDetails { get; set; }
    public List<CapLedgerEntry>? LedgerEntries { get; set; }
    public List<object>? Warnings { get; set; }
}

public class CapLedgerCustomerDetail
{
    public string? FirstName { get; set; }
    public long UserId { get; set; }
    public string? ExternalId { get; set; }
    public string? EntityType { get; set; }
}

public class CapLedgerDetails
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalEntries { get; set; }
    public int PageCount { get; set; }
}

public class CapLedgerEntry
{
    public long EventLogId { get; set; }
    public string? EventName { get; set; }
    public long CustomerId { get; set; }
    public DateTime LedgerCreatedDate { get; set; }
    public List<CapLedgerEntryDetail>? EntryDetails { get; set; }
    public string? NetPointsOnEvent { get; set; }
    public string? Store { get; set; }
    public string? StoreCode { get; set; }
    public string? TillCode { get; set; }
    public int SourceProgramId { get; set; }
    public string? SourceProgramName { get; set; }
}

public class CapLedgerEntryDetail
{
    public string? LedgerEntryType { get; set; }
    public string? Points { get; set; }
    public string? PointsCategory { get; set; }
    public string? ProgramName { get; set; }
    public int ProgramId { get; set; }
}
