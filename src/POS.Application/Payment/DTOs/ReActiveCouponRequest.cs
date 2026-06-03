namespace POS.Application.Payment.DTOs;

public class ReActiveCouponRequest
{
    public string Partner { get; set; } = string.Empty;
    public string StoreNo { get; set; } = string.Empty;
    public List<string> SerialNo { get; set; } = new();
    public string UserName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
