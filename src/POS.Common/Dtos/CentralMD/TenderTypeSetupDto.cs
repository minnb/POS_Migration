namespace POS.Common.Dtos.CentralMD;

/// <summary>
/// Map bảng CentralMD.dbo.TenderTypeSetup — danh mục hình thức thanh toán.
/// Code khớp TransPaymentEntry.TenderType; Description là tên hiển thị chuẩn.
/// </summary>
public sealed class TenderTypeSetupDto
{
    public string Code        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
