namespace POS.Application.Payment.DTOs;

public class DataVoucherPartnerResponse
{
    public string Status { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int VoucherAmount { get; set; }
    public int Amount { get; set; }
    public string Msg { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public bool IsApplySku { get; set; }
}
