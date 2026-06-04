namespace POS.Application.Loyalty.DTOs;

public class GetCustomerRequest
{
    public string NumberCard { get; set; } = string.Empty;
    public string PosId { get; set; } = string.Empty;
    public string StoreNo { get; set; } = string.Empty;
    public string ClubCode { get; set; } = string.Empty;
    public bool IsMobile { get; set; } = false;
    public bool IsLog { get; set; } = true;
}
