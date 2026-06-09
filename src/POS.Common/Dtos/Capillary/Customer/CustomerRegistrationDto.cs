using POS.Common.Dtos.Capillary;

namespace POS.Common.Dtos.Capillary.Customer;

public class CustomerRegistrationRequest
{
    public LoyaltyInfoCapillary? LoyaltyInfo { get; set; }
    public List<ProfilesCapillary>? Profiles { get; set; }
    public ExtendedFieldsCapillary? ExtendedFields { get; set; }
}

public class ProfilesCapillary
{
    public string? FirstName { get; set; }
    public List<IdentifiersCapillary>? Identifiers { get; set; }
    public string Source { get; set; } = "INSTORE";
    public RegisterFieldsCapillary? Fields { get; set; }
}

public class ExtendedFieldsCapillary
{
    public string? Gender { get; set; }
    public string? Acquisition_channel { get; set; }
    public string? Dob_date { get; set; }
    public string? Sub_area { get; set; }
    public string? Area { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
}

public class RegisterFieldsCapillary
{
    public string? Staff_id { get; set; }
    public string? Masan_referral_code { get; set; }
    public string? Masan_customer_id { get; set; }
    public string? Customer_address { get; set; }
}

public class CustomerRegistrationResponse
{
    public int CreatedId { get; set; }
    public List<object>? Warnings { get; set; }
    public List<SideEffect>? SideEffects { get; set; }
}

public class RefundTransationResponse
{
    public int CreatedId { get; set; }
    public List<object>? Warnings { get; set; }
    public List<object>? SideEffects { get; set; }
    public List<object>? Errors { get; set; }
}

public class SideEffect
{
    public string? EntityType { get; set; }
    public decimal RawAwardedPoints { get; set; }
    public long AwardedPoints { get; set; }
    public string? Type { get; set; }
}

public class WarningsCAP
{
    public bool Status { get; set; }
    public string? Code { get; set; }
    public string? Message { get; set; }
}
