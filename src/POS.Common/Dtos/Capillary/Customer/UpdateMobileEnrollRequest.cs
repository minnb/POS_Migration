using POS.Common.Dtos.Capillary;

namespace POS.Common.Dtos.Capillary.Customer;

public class UpdateMobileEnrollRequest
{
    public MobileEnrollRoot? Root { get; set; }
}

public class MobileEnrollRoot
{
    public List<MobileEnrollCustomer>? Customer { get; set; }
}

public class MobileEnrollCustomer
{
    public string? Mobile { get; set; }
    public Custom_fields? Custom_fields { get; set; }
}
