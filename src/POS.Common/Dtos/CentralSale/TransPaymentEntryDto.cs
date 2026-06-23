namespace POS.Common.Dtos.CentralSale;

public class TransPaymentEntryDto
{
    public string? TenderType          { get; set; }  // Hình thức thanh toán
    public string? Description         { get; set; }  // Dữ liệu tham chiếu
    public decimal AmountTendered      { get; set; }  // Số tiền
    public string? CardNo              { get; set; }  // Số tham chiếu
    public string? AppCode             { get; set; }  // Mã chuẩn chi
    public string? POSBankTerminalNo   { get; set; }  // POS ngân hàng
    public string? CardType            { get; set; }  // Loại thẻ
    public string? BankPayment         { get; set; }  // Thanh toán ngân hàng
}
